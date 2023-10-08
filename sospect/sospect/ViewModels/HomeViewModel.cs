using Newtonsoft.Json;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Essentials;

namespace sospect.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        public HomeViewModel(bool CargarParametros)
        {
            ShowUIButtons = CargarParametros;
            IsRunning = CargarParametros;
        }


        public static async Task<bool> InicializarParametrosUsuarioAsync()
        {
            try
            {
                ParametroUsuarioRequest request = new ParametroUsuarioRequest()
                {
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    PUserIdThirdparty = App.persona.user_id_thirdparty,
                    RegistrationId = App.persona.RegistrationId
                };

                string parametros = await ApiService.ObtenerParametrosDeUsuario(request);
                if (!string.IsNullOrEmpty(parametros))
                {
                    ParametrosUsuario parametrosUsuario = JsonConvert.DeserializeObject<ParametrosUsuario>(parametros);
                    parametrosUsuario.FechaParametro = DateTime.Now;
                    parametros = JsonConvert.SerializeObject(parametrosUsuario);
                    Preferences.Set("ParametrosUsuario", parametros);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "HomeViewModel" },
                        { "Method", "ObtenerParametrosDeUsuario" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                return false;
            }
        }

        public async Task InicializarDatosUsuarioAsync()
        {
            try
            {
                if (ShowUIButtons)
                {
                    var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosGuardados))
                    {
                        ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                        NumeroDeNotificaciones = parametros?.MensajesParaUsuario ?? 0;
                    }
                    else
                    {
                        NumeroDeNotificaciones = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
                var properties = new Dictionary<string, string>
                                    {
                                        { "Object", "HomeViewModel" },
                                        { "Method", "InicializarDatosUsuario" }
                                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
        }


        private int _numeroDeNotificaciones;
        public int NumeroDeNotificaciones
        {
            get { return _numeroDeNotificaciones; }
            set
            {
                _numeroDeNotificaciones = value;
                OnPropertyChanged(nameof(NumeroDeNotificaciones));
                HayNotificaciones = _numeroDeNotificaciones > 0;
            }
        }

        private bool _hayNotificaciones;
        public bool HayNotificaciones
        {
            get { return _hayNotificaciones; }
            set
            {
                _hayNotificaciones = value;
                OnPropertyChanged(nameof(HayNotificaciones));
            }
        }

        private bool _showUIButtons;
        public bool ShowUIButtons
        {
            get { return _showUIButtons; }
            set
            {
                _showUIButtons = value;
                OnPropertyChanged(nameof(ShowUIButtons));
            }
        }
    }
}

