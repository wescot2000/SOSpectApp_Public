using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using sospect.Helpers;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlarmRadiusPage : ContentPage
    {
        public AlarmRadiusViewModel ViewModel { get; }

        public AlarmRadiusPage()
        {
            InitializeComponent();
            ViewModel = new AlarmRadiusViewModel(Navigation);
            BindingContext = ViewModel;
        }

        void NewRadiusPicker_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            if (BindingContext is AlarmRadiusViewModel vm && vm.NewRadius != null)
            {
                var LblPoderesRequeridosZV = TranslateExtension.Translate("LblPoderesRequeridosZV");
                
                vm.RequiredPowersLabel = $"{LblPoderesRequeridosZV}: {vm.CalculateRequiredPowers(vm.NewRadius).ToString()}";
            }
        }
    }
}
