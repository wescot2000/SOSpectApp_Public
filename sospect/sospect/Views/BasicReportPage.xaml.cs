﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BasicReportPage : ContentPage
    {
        public BasicReportPage()
        {
            InitializeComponent();

            BindingContext = new BasicReportPageViewModel(Navigation);
        }
    }
}