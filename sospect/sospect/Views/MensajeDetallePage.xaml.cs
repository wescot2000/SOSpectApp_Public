using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace sospect.Views
{
    public partial class MensajeDetallePage : ContentPage
    {
        public MensajeDetallePage(long messageId)
        {
            BindingContext = new DetalleMensajeViewModel(messageId);
            InitializeComponent();
        }
    }
}
