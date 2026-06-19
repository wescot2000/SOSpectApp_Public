// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;

namespace sospect.ViewModels
{
    public class TutorialOverlayViewModel : INotifyPropertyChanged
    {
        private List<TutorialStep> _tutorialSteps;
        private int _currentStepIndex = 0;
        private string _currentStepTitle;
        private string _currentStepDescription;
        private string _nextText;
        private string _previousText;
        private string _skipText;
        private string _currentImageSource;
        private string _highlightImageSource;

        public event PropertyChangedEventHandler PropertyChanged;

        public TutorialOverlayViewModel()
        {
            InitializeTutorialSteps();
            _ = LoadTranslationsAsync();
        }

        private void InitializeTutorialSteps()
        {
            _tutorialSteps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    StepNumber = 1,
                    TitleKey = "LabelParaQueSirveSospect", 
                    DescriptionKey = "LblHolaBienvenido", 
                    HighlightImageSource = null 
                },
                new TutorialStep
                {
                    StepNumber = 2,
                    TitleKey = "LblAspectosBasicos",
                    DescriptionKey = "LblParaIniciar",
                    HighlightImageSource = "refresh_map_highlight.png"
                },
                new TutorialStep
                {
                    StepNumber = 3,
                    TitleKey = "LblAspectosBasicos",
                    DescriptionKey = "LblDescripcionMapa",
                    HighlightImageSource = "tutorial_map_highlight.png" 
                },
                new TutorialStep
                {
                    StepNumber = 4,
                    TitleKey = "LblEnviaUnaAlerta",
                    DescriptionKey = "LblAlarmaNoGrave",
                    HighlightImageSource = "tutorial_orange_button.png" 
                },
                new TutorialStep
                {
                    StepNumber = 5,
                    TitleKey = "LblReportaUnCrimen",
                    DescriptionKey = "LblCrimenCometido",
                    HighlightImageSource = "tutorial_red_button.png" 
                },
                new TutorialStep
                {
                    StepNumber = 6,
                    TitleKey = "LblExplicaLoSucedido",
                    DescriptionKey = "LblDescripcionAlarmas",
                    HighlightImageSource = "tutorial_describe_button.png" 
                },

                new TutorialStep
                {
                    StepNumber = 7,
                    TitleKey = "LblRevisaTusNotificaciones",
                    DescriptionKey = "LblDescripcionMensajes",
                    HighlightImageSource = "tutorial_messages_button.png"
                },
                new TutorialStep
                {
                    StepNumber = 8,
                    TitleKey = "LblAdministraTusConfiguraciones",
                    DescriptionKey = "LblDescripcionMenu",
                    HighlightImageSource = "tutorial_menu_button.png"
                },
                new TutorialStep
                {
                    StepNumber = 9,
                    TitleKey = "LblPorSiFueraPoco",
                    DescriptionKey = "LblRecomendaciones",
                    HighlightImageSource = null 
                },
                new TutorialStep
                {
                    StepNumber = 10,
                    TitleKey = "LblMuyImportante",
                    DescriptionKey = "LblRecomendacionImportante",
                    HighlightImageSource = null
                },
                new TutorialStep
                {
                    StepNumber = 11,
                    TitleKey = "LblParaFinalizar",
                    DescriptionKey = "LblPromocion",
                    HighlightImageSource = "promocioninicial.png"
                }
            };
        }

        private async Task LoadTranslationsAsync()
        {
            try
            {

                NextText = await TranslateExtension.TranslateAsync("LabelSiguiente") ?? "Siguiente";
                PreviousText = await TranslateExtension.TranslateAsync("LabelAnterior") ?? "Anterior";
                SkipText = await TranslateExtension.TranslateAsync("LabelOmitir") ?? "Omitir";

                await LoadCurrentStepContentAsync();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading translations: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayViewModel", "LoadTranslationsAsync");

                NextText = "Siguiente";
                PreviousText = "Anterior";
                SkipText = "Omitir";
                CurrentStepTitle = "Tutorial SOSpect";
                CurrentStepDescription = "Aprende a usar la aplicación paso a paso.";
            }
        }

        private async Task LoadCurrentStepContentAsync()
        {
            if (_tutorialSteps == null || !_tutorialSteps.Any() || _currentStepIndex >= _tutorialSteps.Count)
                return;

            var currentStep = _tutorialSteps[_currentStepIndex];

            try
            {
                CurrentStepTitle = await TranslateExtension.TranslateAsync(currentStep.TitleKey) ?? currentStep.TitleKey;
                CurrentStepDescription = await TranslateExtension.TranslateAsync(currentStep.DescriptionKey) ?? currentStep.DescriptionKey;
                CurrentImageSource = currentStep.BackgroundImageSource;
                HighlightImageSource = currentStep.HighlightImageSource;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading step content: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TutorialOverlayViewModel", "LoadCurrentStepContentAsync");
                CurrentStepTitle = "Tutorial";
                CurrentStepDescription = "Contenido del tutorial";
            }

            // Actualizar textos de botones según el paso
            if (_currentStepIndex == _tutorialSteps.Count - 1)
            {
                NextText = await TranslateExtension.TranslateAsync("LabelFinalizar") ?? "Finalizar";
            }
            else
            {
                NextText = await TranslateExtension.TranslateAsync("LabelSiguiente") ?? "Siguiente";
            }

            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CurrentStepNumber));
            OnPropertyChanged(nameof(TotalSteps));
            OnPropertyChanged(nameof(HasHighlightImage));
        }

        // Propiedades públicas
        public string CurrentStepTitle
        {
            get => _currentStepTitle;
            set
            {
                _currentStepTitle = value;
                OnPropertyChanged();
            }
        }

        public string CurrentStepDescription
        {
            get => _currentStepDescription;
            set
            {
                _currentStepDescription = value;
                OnPropertyChanged();
            }
        }

        public string NextText
        {
            get => _nextText;
            set
            {
                _nextText = value;
                OnPropertyChanged();
            }
        }

        public string PreviousText
        {
            get => _previousText;
            set
            {
                _previousText = value;
                OnPropertyChanged();
            }
        }

        public string SkipText
        {
            get => _skipText;
            set
            {
                _skipText = value;
                OnPropertyChanged();
            }
        }

        public string CurrentImageSource
        {
            get => _currentImageSource;
            set
            {
                _currentImageSource = value;
                OnPropertyChanged();
            }
        }

        public string HighlightImageSource
        {
            get => _highlightImageSource;
            set
            {
                _highlightImageSource = value;
                OnPropertyChanged();
            }
        }

        public bool HasHighlightImage => !string.IsNullOrEmpty(HighlightImageSource);

        public bool CanGoPrevious => _currentStepIndex > 0;

        public int CurrentStepNumber => _currentStepIndex + 1;

        public int TotalSteps => _tutorialSteps?.Count ?? 0;

        // Métodos de navegación
        public async Task<bool> GoToNextStepAsync()
        {
            if (_currentStepIndex < _tutorialSteps.Count - 1)
            {
                _currentStepIndex++;
                await LoadCurrentStepContentAsync();
                return true;
            }
            return false; // Último paso alcanzado
        }

        public async Task GoToPreviousStepAsync()
        {
            if (_currentStepIndex > 0)
            {
                _currentStepIndex--;
                await LoadCurrentStepContentAsync();
            }
        }

        public void UpdateProgressIndicators(Frame[] progressFrames)
        {
            if (progressFrames == null || progressFrames.Length == 0)
                return;

            for (int i = 0; i < progressFrames.Length; i++)
            {
                if (progressFrames[i] != null)
                {
                    progressFrames[i].BackgroundColor = i <= _currentStepIndex
                        ? Color.FromHex("#60A5FA")  // Azul más suave para pasos completados/actual
                        : Color.FromHex("#E5E7EB"); // Gris muy claro para pasos pendientes
                }
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

