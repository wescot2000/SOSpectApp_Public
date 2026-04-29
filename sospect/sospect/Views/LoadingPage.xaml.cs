using System;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.ViewModels;

namespace sospect.Views
{
    public partial class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("LoadingPage: Creada para mostrar splash personalizado");
        }

        protected override bool OnBackButtonPressed()
        {
            // Evitar que el usuario pueda navegar hacia atrás durante la carga
            return true;
        }
    }
}