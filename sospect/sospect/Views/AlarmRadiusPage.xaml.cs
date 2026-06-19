// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
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


