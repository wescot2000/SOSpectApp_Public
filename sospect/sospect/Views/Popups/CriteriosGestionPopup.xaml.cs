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
