using CommunityToolkit.Maui.Views;
using sospect.ViewModels;

namespace sospect.Views.Popups
{
    public partial class ConfirmarWipeData : Popup
    {
        WipeDataPopupViewModel vm;

        public ConfirmarWipeData()
        {
            InitializeComponent();
            vm = new WipeDataPopupViewModel();
            BindingContext = vm;
        }
    }
}
