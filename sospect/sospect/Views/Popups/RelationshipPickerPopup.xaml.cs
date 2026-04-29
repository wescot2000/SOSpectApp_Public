using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Models;
using CommunityToolkit.Mvvm.Input;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class RelationshipPickerPopup : Popup
    {
        public ObservableCollection<RelationshipType> RelationshipTypes { get; set; }
        public RelationshipType RelationshipTypeSelected { get; set; }
        public ICommand SelectRelationshipCommand { get; }

        // Evento para notificar la selección
        public event EventHandler<RelationshipType> RelationshipTypeSelectedEvent;

        // Flag para evitar múltiples cierres
        private bool _isClosing = false;

        public RelationshipPickerPopup(ObservableCollection<RelationshipType> relationshipTypes, RelationshipType selectedType)
        {
            InitializeComponent();
            RelationshipTypes = relationshipTypes;
            RelationshipTypeSelected = selectedType;
            SelectRelationshipCommand = new RelayCommand<RelationshipType>(OnRelationshipTypeSelected);
            BindingContext = this;
        }

        private async void OnRelationshipTypeSelected(RelationshipType relationshipType)
        {
            if (relationshipType != null && !_isClosing)
            {
                _isClosing = true;
                RelationshipTypeSelected = relationshipType;

                // Disparar el evento antes de cerrar
                RelationshipTypeSelectedEvent?.Invoke(this, relationshipType);

                try
                {
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "RelationshipPickerPopup", "OnRelationshipTypeSelected");
                    System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
                }
            }
        }

        private async void OnCancelTapped(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                try
                {
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "RelationshipPickerPopup", "OnCancelTapped");
                    System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
                }
            }
        }
    }
}