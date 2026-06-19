// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

