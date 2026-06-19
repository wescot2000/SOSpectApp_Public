// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using Newtonsoft.Json;
using sospect.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

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



