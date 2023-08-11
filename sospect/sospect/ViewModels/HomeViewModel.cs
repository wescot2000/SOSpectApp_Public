using Newtonsoft.Json;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Threading.Tasks;

using Xamarin.Essentials;

namespace sospect.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        public HomeViewModel(bool CargarParametros)
        {
            try
            {
                if (CargarParametros)
                {
                    ShowUIButtons = true;
                    if (JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", "")) != null)
                    {
                        ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                        NumeroDeNotificaciones = parametros?.MensajesParaUsuario ?? 0;
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            IsRunning = true;
                            if (await InicializarParametrosUsuarioAsync())
                            {
                                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                                NumeroDeNotificaciones = parametros?.MensajesParaUsuario ?? 0;
                            }
                            else
                            {
                                NumeroDeNotificaciones = 0;
                            }
                            IsRunning = false;
                        });
                    }
                }
                else
                {
                    ShowUIButtons = false;
                    IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
            }

           
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
                return false;
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

        private bool _ShowUIButtons;
        public bool ShowUIButtons
        {
            get { return _ShowUIButtons; }
            set
            {
                _ShowUIButtons = value;
                OnPropertyChanged(nameof(_ShowUIButtons));
            }
        }
    }
}

