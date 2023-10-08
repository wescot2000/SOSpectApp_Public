using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.ViewModels;  // Asegúrate de importar el espacio de nombres correcto
using Xamarin.Forms.Xaml;
using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;




namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TutorialPopupPage : PopupPage
    {
        public event EventHandler TutorialCompleted;
        public TutorialPopupPage()
        {
            InitializeComponent();

            // Aquí es donde estableces el DataContext de la página para que sea una nueva instancia de TutorialPopupPageViewModel
            this.BindingContext = new TutorialPopupPageViewModel();

            // Agrega el TapGestureRecognizer al Grid
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnClose;
            ClickableGrid.GestureRecognizers.Add(tapGestureRecognizer);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            OnPopupClosed();
        }

        public event Action OnPopupClosed = delegate { };

        // Puedes agregar un método para cerrar el pop-up cuando el usuario haga clic en él
        private async void OnClose(object sender, System.EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
        public void CompleteTutorial()
        {
            TutorialCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
