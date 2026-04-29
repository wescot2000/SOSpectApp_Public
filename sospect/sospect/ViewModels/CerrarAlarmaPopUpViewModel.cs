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
    [Obsolete("Reemplazado por CierreCapturaViewModel, CierreEncuestaViewModel, CierrePersonaViewModel, CierreMascotaViewModel y CierreBasicoViewModel. No eliminar hasta confirmar estabilidad.")]
    public class CerrarAlarmaPopUpViewModel : BaseViewModel
    {
        // Comando para cancelar el PopUpPage
        public Command CancelarCommand { get; set; }
        public Command ConfirmarCierreCommand { get; set; }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

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
                    p_alarma_id = DescripcionAlarma.alarma_id,
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_descripcion_cierre = DescripcionCierre,
                    p_flag_es_falsaalarma = FlagEsFalsaAlarma,
                    p_flag_hubo_captura = FlagHuboCaptura,
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };

                MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

                try
                {
                    ResponseMessage response = await ApiService.CerrarAlarma(request);

                    if (response.IsSuccess)
                    {
                        await ModernAlerts.ShowSuccess(LabelInformacion, LabelHecho);

                        // Enviar mensaje para cerrar el popup
                        MessagingCenter.Send(this, "CerrarPopup");

                        // Redirigir al usuario a la página de inicio
                        App.EsPrimerArranque = true;
                        Application.Current.MainPage = new SospectTabs();
                    }
                    else
                    {
                        await ModernAlerts.ShowInfo(LabelInformacion, LblCondicionesCierreAlarma);
                    }
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowError(LabelInformacion, ex.Message);
                    CrashlyticsHelper.LogError(ex, "CerrarAlarmaPopUpViewModel", "ConfirmarCierreCommand");
                }
                finally
                {
                    MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
                }
            }, () => !IsRunning);

            // Comando para cancelar - envía mensaje para cerrar popup
            CancelarCommand = new Command(async () =>
            {
                if (IsRunning) return;

                // Enviar mensaje para cerrar el popup
                MessagingCenter.Send(this, "CerrarPopup");

                await Task.CompletedTask;
            }, () => !IsRunning);
        }
    }
}