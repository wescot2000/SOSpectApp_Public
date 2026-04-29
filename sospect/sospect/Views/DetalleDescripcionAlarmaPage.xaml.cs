using System;
using System.Linq;
using System.Threading.Tasks;
using sospect.Models;
using sospect.ViewModels;
using sospect.Services;
using Microsoft.Maui.Controls;
using sospect.Views.Popups;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class DetalleDescripcionAlarmaPage : ContentPage
    {
        DetalleDescripcionAlarmaViewModel viewModel;
        private System.Timers.Timer refreshTimer;

        public DetalleDescripcionAlarmaPage(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();

            // CRÍTICO: Detectar alarmas promocionales con chat privado habilitado
            // En lugar de abrir el chat comunitario, debe ir a ChatPublicidadPage
            if (alarmaCercana.is_advertising && alarmaCercana.publicidad_contacto_habilitado)
            {
                System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Alarma {alarmaCercana.alarma_id} es promocional con chat habilitado - delegando a ViewModel");

                // PATRÓN MVVM: Crear ViewModel y usar el comando
                viewModel = new DetalleDescripcionAlarmaViewModel(alarmaCercana);
                BindingContext = viewModel;

                // Ejecutar navegación a chat usando el ViewModel
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Usar el ICommand del ViewModel para manejar la navegación
                        if (viewModel.NavegearAChatPublicidadCommand.CanExecute(alarmaCercana))
                        {
                            viewModel.NavegearAChatPublicidadCommand.Execute(alarmaCercana);
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaPage", "Constructor_ChatPromocional");
                        await DisplayAlert("Error", $"Error al verificar chat: {ex.Message}", "OK");
                        await Navigation.PopAsync();
                    }
                });

                return; // No continuar con la inicialización del chat comunitario
            }

            // Flujo normal para alarmas no promocionales (chat comunitario)
            viewModel = new DetalleDescripcionAlarmaViewModel(alarmaCercana);
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // R10: Si la alarma tiene votación activa, redirigir transparentemente.
            // DEFENSA CONTRA RACE CONDITION: Si TieneVotacionActiva=false en caché (datos viejos),
            // verificar con la API antes de mostrar el chat comunitario.
            // - Propietario de la alarma → VerHistorialAlarmaPage (solo lectura)
            // - Otros usuarios           → CierreEncuestaPage (para votar)
            if (viewModel?.AlarmaSeleccionada != null)
            {
                bool tieneVotacion = viewModel.AlarmaSeleccionada.TieneVotacionActiva;

                // Si el caché dice que NO hay votación, verificar en la API para descartar race condition
                if (!tieneVotacion)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Verificando votación activa en API para alarma {viewModel.AlarmaSeleccionada.alarma_id}...");
                        var respuesta = await ApiService.ObtenerSolicitudCierreActiva(
                            viewModel.AlarmaSeleccionada.alarma_id,
                            App.persona.user_id_thirdparty);

                        if (respuesta?.IsSuccess == true && respuesta.Data != null)
                        {
                            // La API confirma que hay una solicitud activa — el caché estaba desactualizado
                            tieneVotacion = true;
                            viewModel.AlarmaSeleccionada.TieneVotacionActiva = true;
                            System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] API confirmó votación activa para alarma {viewModel.AlarmaSeleccionada.alarma_id} (caché estaba desactualizado)");

                            // Actualizar también el caché global para que otros puntos de entrada sean consistentes
                            ActualizarVotacionEnCache(viewModel.AlarmaSeleccionada.alarma_id);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] API confirma: sin votación activa para alarma {viewModel.AlarmaSeleccionada.alarma_id}");
                        }
                    }
                    catch (Exception exApi)
                    {
                        // No bloquear la carga del chat si la verificación falla — degradar gracefully
                        System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Error verificando votación (se carga chat normalmente): {exApi.Message}");
                        CrashlyticsHelper.LogError(exApi, "DetalleDescripcionAlarmaPage", "VerificarVotacionActiva");
                    }
                }

                if (tieneVotacion)
                {
                    try
                    {
                        ContentPage redirectPage;
                        if (viewModel.AlarmaSeleccionada.flag_propietario_alarma)
                        {
                            System.Diagnostics.Debug.WriteLine("[DetalleDescripcion] TieneVotacionActiva + propietario -> VerHistorialAlarmaPage");
                            redirectPage = new VerHistorialAlarmaPage(viewModel.AlarmaSeleccionada);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DetalleDescripcion] TieneVotacionActiva + no propietario -> CierreEncuestaPage");
                            redirectPage = new CierreEncuestaPage(viewModel.AlarmaSeleccionada);
                        }
                        Navigation.InsertPageBefore(redirectPage, this);
                        await Navigation.PopAsync(animated: false);
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaPage", "RedirectVotacion");
                        System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Redirect error: {ex.Message}");
                    }
                    return;
                }
            }

            // Iniciar con la carga de los datos inicialmente.
            await RecargarYDesplazar();

            MessagingCenter.Subscribe<App>(this, "DescripcionesActualizadas", async (sender) =>
            {
                await RecargarYDesplazar();
            });

            // Configurar y empezar el timer para refrescos automáticos.
            ConfigurarTimer();
        }

        private async Task RecargarYDesplazar()
        {
            try
            {
                await viewModel.RecargarListadoDescripciones();

                // Usar MainThread para operaciones de UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Pequeño delay para asegurar que la UI se ha actualizado
                    await Task.Delay(300);

                    if (viewModel.ListadoDescripcionesAlarmas.Any())
                    {
                        var lastItem = viewModel.ListadoDescripcionesAlarmas.LastOrDefault();
                        if (lastItem != null)
                        {
                            DescripcionesAlarmaListView.ScrollTo(lastItem, ScrollToPosition.End, animated: true);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaPage", "RecargarYDesplazar");
                System.Diagnostics.Debug.WriteLine($"Error en RecargarYDesplazar: {ex.Message}");
            }
        }

        private void ConfigurarTimer()
        {
            refreshTimer = new System.Timers.Timer(30000); // 30 segundos
            refreshTimer.Elapsed += async (sender, e) =>
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(RecargarYDesplazar);
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaPage", "ConfigurarTimer");
                    System.Diagnostics.Debug.WriteLine($"Error en timer: {ex.Message}");
                }
            };
            refreshTimer.AutoReset = true;
            refreshTimer.Enabled = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Cleanup del timer
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            MessagingCenter.Unsubscribe<App>(this, "DescripcionesActualizadas");
        }

        /// <summary>
        /// Actualiza TieneVotacionActiva=true en los cachés globales para que los otros
        /// puntos de entrada (feed, mapa) también redirijan correctamente en el futuro.
        /// </summary>
        private static void ActualizarVotacionEnCache(long alarmaId)
        {
            try
            {
                if (App.AlarmasCacheadas != null)
                {
                    var enCache = App.AlarmasCacheadas.FirstOrDefault(a => a.alarma_id == alarmaId);
                    if (enCache != null)
                    {
                        enCache.TieneVotacionActiva = true;
                        System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Cache A: alarma {alarmaId} TieneVotacionActiva=true actualizado");
                    }
                }

                if (App.AlarmasCacheadasParaTi != null)
                {
                    var enCacheParaTi = App.AlarmasCacheadasParaTi.FirstOrDefault(a => a.alarma_id == alarmaId);
                    if (enCacheParaTi != null)
                    {
                        enCacheParaTi.TieneVotacionActiva = true;
                        System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Cache B (ParaTi): alarma {alarmaId} TieneVotacionActiva=true actualizado");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] Error actualizando caché: {ex.Message}");
            }
        }
    }
}
