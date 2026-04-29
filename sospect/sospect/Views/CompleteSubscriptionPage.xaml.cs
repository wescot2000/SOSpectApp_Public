using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CompleteSubscriptionPage : ContentPage
    {
        private readonly CompleteSubscriptionPageViewModel _viewModel;

        public CompleteSubscriptionPage()
        {
            InitializeComponent();
            _viewModel = new CompleteSubscriptionPageViewModel();
            BindingContext = _viewModel;
        }
    }
}
