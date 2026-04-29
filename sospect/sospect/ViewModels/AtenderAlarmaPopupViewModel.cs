using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using sospect.CustomRenderers;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class AtenderAlarmaPopupViewModel : BaseViewModel
    {
        // CRÍTICO: Referencia al popup para poder cerrarlo
        private readonly Popup _currentPopup;

        public Command CancelarCommand { get; set; }
        public Command ConfirmarAtencionCommand { get; set; }

        private DescribirAlarma _DescripcionAlarma;
        public DescribirAlarma DescripcionAlarma
        {
            get => this._DescripcionAlarma;
            set => this.SetValue(ref this._DescripcionAlarma, value);
        }

        // MODIFICADO: Constructor recibe referencia al popup
        public AtenderAlarmaPopupViewModel(AlarmaCercana alarmaCercana, Popup popup = null)
        {
            _currentPopup = popup;

            DescripcionAlarma = new DescribirAlarma();
            DescripcionAlarma.alarma_id = alarmaCercana.alarma_id;
            DescripcionAlarma.p_user_id_thirdparty = App.persona.user_id_thirdparty;

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

                MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

                try
                {
                    ResponseMessage response = await ApiService.AtenderAlarma(request);

                    if (response.IsSuccess)
                    {
                        // CRÍTICO: Cerrar popup ANTES de mostrar alert
                        await _currentPopup?.CloseAsync();

                        await ModernAlerts.ShowSuccess(LabelInformacion, Insertiondone);

                        // Redirigir al usuario a la página de inicio
                        Application.Current.MainPage = new SospectTabs();
                    }
                    else
                    {
                        await ModernAlerts.ShowInfo(LabelInformacion, response.Message);
                    }
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowError(LabelInformacion, ex.Message);
                    CrashlyticsHelper.LogError(ex, "AtenderAlarmaPopupViewModel", "ConfirmarAtencionCommand");
                }
                finally
                {
                    MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
                }

            }, () => !IsRunning);

            // MODIFICADO: Cerrar popup al cancelar
            CancelarCommand = new Command(async () =>
            {
                if (IsRunning) return;

                // CRÍTICO: Cerrar el popup
                await _currentPopup?.CloseAsync();

            }, () => !IsRunning);
        }
    }
}