using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;
using sospect.Models;
using sospect.Helpers;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ListarMiRedConfianzaPage : ContentPage
    {
        public ListarMiRedConfianzaPage()
        {
            InitializeComponent();
            BindingContext = new ListarMiRedConfianzaPageViewModel();
        }
    }
}
