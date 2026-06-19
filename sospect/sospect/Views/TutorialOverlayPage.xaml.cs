// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.ViewModels;
using System;
using System.Threading.Tasks;

namespace sospect.Views
{
    public partial class TutorialOverlayPage : ContentPage
    {
        private TutorialOverlayViewModel _viewModel;
        private Frame[] _progressFrames;

        public TutorialOverlayPage()
        {
            InitializeComponent();

            // NOTA: El padding dinámico para status bar ya no es necesario.
            // MAUI 10 SafeAreaEdges="Container" (aplicado globalmente en App.xaml)
            // se encarga de respetar las barras del sistema automáticamente.

            _viewModel = new TutorialOverlayViewModel();
            BindingContext = _viewModel;

            // Inicializar array de indicadores de progreso
            _progressFrames = new Frame[]
            {
                Progress1, Progress2, Progress3, Progress4, Progress5,
                Progress6, Progress7, Progress8, Progress9, Progress10, Progress11
            };

            // Actualizar indicadores iniciales
            _viewModel.UpdateProgressIndicators(_progressFrames);
        }

        // Event handler para XAML (debe ser void)  
        private async void OnSkipClicked(object sender, EventArgs e)
        {
            await HandleSkipAsync();
        }

        // Método async Task para la lógica
        private async Task HandleSkipAsync()
        {
            try
            {
                // Mostrar confirmación antes de omitir
                var skipTitle = await TranslateExtension.TranslateAsync("LabelConfirmarOmitir") ?? "Omitir Tutorial";
                var skipMessage = await TranslateExtension.TranslateAsync("LabelMensajeOmitir") ??
                    "¿Estás seguro de que quieres omitir el tutorial? Puedes volver a verlo cerrando sesión y volviendo a iniciar.";
                var confirmText = await TranslateExtension.TranslateAsync("LabelSi") ?? "Sí";
                var cancelText = await TranslateExtension.TranslateAsync("LabelNo") ?? "No";

                bool shouldSkip = await ModernAlerts.ShowConfirmation(
                    skipTitle,
                    skipMessage,
                    confirmText,
                    cancelText,
                    false);

                if (shouldSkip)
                {
                    await ShowSkipInfoAndClose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en HandleSkipAsync: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "HandleSkipAsync");
                // En caso de error, permitir cerrar el tutorial
                await CloseTutorial();
            }
        }

        private async void OnScreenTapped(object sender, EventArgs e)
        {
            // Evitar que interfiera con otros elementos
            if (LoadingIndicator.IsRunning)
                return;

            // Ejecutar la misma lógica que el botón Next pero sin llamar al event handler
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                bool hasMoreSteps = await _viewModel.GoToNextStepAsync();
                _viewModel.UpdateProgressIndicators(_progressFrames);

                if (!hasMoreSteps)
                {
                    // Es el último paso, cerrar tutorial
                    await Task.Delay(500);
                    await FinishTutorial();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnScreenTapped: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "OnScreenTapped");
                // En caso de error, cerrar el tutorial
                await CloseTutorial();
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task ShowSkipInfoAndClose()
        {
            try
            {
                var infoTitle = await TranslateExtension.TranslateAsync("LabelInformacion") ?? "Información";
                var infoMessage = await TranslateExtension.TranslateAsync("LabelMensajeVolverVerTutorial") ??
                    "Si deseas volver a ver este tutorial, simplemente cierra sesión y vuelve a iniciar sesión en la aplicación.";

                await ModernAlerts.ShowInfo(infoTitle, infoMessage);
                await CloseTutorial();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando info de skip: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "ShowSkipInfoAndClose");
                await CloseTutorial();
            }
        }

        private async void OnPreviousClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                await _viewModel.GoToPreviousStepAsync();
                _viewModel.UpdateProgressIndicators(_progressFrames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnPreviousClicked: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "OnPreviousClicked");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                bool hasMoreSteps = await _viewModel.GoToNextStepAsync();
                _viewModel.UpdateProgressIndicators(_progressFrames);

                if (!hasMoreSteps)
                {
                    // Es el último paso, cerrar tutorial
                    System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: Último paso alcanzado");
                    await Task.Delay(300); // Breve pausa para que el usuario vea el cambio
                    await FinishTutorial();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnNextClicked: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "OnNextClicked");
                // En caso de error, cerrar el tutorial
                await CloseTutorial();
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task FinishTutorial()
        {
            try
            {
                // CRÍTICO: Marcar el tutorial como visto ANTES de mostrar el popup
                Microsoft.Maui.Storage.Preferences.Set("HasSeenTutorial", true);

                // Mostrar mensaje de finalización exitosa
                var congratsTitle = await TranslateExtension.TranslateAsync("LabelFelicitaciones") ?? "¡Felicitaciones!";
                var congratsMessage = await TranslateExtension.TranslateAsync("LabelTutorialCompletado") ??
                    "Has completado el tutorial de SOSpect. ¡Ahora estás listo para usar la aplicación de forma segura!";

                await ModernAlerts.ShowSuccess(congratsTitle, congratsMessage);

                // CRÍTICO: Cerrar el tutorial inmediatamente después de cerrar el popup
                await CloseTutorialInternal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finalizando tutorial: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "FinishTutorial");
                // Asegurar que se cierre incluso si hay error
                await CloseTutorialInternal();
            }
        }

        private async Task CloseTutorialInternal()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: CloseTutorialInternal iniciado");

                // CRÍTICO: Usar MainThread para operaciones de navegación
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Volver a la página principal
                        if (Navigation != null && Navigation.NavigationStack.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: Usando Navigation.PopAsync()");
                            await Navigation.PopAsync(animated: true);
                        }
                        else if (Application.Current?.MainPage is NavigationPage navPage && navPage.Navigation.NavigationStack.Count > 1)
                        {
                            System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: Usando navPage.PopAsync()");
                            await navPage.PopAsync(animated: true);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: Fallback - Reemplazando MainPage");
                            // Fallback: navegar directamente a HomePage
                            Application.Current.MainPage = new NavigationPage(new HomePage())
                            {
                                BarBackgroundColor = Colors.Black
                            };
                        }

                        System.Diagnostics.Debug.WriteLine("TutorialOverlayPage: Navegación exitosa");
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"TutorialOverlayPage: Error en navegación: {navEx.Message}");
                        CrashlyticsHelper.LogError(navEx, "TutorialOverlayPage", "CloseTutorialInternal-Navigation");

                        // Fallback de emergencia
                        try
                        {
                            Application.Current.MainPage = new NavigationPage(new HomePage())
                            {
                                BarBackgroundColor = Colors.Black
                            };
                        }
                        catch (Exception fallbackEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"TutorialOverlayPage: Error en fallback: {fallbackEx.Message}");
                            CrashlyticsHelper.LogError(fallbackEx, "TutorialOverlayPage", "CloseTutorialInternal-Fallback");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TutorialOverlayPage: Error cerrando tutorial: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "CloseTutorialInternal");
            }
        }

        private async Task CloseTutorial()
        {
            // Marcar el tutorial como visto
            Microsoft.Maui.Storage.Preferences.Set("HasSeenTutorial", true);

            // Usar el método interno para cerrar
            await CloseTutorialInternal();
        }

        protected override bool OnBackButtonPressed()
        {
            // Interceptar el botón de retroceso del dispositivo
            HandleBackButtonAsync();
            return true; // Prevenir el comportamiento por defecto
        }

        private async void HandleBackButtonAsync()
        {
            await HandleSkipAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Asegurar que los indicadores estén actualizados
                _viewModel?.UpdateProgressIndicators(_progressFrames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnAppearing: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "OnAppearing");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                // Cleanup si es necesario
                LoadingIndicator.IsRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnDisappearing: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayPage", "OnDisappearing");
            }
        }
    }
}

