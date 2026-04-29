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
    public class CierreCapturaViewModel : BaseCierreAlarmaViewModel
    {
        private bool _flagEsFalsaAlarma;
        public bool FlagEsFalsaAlarma
        {
            get => _flagEsFalsaAlarma;
            set => SetValue(ref _flagEsFalsaAlarma, value);
        }

        private bool _flagHuboCaptura;
        public bool FlagHuboCaptura
        {
            get => _flagHuboCaptura;
            set => SetValue(ref _flagHuboCaptura, value);
        }

        public ICommand ConfirmarCierreCommand { get; }

        public CierreCapturaViewModel(AlarmaCercana alarmaCercana) : base(alarmaCercana)
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
                    p_flag_es_falsaalarma = FlagEsFalsaAlarma,
                    p_flag_hubo_captura = FlagHuboCaptura,
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    p_tipo_cierre = "cierre_captura"
                };

                // Convertir archivos multimedia a DTOs
                if (MediaFiles != null && MediaFiles.Count > 0)
                {
                    request.Fotos = await ConvertMediaFilesToDtos();
                }

                ResponseMessage response = await ApiService.CerrarAlarma(request);

                if (response.IsSuccess)
                {
                    // Limpiar archivos temporales
                    CleanupMediaFiles();

                    await ModernAlerts.ShowSuccess(LabelInformacion, LabelHecho);

                    // Redirigir al usuario a la pagina de inicio
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
                CrashlyticsHelper.LogError(ex, "CierreCapturaViewModel", "ConfirmarCierre");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
            }
        }
    }
}
