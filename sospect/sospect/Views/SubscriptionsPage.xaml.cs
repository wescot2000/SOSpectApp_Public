using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using sospect.ViewModels;
using sospect.Models;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class SubscriptionsPage : ContentPage
    {
        public SubscriptionsPage()
        {
            InitializeComponent();

            BindingContext = new SubscriptionsPageViewModel();
        }

    }
}