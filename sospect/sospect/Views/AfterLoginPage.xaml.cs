using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AfterLoginPage : ContentPage
    {
        public AfterLoginPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}
