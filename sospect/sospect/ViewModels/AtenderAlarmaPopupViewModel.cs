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
    public class AtenderAlarmaPopupViewModel : BaseViewModel
    {
        // Definición del comando para cancelar el PopUpPage
        public Command CancelarCommand { get; set; }

        public Command ConfirmarAtencionCommand { get; set; }

        private DescribirAlarma _DescripcionAlarma;
        public DescribirAlarma DescripcionAlarma
        {
            get => this._DescripcionAlarma;
            set => this.SetValue(ref this._DescripcionAlarma, value);
        }

        public AtenderAlarmaPopupViewModel(AlarmaCercana alarmaCercana)
        {
            
            DescripcionAlarma = new DescribirAlarma();
            DescripcionAlarma.alarma_id = alarmaCercana.alarma_id;
            DescripcionAlarma.p_user_id_thirdparty = alarmaCercana.user_id_thirdparty;

            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var Insertiondone = TranslateExtension.Translate("Insertiondone");
            var LabelOK = TranslateExtension.Translate("LabelOK");

            
            ConfirmarAtencionCommand = new Command(async () =>
            {
                if (IsRunning) return;

                var request = new AtenderAlarmaRequest()
                {
                    p_alarma_id = DescripcionAlarma.alarma_id, 
                    p_user_id_thirdparty = DescripcionAlarma.p_user_id_thirdparty, 
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma() 
                };
                IsRunning = true;
                try
                {
                    ResponseMessage response = await ApiService.AtenderAlarma(request);

                    if (response.IsSuccess)
                    {
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, Insertiondone, LabelOK);

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
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, response.Message, LabelOK);
                    }
                }
                catch (Exception ex)
                {
                    var properties = new Dictionary<string, string> {
                        { "Object", "AtenderAlarmaPopupViewModel" },
                        { "Method", "ConfirmarAtencionCommand" }
                    };
                    await App.Current.MainPage.DisplayAlert(LabelInformacion, ex.Message, LabelOK);
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


