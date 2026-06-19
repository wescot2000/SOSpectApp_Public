// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if ANDROID
using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using System;

namespace sospect.Platforms.Android.Handlers
{
    public class CustomImageHandler : ImageHandler
    {
        private Image _imageElement;

        protected override void ConnectHandler(ImageView platformView)
        {
            base.ConnectHandler(platformView);

            // Obtener el elemento Image de MAUI (no IImage)
            _imageElement = VirtualView as Image;

            if (_imageElement != null)
            {
                // Suscribirse a cambios en las propiedades del elemento
                _imageElement.PropertyChanged += OnImagePropertyChanged;

                // Actualizar la imagen inicial
                UpdateImageFromResourceName();
            }
        }

        protected override void DisconnectHandler(ImageView platformView)
        {
            if (_imageElement != null)
            {
                _imageElement.PropertyChanged -= OnImagePropertyChanged;
                _imageElement = null;
            }

            base.DisconnectHandler(platformView);
        }

        private void OnImagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Cuando cambia el attached property, actualizar la imagen
            if (e.PropertyName == ImageHelper.AndroidResourceNameProperty.PropertyName)
            {
                System.Diagnostics.Debug.WriteLine($"CustomImageHandler: PropertyChanged detectado: {e.PropertyName}");
                UpdateImageFromResourceName();
            }
        }

        private void UpdateImageFromResourceName()
        {
            if (_imageElement == null || PlatformView == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomImageHandler: _imageElement o PlatformView es null");
                return;
            }

            var resourceName = ImageHelper.GetAndroidResourceName(_imageElement);

            if (string.IsNullOrEmpty(resourceName))
            {
                // OPTIMIZADO: Log eliminado - este caso es normal cuando no hay imagen configurada
                return;
            }

            try
            {
                var context = PlatformView.Context;

                // Limpiar el nombre (igual que en CustomMapHandler)
                var cleanName = resourceName
                    .Replace(".png", "")
                    .Replace(".PNG", "")
                    .ToLower();

                System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Cargando '{cleanName}' (original: '{resourceName}')");

                // Obtener resource ID
                var resourceId = context.Resources.GetIdentifier(cleanName, "drawable", context.PackageName);

                if (resourceId == 0)
                {
                    resourceId = context.Resources.GetIdentifier(cleanName, "mipmap", context.PackageName);
                }

                if (resourceId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Recurso NO encontrado: {cleanName}");

                    // Intentar con fallback "sospecticon"
                    resourceId = context.Resources.GetIdentifier("sospecticon", "drawable", context.PackageName);
                    if (resourceId == 0)
                    {
                        resourceId = context.Resources.GetIdentifier("sospecticon", "mipmap", context.PackageName);
                    }

                    if (resourceId == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Fallback 'sospecticon' tampoco encontrado");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Usando fallback 'sospecticon'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Recurso encontrado! ID={resourceId}");
                }

                // Decodificar el bitmap (mismo método que CustomMapHandler)
                var bitmap = BitmapFactory.DecodeResource(context.Resources, resourceId);

                if (bitmap != null)
                {
                    PlatformView.SetImageBitmap(bitmap);
                    System.Diagnostics.Debug.WriteLine($"CustomImageHandler: Imagen establecida correctamente (Width={bitmap.Width}, Height={bitmap.Height})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CustomImageHandler: BitmapFactory.DecodeResource retornó null");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomImageHandler", "UpdateImageFromResourceName");
                System.Diagnostics.Debug.WriteLine($"CustomImageHandler ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        // Método opcional: para actualizar cuando cambie Source normal (si quieres mantener compatibilidad)
        public static void MapSource(CustomImageHandler handler, Image image)
        {
            // Si tiene el attached property, usamos UpdateImageFromResourceName
            var resourceName = ImageHelper.GetAndroidResourceName(image);
            if (!string.IsNullOrEmpty(resourceName))
            {
                handler.UpdateImageFromResourceName();
                return;
            }

            // Si no, delegar al comportamiento default de ImageHandler
            ImageHandler.MapSource(handler, image);
        }
    }
}
#endif

