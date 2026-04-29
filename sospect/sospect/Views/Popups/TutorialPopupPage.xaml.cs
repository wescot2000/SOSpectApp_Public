using sospect.ViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;

namespace sospect.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TutorialPopupPage : Popup
    {
        private TapGestureRecognizer tapGestureRecognizer;
        public event EventHandler TutorialCompleted;

        public TutorialPopupPage()
        {
            InitializeComponent();
            // Agrega el TapGestureRecognizer al Grid
            tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnClose;
            ClickableGrid.GestureRecognizers.Add(tapGestureRecognizer);

            // Iniciar el timer para cerrar automáticamente
            _ = ClosePopupAfterDelay();
        }

        private async Task ClosePopupAfterDelay()
        {
            var viewModel = BindingContext as TutorialPopupPageViewModel;
            if (viewModel != null && viewModel.Delay > 0)
            {
                await Task.Delay(viewModel.Delay);
                await CloseAsync(); //  Usar CloseAsync() en lugar de Close()
            }
        }

        public event Action OnPopupClosed = delegate { };

        // Método para cerrar el popup cuando el usuario haga clic
        private async void OnClose(object sender, System.EventArgs e)
        {
            OnPopupClosed?.Invoke();
            await CloseAsync(); //  Usar CloseAsync() en lugar de Close()
        }

        public void CompleteTutorial()
        {
            TutorialCompleted?.Invoke(this, EventArgs.Empty);
        }

        // Método público para cerrar desde código externo
        public async Task ClosePopup()
        {
            OnPopupClosed?.Invoke();
            await CloseAsync();
        }
    }
}