// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using sospect.Models;
using sospect.Helpers;
using sospect.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;

namespace sospect.ViewModels
{
    public class VerMapaPopupViewModel : BaseViewModel
    {
        public ICommand VerEnMapaCommand { get; }
        public ICommand IrAPuntoCommand { get; }
        public ICommand CerrarPopupCommand { get; }

        private readonly AlarmaCercana _alarmaCercana;
        private readonly Popup _popup;

        // Constructor que recibe referencia al popup para poder cerrarlo
        public VerMapaPopupViewModel(AlarmaCercana alarmaCercana, Popup popup)
        {
            _alarmaCercana = alarmaCercana;
            _popup = popup;
            VerEnMapaCommand = new Command(OnVerEnMapa);
            IrAPuntoCommand = new Command(OnIrAPunto);
            CerrarPopupCommand = new Command(OnCerrarPopup);
        }

        private async void OnVerEnMapa()
        {
            bool popupCerrado = false;
            try
            {
                IsRunning = true;

                // Cerrar el popup primero
                if (_popup != null)
                {
                    try
                    {
                        await _popup.CloseAsync();
                        popupCerrado = true;
                        System.Diagnostics.Debug.WriteLine("VerMapaPopupViewModel: Popup cerrado exitosamente");
                    }
                    catch (Exception closeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Error al cerrar popup: {closeEx.Message}");
                        // Continuar aunque falle el cierre del popup
                    }
                }

                // CORRECCIÓN: Crear nueva HomePage con solo la alarma seleccionada (como en Xamarin)
                // Esto asegura que solo se muestre la alarma específica en el mapa
                var alarmas = new ObservableCollection<AlarmaCercana> { _alarmaCercana };
                var homePage = new HomePage(alarmas);

                // CORRECCIÓN: Obtener Navigation del tab actual en TabbedPage
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Navegando a HomePage con alarma {_alarmaCercana.alarma_id}");
                    await navigation.PushAsync(homePage);
                    System.Diagnostics.Debug.WriteLine("VerMapaPopupViewModel: Navegación completada");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VerMapaPopupViewModel: Navigation no disponible");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Error en OnVerEnMapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "VerMapaPopupViewModel", "OnVerEnMapa");

                // Solo intentar cerrar el popup si no se cerró antes
                if (!popupCerrado && _popup != null)
                {
                    try
                    {
                        await _popup.CloseAsync();
                    }
                    catch
                    {
                        // Ignorar errores al cerrar el popup en caso de error
                    }
                }

                // Mostrar mensaje de error al usuario
                try
                {
                    var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                    var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                    await ModernAlerts.ShowWarning(LabelInformacion, $"{MensajeError}");
                }
                catch
                {
                    // Si incluso el mensaje de error falla, no hacer nada
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void OnIrAPunto()
        {
            bool popupCerrado = false;
            try
            {
                IsRunning = true;
                var LblElegirApp = await TranslateExtension.TranslateAsync("LblElegirApp");
                var LblMapas = await TranslateExtension.TranslateAsync("LblMapas");

                double latitud = (double)_alarmaCercana.latitud_alarma;
                double longitud = (double)_alarmaCercana.longitud_alarma;
                // InvariantCulture garantiza punto decimal en las URLs (evita coma por cultura es/CO)
                string latStr = latitud.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = longitud.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lugar = $"{latStr},{lonStr}";
                string googleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={lugar}";
                string appleMapsUrl = $"http://maps.apple.com/?daddr={lugar}";
                // iOS requiere el scheme nativo waze:// para que la app reciba las coordenadas destino
                string wazeUrl = DeviceInfo.Platform == DevicePlatform.iOS
                    ? $"waze://?ll={latStr},{lonStr}&navigate=yes"
                    : $"https://waze.com/ul?ll={latStr},{lonStr}&navigate=yes";

                Console.WriteLine($"[WAZE-DIAG] Platform={DeviceInfo.Platform}, Lat={latStr}, Lon={lonStr}");
                Console.WriteLine($"[WAZE-DIAG] wazeUrl construido: {wazeUrl}");

                // Verificar si Waze puede abrirse antes de mostrar el popup
                bool wazeDisponible = await Launcher.CanOpenAsync(wazeUrl);
                Console.WriteLine($"[WAZE-DIAG] Launcher.CanOpenAsync(wazeUrl)={wazeDisponible}");

                // Cerrar popup ANTES de mostrar el popup de confirmación
                if (_popup != null)
                {
                    await _popup.CloseAsync();
                    popupCerrado = true;
                }

                // Mostrar popup elegante de confirmación usando ModernAlerts
                bool usarWaze = await ModernAlerts.ShowConfirmation(
                    LblElegirApp,
                    await TranslateExtension.TranslateAsync("LblSeleccioneAppNavegacion"),
                    "Waze",
                    LblMapas,
                    false
                );

                Console.WriteLine($"[WAZE-DIAG] Usuario seleccionó: {(usarWaze ? "Waze" : "Maps")}");

                if (!usarWaze)
                {
                    // Usuario eligió Maps
                    try
                    {
                        if (DeviceInfo.Platform == DevicePlatform.Android)
                        {
                            Console.WriteLine($"[WAZE-DIAG] Abriendo Google Maps: {googleMapsUrl}");
                            await Launcher.OpenAsync(googleMapsUrl);
                        }
                        else if (DeviceInfo.Platform == DevicePlatform.iOS)
                        {
                            Console.WriteLine($"[WAZE-DIAG] Abriendo Apple Maps: {appleMapsUrl}");
                            await Launcher.OpenAsync(appleMapsUrl);
                        }
                        Console.WriteLine("[WAZE-DIAG] Maps abierto exitosamente");
                    }
                    catch (Exception mapsEx)
                    {
                        Console.WriteLine($"[WAZE-DIAG] Error al abrir Maps: {mapsEx.Message}");
                        CrashlyticsHelper.LogError(mapsEx, "VerMapaPopupViewModel", "OnIrAPunto-Maps");
                    }
                }
                else
                {
                    // Usuario eligió Waze - intentar abrir
                    try
                    {
                        Console.WriteLine($"[WAZE-DIAG] Intentando abrir Waze con URL: {wazeUrl}");
                        await Launcher.OpenAsync(wazeUrl);
                        Console.WriteLine("[WAZE-DIAG] Waze abierto exitosamente");
                    }
                    catch (Exception wazeEx)
                    {
                        Console.WriteLine($"[WAZE-DIAG] Error abriendo Waze: {wazeEx.GetType().Name} - {wazeEx.Message}");
                        CrashlyticsHelper.LogError(wazeEx, "VerMapaPopupViewModel", "OnIrAPunto-Waze");

                        // Waze no está instalado, abrir Maps como fallback SIN mostrar popup
                        try
                        {
                            Console.WriteLine("[WAZE-DIAG] Waze no disponible, usando Maps como fallback");
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                await Launcher.OpenAsync(googleMapsUrl);
                            }
                            else if (DeviceInfo.Platform == DevicePlatform.iOS)
                            {
                                await Launcher.OpenAsync(appleMapsUrl);
                            }
                            Console.WriteLine("[WAZE-DIAG] Maps fallback abierto exitosamente");
                        }
                        catch (Exception mapsFallbackEx)
                        {
                            Console.WriteLine($"[WAZE-DIAG] Error Maps fallback: {mapsFallbackEx.Message}");
                            CrashlyticsHelper.LogError(mapsFallbackEx, "VerMapaPopupViewModel", "OnIrAPunto-MapsFallback");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Error en OnIrAPunto: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "VerMapaPopupViewModel", "OnIrAPunto");

                // Solo intentar cerrar el popup si no se cerró antes
                if (!popupCerrado && _popup != null)
                {
                    try
                    {
                        await _popup.CloseAsync();
                    }
                    catch
                    {
                        // Ignorar errores al cerrar el popup
                    }
                }

                // NO mostrar popup de error en catch general
                // Esto evita PopupBlockedException cuando la app regresa del background
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void OnCerrarPopup()
        {
            try
            {
                if (_popup != null)
                {
                    await _popup.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Error al cerrar popup: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "VerMapaPopupViewModel", "OnCerrarPopup");
            }
        }

        // CORRECCIÓN: Helper method para obtener Navigation desde TabbedPage
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (App.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar App.Current.MainPage.Navigation si está disponible
                if (App.Current?.MainPage?.Navigation != null)
                {
                    return App.Current.MainPage.Navigation;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerMapaPopupViewModel: Error obteniendo Navigation: {ex.Message}");
                return null;
            }
        }
    }
}

