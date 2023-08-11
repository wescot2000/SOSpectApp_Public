using sospect.Models;
using sospect.ViewModels;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProtectedSubscriptionPage : ContentPage
    {
        public ProtectedSubscriptionPage()
        {
            InitializeComponent();
            BindingContext = new ProtectedSubscriptionPageViewModel();
        }

        private void OnRelationshipPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            var selectedRelationshipType = picker.SelectedItem as RelationshipType;
            if (BindingContext is ProtectedSubscriptionPageViewModel viewModel)
            {
                viewModel.SelectedRelationshipType = selectedRelationshipType;
            }
        }
    }
}
