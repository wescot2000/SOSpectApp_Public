using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Extensions;
using sospect.CustomRenderers;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class CerrarAlarmaPopUpViewModel : BaseViewModel
    {
        // Definición del comando para cancelar el PopUpPage
        public Command CancelarCommand { get; set; }

        public Command ConfirmarCierreCommand { get; set; }

        private DescribirAlarma _DescripcionAlarma;
        public DescribirAlarma DescripcionAlarma
        {
            get => this._DescripcionAlarma;
            set => this.SetValue(ref this._DescripcionAlarma, value);
        }

        private bool _flagHuboCaptura;
        public bool FlagHuboCaptura
        {
            get => _flagHuboCaptura;
            set => SetValue(ref _flagHuboCaptura, value);
        }

        private bool _flagEsFalsaAlarma;
        public bool FlagEsFalsaAlarma
        {
            get => _flagEsFalsaAlarma;
            set => SetValue(ref _flagEsFalsaAlarma, value);
        }


        private string _descripcionCierre;
        public string DescripcionCierre
        {
            get => _descripcionCierre;
            set => SetValue(ref _descripcionCierre, value);
        }


        private bool _flag_propietario_alarma;
        public bool flag_propietario_alarma
        {
            get => this._flag_propietario_alarma;
            set => this.SetValue(ref this._flag_propietario_alarma, value);
        }

        
        public CerrarAlarmaPopUpViewModel(AlarmaCercana alarmaCercana)
        {
            
            DescripcionAlarma = new DescribirAlarma();
            DescripcionAlarma.alarma_id = alarmaCercana.alarma_id;
            DescripcionAlarma.p_user_id_thirdparty = alarmaCercana.user_id_thirdparty;

            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelHecho = TranslateExtension.Translate("LabelHecho");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblCondicionesCierreAlarma = TranslateExtension.Translate("LblCondicionesCierreAlarma");
            

            ConfirmarCierreCommand = new Command(async () =>
            {
                if (IsRunning) return;

                var request = new CerrarAlarmaRequest()
                {
                    p_alarma_id = DescripcionAlarma.alarma_id, // Aquí debes asignar el valor correcto
                    p_user_id_thirdparty = DescripcionAlarma.p_user_id_thirdparty, // Aquí debes asignar el valor correcto
                    p_descripcion_cierre = DescripcionCierre, // Aquí debes asignar el valor correcto
                    p_flag_es_falsaalarma = FlagEsFalsaAlarma, // Este valor puede variar según lo que necesites
                    p_flag_hubo_captura = FlagHuboCaptura   , // Este valor puede variar según lo que necesites
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma() // Por ejemplo, si es en español
            };

                IsRunning = true;

                try
                {
                    ResponseMessage response = await ApiService.CerrarAlarma(request);

                    if (response.IsSuccess)
                    {
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, LabelHecho, LabelOK);

                        if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Any())
                        {
                            // Cerrar el PopUpPage
                            await App.Current.MainPage.Navigation.PopPopupAsync();
                        }

                        // Redirigir al usuario a la página de inicio.
                        // Asegúrate de que la navegación se haga correctamente a tu página de inicio.
                        App.Current.MainPage = new NavigationPage(new SospectTabs());
                    }
                    else
                    {
                        
                        // Aquí puedes implementar cualquier acción en caso de error
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, LblCondicionesCierreAlarma, LabelOK);
                    }

                }
                catch (Exception ex)
                {
                    await App.Current.MainPage.DisplayAlert(LabelInformacion, ex.Message, LabelOK);
                    var properties = new Dictionary<string, string> {
                        { "Object", "CerrarAlarmaPopUpViewModel" },
                        { "Method", "CerrarAlarma" }
                    };
                    Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                }
                finally
                {
                    IsRunning = false;
                }

            }, () => !IsRunning);


            // Asignación de la acción que se ejecutará al pulsar el botón de cancelar en el PopUpPage
            CancelarCommand = new Command(async () =>
            {
                if (IsRunning) return;
                // Cerrar el PopUpPage
                await App.Current.MainPage.Navigation.PopPopupAsync();
            }, () => !IsRunning);
               
        }
    }
}

