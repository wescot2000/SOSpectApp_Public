using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.Models;
using sospect.ViewModels;
using sospect.Views.Popups;
using System;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProtectedSubscriptionPage : ContentPage
    {
        private ProtectedSubscriptionPageViewModel _viewModel;

        public ProtectedSubscriptionPage()
        {
            InitializeComponent();
            _viewModel = new ProtectedSubscriptionPageViewModel();
            BindingContext = _viewModel;
        }

        private async void OnRelationshipFrameTapped(object sender, EventArgs e)
        {
            if (_viewModel.RelationshipTypes == null || _viewModel.RelationshipTypes.Count == 0)
                return;

            var popup = new RelationshipPickerPopup(
                _viewModel.RelationshipTypes,
                _viewModel.SelectedRelationshipType
            );

            popup.RelationshipTypeSelectedEvent += (s, selectedRelationship) =>
            {
                _viewModel.SelectedRelationshipType = selectedRelationship;
            };

            await Application.Current.MainPage.ShowPopupAsync(popup);
        }
    }
}