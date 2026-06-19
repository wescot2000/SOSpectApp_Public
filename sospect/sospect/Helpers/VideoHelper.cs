// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.IO;
using System.Threading.Tasks;

namespace sospect.Helpers
{
    public static class VideoHelper
    {
        /// <summary>
        /// Genera un thumbnail JPEG del primer frame del video.
        /// En iOS usa AVAssetImageGenerator. En otras plataformas devuelve el path del video (sin thumbnail).
        /// </summary>
        public static async Task<string> GenerateVideoThumbnailAsync(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                System.Diagnostics.Debug.WriteLine($"[VideoHelper] GenerateVideoThumbnailAsync: archivo no existe: {videoPath}");
                return videoPath;
            }

            System.Diagnostics.Debug.WriteLine($"[VideoHelper] GenerateVideoThumbnailAsync: iniciando para {videoPath}");

            try
            {
#if IOS || MACCATALYST
                var cacheDir = FileSystem.CacheDirectory; // capturar fuera del Task.Run (hilo principal)
                return await Task.Run(() =>
                {
                    try
                    {
                        var fileUrl = Foundation.NSUrl.FromFilename(videoPath);
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] fileUrl: {fileUrl}");

                        var asset = AVFoundation.AVAsset.FromUrl(fileUrl);
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] asset creado, tracks: {asset.Tracks?.Length ?? -1}");

                        var generator = new AVFoundation.AVAssetImageGenerator(asset);
                        generator.AppliesPreferredTrackTransform = true;
                        generator.RequestedTimeToleranceBefore = CoreMedia.CMTime.Zero;
                        generator.RequestedTimeToleranceAfter = CoreMedia.CMTime.FromSeconds(2, 600);

                        // Intentar primero en 0.5s (el frame en t=0 a veces es negro)
                        var requestTime = CoreMedia.CMTime.FromSeconds(0.5, 600);
                        Foundation.NSError outError;
                        CoreMedia.CMTime actualTime;
                        var cgImage = generator.CopyCGImageAtTime(requestTime, out actualTime, out outError);

                        if (cgImage == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[VideoHelper] Frame en 0.5s es null, error={outError?.LocalizedDescription}. Intentando en t=0...");
                            // Fallback: intentar en t=0 con tolerancia más amplia
                            generator.RequestedTimeToleranceBefore = CoreMedia.CMTime.FromSeconds(2, 600);
                            cgImage = generator.CopyCGImageAtTime(CoreMedia.CMTime.Zero, out actualTime, out outError);
                        }

                        if (cgImage == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[VideoHelper] AVAssetImageGenerator devolvió null en t=0 también. Error={outError?.LocalizedDescription}");
                            return videoPath;
                        }

                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] cgImage obtenido en actualTime={actualTime.Seconds:F2}s");

                        var uiImage = new UIKit.UIImage(cgImage);
                        var jpgData = uiImage.AsJPEG(0.75f);
                        if (jpgData == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[VideoHelper] AsJPEG devolvió null");
                            return videoPath;
                        }

                        var thumbPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}_thumb.jpg");
                        bool saved = jpgData.Save(thumbPath, atomically: true);
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] Thumbnail guardado={saved}, path={thumbPath}");

                        return saved ? thumbPath : videoPath;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] Error generando thumbnail iOS: {ex.Message}\n{ex.StackTrace}");
                        return videoPath;
                    }
                });
#else
                await Task.CompletedTask;
                return videoPath;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VideoHelper] GenerateVideoThumbnailAsync ERROR: {ex.Message}");
                return videoPath;
            }
        }

        /// <summary>
        /// Genera un thumbnail del video y lo retorna como string Base64 (JPEG).
        /// Retorna null si no puede generarlo.
        /// </summary>
        public static async Task<string?> GenerateVideoThumbnailBase64Async(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
                return null;

            try
            {
#if IOS || MACCATALYST
                return await Task.Run(() =>
                {
                    try
                    {
                        var fileUrl = Foundation.NSUrl.FromFilename(videoPath);
                        var asset = AVFoundation.AVAsset.FromUrl(fileUrl);
                        var generator = new AVFoundation.AVAssetImageGenerator(asset);
                        generator.AppliesPreferredTrackTransform = true;
                        generator.RequestedTimeToleranceBefore = CoreMedia.CMTime.Zero;
                        generator.RequestedTimeToleranceAfter = CoreMedia.CMTime.FromSeconds(2, 600);

                        Foundation.NSError outError;
                        CoreMedia.CMTime actualTime;
                        var requestTime = CoreMedia.CMTime.FromSeconds(0.5, 600);
                        var cgImage = generator.CopyCGImageAtTime(requestTime, out actualTime, out outError);

                        if (cgImage == null)
                        {
                            generator.RequestedTimeToleranceBefore = CoreMedia.CMTime.FromSeconds(2, 600);
                            cgImage = generator.CopyCGImageAtTime(CoreMedia.CMTime.Zero, out actualTime, out outError);
                        }

                        if (cgImage == null) return null;

                        var uiImage = new UIKit.UIImage(cgImage);
                        var jpgData = uiImage.AsJPEG(0.75f);
                        if (jpgData == null) return null;

                        return jpgData.GetBase64EncodedString(Foundation.NSDataBase64EncodingOptions.None);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] Error generando thumbnail Base64 iOS: {ex.Message}");
                        return null;
                    }
                });
#elif ANDROID
                return await Task.Run(() =>
                {
                    try
                    {
                        var retriever = new Android.Media.MediaMetadataRetriever();
                        retriever.SetDataSource(videoPath);
                        var bitmap = retriever.GetFrameAtTime(0, Android.Media.Option.ClosestSync);
                        if (bitmap == null) return null;

                        using var stream = new System.IO.MemoryStream();
                        bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 75, stream);
                        return Convert.ToBase64String(stream.ToArray());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VideoHelper] Error generando thumbnail Base64 Android: {ex.Message}");
                        return null;
                    }
                });
#else
                await Task.CompletedTask;
                return null;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VideoHelper] GenerateVideoThumbnailBase64Async ERROR: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retorna el MIME type correcto según la extensión real del archivo de video.
        /// </summary>
        public static string GetVideoMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            return ext switch
            {
                ".mov" => "video/quicktime",
                ".m4v" => "video/x-m4v",
                ".3gp" => "video/3gpp",
                ".avi" => "video/x-msvideo",
                _      => "video/mp4"
            };
        }
    }
}


