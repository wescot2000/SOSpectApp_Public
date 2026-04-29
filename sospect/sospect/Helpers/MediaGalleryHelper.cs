using System;
using System.IO;
using System.Threading.Tasks;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper para guardar fotos y videos en la galería del dispositivo
    /// Funciona en Android e iOS
    /// </summary>
    public static class MediaGalleryHelper
    {
        /// <summary>
        /// Guarda una foto o video en la galería del usuario
        /// </summary>
        /// <param name="filePath">Ruta completa del archivo a guardar</param>
        /// <param name="isVideo">True si es video, False si es foto</param>
        /// <returns>True si se guardó exitosamente</returns>
        public static async Task<bool> SaveToGalleryAsync(string filePath, bool isVideo = false)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Archivo no existe: {filePath}");
                    return false;
                }

#if ANDROID
                return await SaveToGalleryAndroidAsync(filePath, isVideo);
#elif IOS || MACCATALYST
                return await SaveToGalleryiOSAsync(filePath, isVideo);
#else
                System.Diagnostics.Debug.WriteLine("[MediaGalleryHelper] Plataforma no soportada");
                return false;
#endif
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MediaGalleryHelper", "SaveToGalleryAsync");
                System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Error: {ex.Message}");
                return false;
            }
        }

#if ANDROID
        private static async Task<bool> SaveToGalleryAndroidAsync(string filePath, bool isVideo)
        {
            try
            {
                var context = Android.App.Application.Context;
                var contentResolver = context.ContentResolver;

                // Leer bytes del archivo
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                string fileName = Path.GetFileName(filePath);
                string mimeType = isVideo ? "video/mp4" : "image/jpeg";

                // Usar MediaStore para Android 10+ (API 29+)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    var collection = isVideo
                        ? Android.Provider.MediaStore.Video.Media.ExternalContentUri
                        : Android.Provider.MediaStore.Images.Media.ExternalContentUri;

                    var contentValues = new Android.Content.ContentValues();
                    contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
                    contentValues.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, mimeType);
                    contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DateAdded, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    if (isVideo)
                    {
                        contentValues.Put(Android.Provider.MediaStore.Video.VideoColumns.RelativePath,
                            Android.OS.Environment.DirectoryMovies + "/SOSpect");
                    }
                    else
                    {
                        contentValues.Put(Android.Provider.MediaStore.Images.ImageColumns.RelativePath,
                            Android.OS.Environment.DirectoryPictures + "/SOSpect");
                    }

                    var uri = contentResolver?.Insert(collection, contentValues);
                    if (uri != null)
                    {
                        using var stream = contentResolver?.OpenOutputStream(uri);
                        if (stream != null)
                        {
                            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            await stream.FlushAsync();
                            System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Guardado en galería Android: {fileName}");
                            return true;
                        }
                    }
                }
                else
                {
                    // Para Android < 10, usar el método legacy
                    var picturesPath = isVideo
                        ? Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies)
                        : Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures);

                    var sospectFolder = new Java.IO.File(picturesPath, "SOSpect");
                    if (!sospectFolder.Exists())
                    {
                        sospectFolder.Mkdirs();
                    }

                    var destFile = new Java.IO.File(sospectFolder, fileName);
                    await File.WriteAllBytesAsync(destFile.AbsolutePath, fileBytes);

                    // Notificar al MediaScanner
                    var intent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
                    intent.SetData(Android.Net.Uri.FromFile(destFile));
                    context.SendBroadcast(intent);

                    System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Guardado en galería Android (legacy): {fileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MediaGalleryHelper", "SaveToGalleryAndroidAsync");
                System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Error Android: {ex.Message}");
                return false;
            }
        }
#endif

#if IOS || MACCATALYST
        private static async Task<bool> SaveToGalleryiOSAsync(string filePath, bool isVideo)
        {
            try
            {
                if (isVideo)
                {
                    // Guardar video en Photos
                    var tcs = new TaskCompletionSource<bool>();

                    UIKit.UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        try
                        {
                            Photos.PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() =>
                            {
                                Photos.PHAssetChangeRequest.FromVideo(Foundation.NSUrl.FromFilename(filePath));
                            }, (success, error) =>
                            {
                                if (success)
                                {
                                    System.Diagnostics.Debug.WriteLine("[MediaGalleryHelper] Video guardado en Photos iOS");
                                    tcs.SetResult(true);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Error iOS video: {error?.LocalizedDescription}");
                                    tcs.SetResult(false);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Exception iOS video: {ex.Message}");
                            tcs.SetResult(false);
                        }
                    });

                    return await tcs.Task;
                }
                else
                {
                    // Guardar foto en Photos
                    var tcs = new TaskCompletionSource<bool>();

                    UIKit.UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        try
                        {
                            // No usar "using" aquí: PerformChanges es asíncrono y el callback
                            // se ejecuta después de que el bloque using ya habría liberado la imagen,
                            // causando ObjectDisposedException en iOS.
                            var image = UIKit.UIImage.FromFile(filePath);
                            if (image != null)
                            {
                                Photos.PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() =>
                                {
                                    Photos.PHAssetChangeRequest.FromImage(image);
                                }, (success, error) =>
                                {
                                    // Liberar la imagen DESPUÉS de que PerformChanges haya terminado
                                    image.Dispose();
                                    if (success)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[MediaGalleryHelper] Foto guardada en Photos iOS");
                                        tcs.SetResult(true);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Error iOS foto: {error?.LocalizedDescription}");
                                        tcs.SetResult(false);
                                    }
                                });
                            }
                            else
                            {
                                tcs.SetResult(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Exception iOS foto: {ex.Message}");
                            tcs.SetResult(false);
                        }
                    });

                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MediaGalleryHelper", "SaveToGalleryiOSAsync");
                System.Diagnostics.Debug.WriteLine($"[MediaGalleryHelper] Error iOS: {ex.Message}");
                return false;
            }
        }
#endif
    }
}
