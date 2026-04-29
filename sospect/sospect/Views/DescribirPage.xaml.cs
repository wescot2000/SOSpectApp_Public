using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sospect.Models;
using sospect.Selectors;
using sospect.ViewModels;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class DescribirPage : ContentPage
    {
        private DescribirAlarmaViewModel _viewModel;
        private bool _isInitialized = false;
        long? alarmaIdLocal;

        // OPTIMIZACIÓN: Debouncer para evitar múltiples re-filtrados cuando llegan varios mensajes de caché
        private readonly DebounceHelper _cacheUpdateDebouncer = new DebounceHelper();

        // SOLUCI�N: Constructor por defecto sin inicializaci�n inmediata del ViewModel
        public DescribirPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("DescribirPage: Constructor por defecto iniciado sin ViewModel inmediato");

            // CR�TICO: No crear ViewModel aqu� para evitar crash durante startup
            // Se crear� de forma diferida en OnAppearing
        }

        // Constructor con par�metro de alarma espec�fica
        public DescribirPage(long? alarmaId = null)
        {
            alarmaIdLocal = alarmaId;
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine($"DescribirPage: Constructor con alarmaId={alarmaId} iniciado");

            // CR�TICO: Solo para alarmas espec�ficas crear ViewModel inmediatamente
            // porque estas p�ginas no se cargan durante el startup de SospectTabs
            if (alarmaId.HasValue)
            {
                BindingContext = new DescribirAlarmaViewModel(alarmaId);
                _isInitialized = true;
            }
        }

        protected override async void OnAppearing()
        {
            App.DescribirPageActiva = true;
            System.Diagnostics.Debug.WriteLine("[DescribirPage] DescribirPageActiva = true (suspendiendo refrescos de API)");

            base.OnAppearing();

            // Suscribirse a notificaciones de alarma lanzada y caché actualizado
            SuscribirMensajes();

            // Inicializar ViewModel solo cuando la página sea visible (primera vez)
            if (!_isInitialized)
            {
                await InitializeViewModelAsync();
            }
            else
            {
                // SIMPLIFICADO: Siempre re-filtrar desde caché cuando la página aparece
                // Esto asegura que cualquier cambio en App.AlarmasCacheadas se refleje
                // ObtenerAlarmas() lee de App.AlarmasCacheadas (NO llama API), es rápido
                if (alarmaIdLocal == null && BindingContext is DescribirAlarmaViewModel viewmodel)
                {
                    System.Diagnostics.Debug.WriteLine("[DescribirPage] OnAppearing - Re-filtrando desde caché");
                    await viewmodel.ObtenerAlarmas();
                }
            }
        }

        private void SuscribirMensajes()
        {
            // Desuscribir primero para evitar duplicados
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmaLanzadaExitosamente");
            MessagingCenter.Unsubscribe<ViewModels.LanzarAlarmaViewModel, string>(this, "AlarmaLanzadaExitosamente");
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmasCacheActualizadas");
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmaLanzada_RefrescarDescribir");

            // Suscribirse a cuando se lanza una alarma exitosamente
            MessagingCenter.Subscribe<object, string>(this, "AlarmaLanzadaExitosamente", async (sender, mensaje) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[DescribirPage] Recibido AlarmaLanzadaExitosamente - refrescando listado");

                    // Esperar un momento para que el API procese la alarma
                    await Task.Delay(2500);

                    // Refrescar desde API para obtener la alarma nueva con sus flags correctos
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (BindingContext is DescribirAlarmaViewModel viewModel)
                        {
                            // Forzar refresh desde API
                            bool success = await App.RefrescarAlarmasDesdeAPI();
                            if (success)
                            {
                                await viewModel.ObtenerAlarmas();
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DescribirPage] Error en AlarmaLanzadaExitosamente: {ex.Message}");
                }
            });

            // FIX 2026-02-27: Suscribirse al mensaje de refresco desde caché tras lanzar alarma propia.
            // A diferencia de AlarmaLanzadaExitosamente (que llama a la API), este solo re-filtra
            // las alarmas ya cacheadas. La alarma propia ya tiene flag_visible_siguiendo=true
            // después del fix en InsertarAlarmaEnCacheLocal.
            MessagingCenter.Subscribe<object, string>(this, "AlarmaLanzada_RefrescarDescribir", async (sender, _) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[DescribirPage] Recibido AlarmaLanzada_RefrescarDescribir - re-filtrando desde caché");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (BindingContext is DescribirAlarmaViewModel viewModel)
                        {
                            await viewModel.ObtenerAlarmas();
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DescribirPage] Error en AlarmaLanzada_RefrescarDescribir: {ex.Message}");
                }
            });

            // Suscribirse a cuando el caché se actualiza en background
            // OPTIMIZADO: Usa debounce de 1 segundo para evitar múltiples re-filtrados
            MessagingCenter.Subscribe<object, string>(this, "AlarmasCacheActualizadas", async (sender, mensaje) =>
            {
                try
                {
                    // OPTIMIZACIÓN: Debounce de 1 segundo para colapsar múltiples actualizaciones
                    await _cacheUpdateDebouncer.DebounceAsync(async () =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[DescribirPage] AlarmasCacheActualizadas (debounced) - re-filtrando listado");

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (BindingContext is DescribirAlarmaViewModel viewModel)
                            {
                                await viewModel.ObtenerAlarmas();
                            }
                        });
                    }, 1000); // 1 segundo de debounce
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DescribirPage] Error en AlarmasCacheActualizadas: {ex.Message}");
                }
            });
        }

        // SOLUCI�N: Inicializaci�n diferida y segura del ViewModel
        private async Task InitializeViewModelAsync()
        {
            try
            {
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("DescribirPage: Inicializando ViewModel de forma diferida");

                // Crear ViewModel solo cuando la p�gina est� completamente cargada
                _viewModel = new DescribirAlarmaViewModel(null);
                BindingContext = _viewModel;

                // Aplicar l�gica existente de carga de datos
                if (alarmaIdLocal == null)
                {
                    if (string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) || Preferences.Get("alarma_id", "") == "0")
                    {
                        await _viewModel.ObtenerAlarmas();
                    }
                }

                System.Diagnostics.Debug.WriteLine("DescribirPage: ViewModel inicializado exitosamente con l�gica existente");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirPage", "InitializeViewModelAsync");
                System.Diagnostics.Debug.WriteLine($"DescribirPage: Error inicializando ViewModel: {ex.Message}");

                // Crear ViewModel b�sico como fallback
                _viewModel = new DescribirAlarmaViewModel(null);
                BindingContext = _viewModel;
            }
        }

        protected override void OnDisappearing()
        {
            // Patrón Twitter/X: al salir del feed, reanudar refrescos automáticos de API
            App.DescribirPageActiva = false;
            System.Diagnostics.Debug.WriteLine("[DescribirPage] DescribirPageActiva = false (reanudando refrescos de API)");

            try
            {
                // OPTIMIZACIÓN: Cancelar debouncer pendiente
                _cacheUpdateDebouncer.Cancel();

                base.OnDisappearing();
                System.Diagnostics.Debug.WriteLine("DescribirPage: OnDisappearing llamado");

                // Desuscribir mensajes para evitar memory leaks
                DesuscribirMensajes();

                // SOLUCI�N: Limpieza segura para evitar ObjectDisposedException
                if (BindingContext is DescribirAlarmaViewModel viewModel)
                {
                    // Detener cualquier operaci�n en curso
                    viewModel.IsRunning = false;
                }
            }
            catch (ObjectDisposedException ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirPage", "OnDisappearing");
                System.Diagnostics.Debug.WriteLine($"DescribirPage: ObjectDisposedException manejada en OnDisappearing: {ex.Message}");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirPage", "OnDisappearing");
                System.Diagnostics.Debug.WriteLine($"DescribirPage: Error en OnDisappearing: {ex.Message}");
            }
        }

        private void DesuscribirMensajes()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmaLanzadaExitosamente");
            MessagingCenter.Unsubscribe<ViewModels.LanzarAlarmaViewModel, string>(this, "AlarmaLanzadaExitosamente");
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmasCacheActualizadas");
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmaLanzada_RefrescarDescribir");
        }

        // M�todo p�blico para forzar actualizaci�n desde otras p�ginas
        public async Task RefreshDataAsync()
        {
            if (BindingContext is DescribirAlarmaViewModel viewModel)
            {
                await viewModel.ObtenerAlarmas();
            }
        }

        // Handler de scroll del feed — delega al ViewModel para ocultar/mostrar barras (estilo X/Twitter)
        private void OnFeedScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            (BindingContext as DescribirAlarmaViewModel)?.OnFeedScrolled(e.VerticalOffset);
        }
    }
}