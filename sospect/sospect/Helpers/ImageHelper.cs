using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace sospect.Helpers
{
    public static class ImageHelper
    {
        /// <summary>
        /// Lee la orientación EXIF de una imagen y devuelve el ángulo de rotación necesario.
        /// Orientaciones EXIF:
        /// 1 = Normal (0°)
        /// 3 = Rotada 180°
        /// 6 = Rotada 90° CW (reloj)
        /// 8 = Rotada 90° CCW (contra-reloj)
        /// </summary>
        public static int GetExifRotationAngle(Stream imageStream)
        {
            try
            {
                // Leer los primeros bytes para encontrar el marcador EXIF
                byte[] buffer = new byte[65536]; // 64KB debería ser suficiente para EXIF
                int bytesRead = imageStream.Read(buffer, 0, buffer.Length);

                // Resetear la posición del stream
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }

                // Buscar el marcador JPEG SOI (Start of Image) FFD8
                if (bytesRead < 2 || buffer[0] != 0xFF || buffer[1] != 0xD8)
                {
                    return 0; // No es JPEG
                }

                int offset = 2;
                while (offset < bytesRead - 4)
                {
                    // Buscar marcador de segmento
                    if (buffer[offset] != 0xFF)
                    {
                        offset++;
                        continue;
                    }

                    byte marker = buffer[offset + 1];

                    // APP1 marker (EXIF)
                    if (marker == 0xE1)
                    {
                        int segmentLength = (buffer[offset + 2] << 8) | buffer[offset + 3];

                        // Verificar "Exif\0\0"
                        if (offset + 10 < bytesRead &&
                            buffer[offset + 4] == 'E' && buffer[offset + 5] == 'x' &&
                            buffer[offset + 6] == 'i' && buffer[offset + 7] == 'f' &&
                            buffer[offset + 8] == 0 && buffer[offset + 9] == 0)
                        {
                            int tiffOffset = offset + 10;

                            // Determinar endianness (II = little endian, MM = big endian)
                            bool isLittleEndian = buffer[tiffOffset] == 'I' && buffer[tiffOffset + 1] == 'I';

                            // Leer offset al primer IFD
                            int ifdOffset = isLittleEndian
                                ? buffer[tiffOffset + 4] | (buffer[tiffOffset + 5] << 8) | (buffer[tiffOffset + 6] << 16) | (buffer[tiffOffset + 7] << 24)
                                : (buffer[tiffOffset + 4] << 24) | (buffer[tiffOffset + 5] << 16) | (buffer[tiffOffset + 6] << 8) | buffer[tiffOffset + 7];

                            int ifdStart = tiffOffset + ifdOffset;

                            if (ifdStart + 2 >= bytesRead)
                                return 0;

                            // Número de entradas en el IFD
                            int numEntries = isLittleEndian
                                ? buffer[ifdStart] | (buffer[ifdStart + 1] << 8)
                                : (buffer[ifdStart] << 8) | buffer[ifdStart + 1];

                            // Buscar la etiqueta de orientación (0x0112)
                            for (int i = 0; i < numEntries && ifdStart + 2 + i * 12 + 12 <= bytesRead; i++)
                            {
                                int entryOffset = ifdStart + 2 + i * 12;

                                int tag = isLittleEndian
                                    ? buffer[entryOffset] | (buffer[entryOffset + 1] << 8)
                                    : (buffer[entryOffset] << 8) | buffer[entryOffset + 1];

                                // Tag 0x0112 = Orientation
                                if (tag == 0x0112)
                                {
                                    int orientation = isLittleEndian
                                        ? buffer[entryOffset + 8] | (buffer[entryOffset + 9] << 8)
                                        : (buffer[entryOffset + 8] << 8) | buffer[entryOffset + 9];

                                    System.Diagnostics.Debug.WriteLine($"ImageHelper: Orientación EXIF encontrada: {orientation}");

                                    // Convertir orientación EXIF a ángulo de rotación
                                    switch (orientation)
                                    {
                                        case 1: return 0;      // Normal
                                        case 3: return 180;    // Rotado 180°
                                        case 6: return 90;     // Rotado 90° CW
                                        case 8: return 270;    // Rotado 90° CCW (270° CW)
                                        default: return 0;
                                    }
                                }
                            }
                        }

                        offset += 2 + segmentLength;
                    }
                    else if (marker == 0xD9 || marker == 0xDA)
                    {
                        // End of Image o Start of Scan - dejar de buscar
                        break;
                    }
                    else if (marker >= 0xE0 && marker <= 0xEF)
                    {
                        // Otros segmentos APP, saltar
                        int segmentLength = (buffer[offset + 2] << 8) | buffer[offset + 3];
                        offset += 2 + segmentLength;
                    }
                    else
                    {
                        offset++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageHelper: Error leyendo EXIF: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Comprime y corrige la orientación de una imagen desde un archivo.
        /// Devuelve la ruta del archivo comprimido con orientación corregida.
        /// </summary>
        public static async Task<string> CompressAndFixOrientationAsync(
            string sourcePath,
            string destinationPath,
            int maxWidth = 1024,
            float quality = 0.6f)
        {
            try
            {
                // Primero leer la orientación EXIF
                int rotationAngle;
                using (var exifStream = File.OpenRead(sourcePath))
                {
                    rotationAngle = GetExifRotationAngle(exifStream);
                }

                System.Diagnostics.Debug.WriteLine($"ImageHelper: Archivo {Path.GetFileName(sourcePath)} requiere rotación de {rotationAngle}°");

                // Leer la imagen
                using (var sourceStream = File.OpenRead(sourcePath))
                {
                    var originalSize = sourceStream.Length;
                    System.Diagnostics.Debug.WriteLine($"ImageHelper: Tamaño original: {originalSize / 1024}KB");

                    var image = PlatformImage.FromStream(sourceStream);
                    if (image == null)
                    {
                        System.Diagnostics.Debug.WriteLine("ImageHelper: No se pudo cargar la imagen");
                        return null;
                    }

                    float width = image.Width;
                    float height = image.Height;

                    // Si hay rotación de 90 o 270 grados, intercambiar dimensiones para el cálculo
                    if (rotationAngle == 90 || rotationAngle == 270)
                    {
                        // Las dimensiones efectivas se invierten después de rotar
                        var temp = width;
                        width = height;
                        height = temp;
                    }

                    // Calcular nuevas dimensiones manteniendo la proporción
                    if (width > maxWidth)
                    {
                        float scale = maxWidth / width;
                        width = maxWidth;
                        height = height * scale;
                    }

                    // Redimensionar si es necesario (antes de rotar)
                    Microsoft.Maui.Graphics.IImage processedImage = image;
                    float originalWidth = image.Width;
                    float originalHeight = image.Height;

                    if (rotationAngle == 90 || rotationAngle == 270)
                    {
                        // Para rotaciones de 90/270, redimensionar usando dimensiones invertidas
                        if (originalHeight > maxWidth)
                        {
                            float scale = maxWidth / originalHeight;
                            processedImage = image.Resize((int)(originalWidth * scale), (int)(originalHeight * scale), ResizeMode.Fit);
                        }
                    }
                    else
                    {
                        if (originalWidth > maxWidth)
                        {
                            float scale = maxWidth / originalWidth;
                            processedImage = image.Resize((int)(originalWidth * scale), (int)(originalHeight * scale), ResizeMode.Fit);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"ImageHelper: Dimensiones después de redimensionar: {processedImage.Width}x{processedImage.Height}");

                    // Aplicar rotación si es necesario
                    if (rotationAngle != 0)
                    {
                        processedImage = RotateImage(processedImage, rotationAngle);
                        System.Diagnostics.Debug.WriteLine($"ImageHelper: Imagen rotada {rotationAngle}°, nuevas dimensiones: {processedImage.Width}x{processedImage.Height}");
                    }

                    // Guardar la imagen procesada
                    using (var destStream = File.Create(destinationPath))
                    {
                        processedImage.Save(destStream, ImageFormat.Jpeg, quality);
                        await destStream.FlushAsync();
                    }

                    // Limpiar
                    if (processedImage != image)
                    {
                        processedImage.Dispose();
                    }
                    image.Dispose();

                    var compressedSize = new FileInfo(destinationPath).Length;
                    System.Diagnostics.Debug.WriteLine($"ImageHelper: Tamaño comprimido: {compressedSize / 1024}KB");

                    return destinationPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageHelper: Error procesando imagen: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ImageHelper", "CompressAndFixOrientationAsync");
                return null;
            }
        }

        /// <summary>
        /// Rota una imagen según el ángulo especificado (90, 180, 270 grados).
        /// </summary>
        private static Microsoft.Maui.Graphics.IImage RotateImage(Microsoft.Maui.Graphics.IImage source, int angleDegrees)
        {
            try
            {
                int sourceWidth = (int)source.Width;
                int sourceHeight = (int)source.Height;

                // Calcular dimensiones del resultado
                int destWidth, destHeight;
                if (angleDegrees == 90 || angleDegrees == 270)
                {
                    destWidth = sourceHeight;
                    destHeight = sourceWidth;
                }
                else
                {
                    destWidth = sourceWidth;
                    destHeight = sourceHeight;
                }

#if ANDROID
                // Usar Android.Graphics.Bitmap para rotación
                using (var memStream = new MemoryStream())
                {
                    source.Save(memStream, ImageFormat.Jpeg, 1.0f);
                    memStream.Position = 0;

                    var originalBitmap = Android.Graphics.BitmapFactory.DecodeStream(memStream);
                    if (originalBitmap == null)
                        return source;

                    var matrix = new Android.Graphics.Matrix();
                    matrix.PostRotate(angleDegrees);

                    var rotatedBitmap = Android.Graphics.Bitmap.CreateBitmap(
                        originalBitmap,
                        0, 0,
                        originalBitmap.Width,
                        originalBitmap.Height,
                        matrix,
                        true);

                    originalBitmap.Recycle();

                    // Convertir de vuelta a IImage
                    using (var rotatedStream = new MemoryStream())
                    {
                        rotatedBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 100, rotatedStream);
                        rotatedBitmap.Recycle();
                        rotatedStream.Position = 0;

                        return PlatformImage.FromStream(rotatedStream);
                    }
                }
#elif IOS || MACCATALYST
                // Usar UIKit para rotación en iOS
                using (var memStream = new MemoryStream())
                {
                    source.Save(memStream, ImageFormat.Jpeg, 1.0f);
                    memStream.Position = 0;

                    var data = Foundation.NSData.FromStream(memStream);
                    var uiImage = UIKit.UIImage.LoadFromData(data);

                    if (uiImage == null)
                        return source;

                    UIKit.UIImageOrientation orientation;
                    switch (angleDegrees)
                    {
                        case 90:
                            orientation = UIKit.UIImageOrientation.Right;
                            break;
                        case 180:
                            orientation = UIKit.UIImageOrientation.Down;
                            break;
                        case 270:
                            orientation = UIKit.UIImageOrientation.Left;
                            break;
                        default:
                            return source;
                    }

                    var rotatedImage = UIKit.UIImage.FromImage(uiImage.CGImage, uiImage.CurrentScale, orientation);

                    // Renderizar la imagen rotada para que la orientación sea aplicada permanentemente
                    var size = new CoreGraphics.CGSize(destWidth, destHeight);
                    UIKit.UIGraphics.BeginImageContextWithOptions(size, false, uiImage.CurrentScale);
                    rotatedImage.Draw(new CoreGraphics.CGRect(0, 0, destWidth, destHeight));
                    var normalizedImage = UIKit.UIGraphics.GetImageFromCurrentImageContext();
                    UIKit.UIGraphics.EndImageContext();

                    if (normalizedImage == null)
                        return source;

                    using (var rotatedData = normalizedImage.AsJPEG(1.0f))
                    using (var rotatedStream = rotatedData.AsStream())
                    {
                        return PlatformImage.FromStream(rotatedStream);
                    }
                }
#else
                // Fallback para otras plataformas - sin rotación
                System.Diagnostics.Debug.WriteLine("ImageHelper: Rotación no soportada en esta plataforma");
                return source;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageHelper: Error rotando imagen: {ex.Message}");
                return source;
            }
        }

        public static readonly BindableProperty AndroidResourceNameProperty =
            BindableProperty.CreateAttached(
                "AndroidResourceName",
                typeof(string),
                typeof(ImageHelper),
                null,
                propertyChanged: OnAndroidResourceNameChanged);

        public static string GetAndroidResourceName(BindableObject view)
            => (string)view.GetValue(AndroidResourceNameProperty);

        public static void SetAndroidResourceName(BindableObject view, string value)
        {
            System.Diagnostics.Debug.WriteLine($"ImageHelper.SetAndroidResourceName: Intentando establecer '{value}'");
            view.SetValue(AndroidResourceNameProperty, value);
        }

        private static void OnAndroidResourceNameChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is Image image)
            {
                var resourceName = newValue as string;
                System.Diagnostics.Debug.WriteLine($"ImageHelper: PropertyChanged - OLD='{oldValue}', NEW='{resourceName}'");

                if (string.IsNullOrEmpty(resourceName))
                {
                    System.Diagnostics.Debug.WriteLine($"ImageHelper: WARNING - resourceName es null o vacío");

                    // Diagnóstico adicional - ver qué contiene el BindingContext
                    if (image.BindingContext != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ImageHelper: BindingContext tipo: {image.BindingContext.GetType().Name}");

                        // Intentar obtener la propiedad Icono del BindingContext
                        var iconoProp = image.BindingContext.GetType().GetProperty("Icono");
                        if (iconoProp != null)
                        {
                            var iconoValue = iconoProp.GetValue(image.BindingContext);
                            System.Diagnostics.Debug.WriteLine($"ImageHelper: Valor de Icono en BindingContext: '{iconoValue}'");
                        }

                        var iconoPathProp = image.BindingContext.GetType().GetProperty("IconoPathSinExtension");
                        if (iconoPathProp != null)
                        {
                            var iconoPathValue = iconoPathProp.GetValue(image.BindingContext);
                            System.Diagnostics.Debug.WriteLine($"ImageHelper: Valor de IconoPathSinExtension en BindingContext: '{iconoPathValue}'");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ImageHelper: BindingContext es NULL");
                    }
                }
            }
        }
    }
}