using System;
using Microsoft.Maui.Controls;

namespace sospect.Views
{
    public partial class ConfirmationPage : ContentPage
    {
        public event EventHandler<bool> ConfirmationResult;

        public ConfirmationPage(string message)
        {
            InitializeComponent();
            ConfirmationMessageLabel.Text = message;
        }

        private void OnYesButtonTapped(object sender, EventArgs e)
        {
            ConfirmationResult?.Invoke(this, true);
        }

        private void OnNoButtonTapped(object sender, EventArgs e)
        {
            ConfirmationResult?.Invoke(this, false);
        }
    }
}