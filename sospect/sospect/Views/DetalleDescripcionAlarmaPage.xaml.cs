using System;
using System.Collections.Generic;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Views
{
    public partial class DetalleDescripcionAlarmaPage : ContentPage
    {
        public DetalleDescripcionAlarmaPage(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            BindingContext = new DetalleDescripcionAlarmaViewModel(alarmaCercana);
        }
    }
}

