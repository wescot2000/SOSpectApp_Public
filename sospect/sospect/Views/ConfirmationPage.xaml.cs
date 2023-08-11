using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;

namespace sospect.Views
{
    public partial class ConfirmationPage : ContentPage
    {
        public event EventHandler<bool> ConfirmationResult;

        public ConfirmationPage(string message)
        {
            InitializeComponent();
            ConfirmationMessageLabel.Text = message;

            YesButton.Clicked += (sender, args) => ConfirmationResult?.Invoke(this, true);
            NoButton.Clicked += (sender, args) => ConfirmationResult?.Invoke(this, false);
        }
    }
}
