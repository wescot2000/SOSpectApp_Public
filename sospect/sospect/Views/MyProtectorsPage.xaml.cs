using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MyProtectorsPage : ContentPage
    {
        private readonly MyProtectorsViewModel _viewModel;

        public MyProtectorsPage()
        {
            InitializeComponent();
            _viewModel = new MyProtectorsViewModel();
            BindingContext = _viewModel;
        }
    }
}