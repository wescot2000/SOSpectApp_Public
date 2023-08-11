using System;
using Newtonsoft.Json;
using sospect.Models;
using Xamarin.Essentials;

namespace sospect.ViewModels
{
    public class SuspendedAccountPageViewModel : BaseViewModel
    {
        private string _FechaFinSuspension;

        public string FechaFinSuspension
        {
            get { return _FechaFinSuspension; }
            set { SetProperty(ref _FechaFinSuspension, value); }
        }

        public SuspendedAccountPageViewModel()
        {
            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

            FechaFinSuspension = parametros.fechafin_bloqueo_usuario.ToString();
        }
    }
}

