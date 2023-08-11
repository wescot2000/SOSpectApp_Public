using System;
using System.Collections.Generic;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.Views
{
    public partial class DescribirPage : ContentPage
    {
        long? alarmaIdLocal;

        public DescribirPage()
        {
            InitializeComponent();
            BindingContext = new DescribirAlarmaViewModel(null);
        }

        public DescribirPage(long? alarmaId = null)
        {
            alarmaIdLocal = alarmaId;
            InitializeComponent();
            BindingContext = new DescribirAlarmaViewModel(alarmaId);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (alarmaIdLocal == null)
            {
                if (string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) || Preferences.Get("alarma_id", "") == "0")
                {
                    if (BindingContext is DescribirAlarmaViewModel viewmodel)
                    {
                        await viewmodel.ObtenerAlarmas();
                    }
                }
                else
                {
                    return;
                }
            }
        }
    }
}

