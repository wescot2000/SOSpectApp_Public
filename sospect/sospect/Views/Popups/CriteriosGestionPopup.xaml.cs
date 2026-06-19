// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Views;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class CriteriosGestionPopup : Popup
    {
        public CriteriosGestionPopup()
        {
            InitializeComponent();
        }

        private async void OnCerrarTapped(object sender, EventArgs e)
        {
            try
            {
                await CloseAsync();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CriteriosGestionPopup", "OnCerrarTapped");
            }
        }
    }
}


