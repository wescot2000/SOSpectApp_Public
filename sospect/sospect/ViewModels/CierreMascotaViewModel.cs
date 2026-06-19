// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class CierreMascotaViewModel : BaseCierreAlarmaViewModel
    {
        private bool _flagMascotaRecuperada;
        public bool FlagMascotaRecuperada
        {
            get => _flagMascotaRecuperada;
            set => SetValue(ref _flagMascotaRecuperada, value);
        }

        public ICommand ConfirmarCierreCommand { get; }

        public CierreMascotaViewModel(AlarmaCercana alarmaCercana) : base(alarmaCercana)
        {
            ConfirmarCierreCommand = new Command(async () => await ConfirmarCierre());
        }

        private async Task ConfirmarCierre()
        {
            if (IsRunning) return;

            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var LabelHecho = await TranslateExtension.TranslateAsync("LabelHecho");
            var LblCondicionesCierreAlarma = await TranslateExtension.TranslateAsync("LblCondicionesCierreAlarma");

            MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

            try
            {
                var request = new CerrarAlarmaRequest
                {
                    p_alarma_id = AlarmaSeleccionada.alarma_id,
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_descripcion_cierre = DescripcionCierre,
                    p_flag_es_falsaalarma = false,
                    p_flag_hubo_captura = false,
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    p_tipo_cierre = "cierre_mascota",
                    p_flag_mascota_recuperada = FlagMascotaRecuperada
                };

                // Convertir archivos multimedia a DTOs
                if (MediaFiles != null && MediaFiles.Count > 0)
                {
                    request.Fotos = await ConvertMediaFilesToDtos();
                }

                ResponseMessage response = await ApiService.CerrarAlarma(request);

                if (response.IsSuccess)
                {
                    CleanupMediaFiles();

                    await ModernAlerts.ShowSuccess(LabelInformacion, LabelHecho);

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
                CrashlyticsHelper.LogError(ex, "CierreMascotaViewModel", "ConfirmarCierre");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
            }
        }
    }
}


