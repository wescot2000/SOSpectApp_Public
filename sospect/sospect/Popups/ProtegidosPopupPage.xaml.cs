using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.Models;
using sospect.ViewModels;
using System.Windows.Input;
using Xamarin.Forms.Xaml;
using sospect.Views;



namespace sospect.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProtegidosPopupPage : PopupPage
    {
        public ProtegidosPopupPage()
        {
            InitializeComponent();

            BindingContext = new ProtegidosPopupPageViewModel();
        }
    }
}

