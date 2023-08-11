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
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class DescribirAlarmaPopUpViewModel : BaseViewModel
    {
        // Definición del comando para cancelar el PopUpPage
        public Command CancelarCommand { get; set; }

        // Definición del comando para ejecutar una acción con la información del formulario del PopUpPage
        public Command HechoCommand { get; set; }
        public Command HechoDireccionCommand { get; set; }

        private ObservableCollection<TipoAlarma> _TiposAlarma;
        public ObservableCollection<TipoAlarma> TiposAlarma
        {
            get => this._TiposAlarma;
            set => this.SetValue(ref this._TiposAlarma, value);
        }

        private TipoAlarma _TiposAlarmaSeleccionado;
        public TipoAlarma TipoAlarmaSeleccionado
        {
            get => this._TiposAlarmaSeleccionado;
            set => this.SetValue(ref this._TiposAlarmaSeleccionado, value);
        }

        private bool _flag_propietario_alarma;
        public bool flag_propietario_alarma
        {
            get => this._flag_propietario_alarma;
            set => this.SetValue(ref this._flag_propietario_alarma, value);
        }

        private bool _MostrarBotonPuntoHuida;
        public bool MostrarBotonPuntoHuida
        {
            get => this._MostrarBotonPuntoHuida;
            set => this.SetValue(ref this._MostrarBotonPuntoHuida, value);
        }

        private DescribirAlarma _DescripcionAlarma;
        public DescribirAlarma DescripcionAlarma
        {
            get => this._DescripcionAlarma;
            set => this.SetValue(ref this._DescripcionAlarma, value);
        }

        private bool _pickerEnabled = true;
        public bool PickerEnabled
        {
            get { return _pickerEnabled; }
            set
            {
                _pickerEnabled = value;
                OnPropertyChanged();
            }
        }

        public DescribirAlarmaPopUpViewModel(DescribirAlarma alarmaAEditar)
        {
            DescripcionAlarma = alarmaAEditar;
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var Insertiondone = TranslateExtension.Translate("Insertiondone");

            PropertyChanged += OnPropertyChanged;

            // Asignación de la acción que se ejecutará al pulsar el botón de cancelar en el PopUpPage
            CancelarCommand = new Command(async () =>
            {
                // Cerrar el PopUpPage
                await App.Current.MainPage.Navigation.PopPopupAsync();
            });

            _ = CargarTiposAlarma(null, alarmaAEditar);

            HechoCommand = new Command(async () =>
            {
                if (IsRunning) return;
                // Validar los campos del formulario
                IsRunning = true;
                ResponseMessage response = await ApiService.ActualizarDescripcionAlarma(DescripcionAlarma);
                IsRunning = false;
                await App.Current.MainPage.DisplayAlert(LabelInformacion, Insertiondone, LabelOK);

                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Any())
                {
                    // Cerrar el PopUpPage
                    await App.Current.MainPage.Navigation.PopPopupAsync();
                }
                //Actualizar lista de descripcion mandandole un mensaje
                MessagingCenter.Send<object>("valor", "RegistroActualizado");
            }, () => !IsRunning);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TipoAlarmaSeleccionado))
            {
                MostrarBotonPuntoHuida = TipoAlarmaSeleccionado?.TipoalarmaId == 2 || TipoAlarmaSeleccionado?.TipoalarmaId == 9;
            }
        }

        private async Task CargarTiposAlarma(AlarmaCercana alarmaCercana = null, DescribirAlarma describirAlarma = null)
        {
            IsRunning = true;
            // Obtener los IDs de los tipos de alarma desde la base de datos
            var idsTiposAlarma = await ApiService.ObtenerIdsTiposAlarma();
            IsRunning = false;

            // Crear una nueva lista de objetos TipoAlarma con la propiedad DescripcionTraducida asignada
            var tiposAlarma = idsTiposAlarma.Select(id => new TipoAlarma { TipoalarmaId = id }).ToList();

            // Asignar la lista de objetos TipoAlarma a la propiedad TiposAlarma
            TiposAlarma = new ObservableCollection<TipoAlarma>(tiposAlarma);

            if (alarmaCercana != null)
            {
                if (alarmaCercana.tipoalarma_id == 9)
                {
                    TipoAlarmaSeleccionado = new TipoAlarmaEspecial(AppResources.Alarm_9, 9);
                    PickerEnabled = false;
                }
                else
                {
                    TipoAlarmaSeleccionado = TiposAlarma.Where(x => x.TipoalarmaId == alarmaCercana.tipoalarma_id).First();
                    PickerEnabled = true;
                }
            }
            else
            {
                TipoAlarmaSeleccionado = TiposAlarma.Where(x => x.TipoalarmaId == describirAlarma.p_tipoalarma_id).First();
            }
        }


        public DescribirAlarmaPopUpViewModel(AlarmaCercana alarmaCercana)
        {
            DescripcionAlarma = new DescribirAlarma();
            DescripcionAlarma.alarma_id = alarmaCercana.alarma_id;
            DescripcionAlarma.p_user_id_thirdparty = alarmaCercana.user_id_thirdparty;
            DescripcionAlarma.ip_usuario = InternetUtil.GetPublicIpAddress();

            flag_propietario_alarma = alarmaCercana.flag_propietario_alarma;
            _ = CargarTiposAlarma(alarmaCercana, null);

            PropertyChanged += OnPropertyChanged;
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var Insertiondone = TranslateExtension.Translate("Insertiondone");
            var LabelOK = TranslateExtension.Translate("LabelOK");

            // Asignación de la acción que se ejecutará al pulsar el botón de cancelar en el PopUpPage
            CancelarCommand = new Command(async () =>
            {
                // Cerrar el PopUpPage
                await App.Current.MainPage.Navigation.PopPopupAsync();
            });

            HechoDireccionCommand = new Command(async (mapa) =>
            {
                if (IsRunning) return;

                // Indicar que la operación está en curso.
                IsRunning = true;

                if (mapa is MiniMapa mapaDireccionHuida && mapaDireccionHuida.CurrentMapPosition != null)
                {
                    DescripcionAlarma.latitud_escape = mapaDireccionHuida.CurrentMapPosition.Latitude;
                    DescripcionAlarma.longitud_escape = mapaDireccionHuida.CurrentMapPosition.Longitude;
                    if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Any())
                    {
                        // Cerrar el PopUpPage
                        await App.Current.MainPage.Navigation.PopPopupAsync();
                    }
                }
                else
                {
                    var Infor = TranslateExtension.Translate("LabelInformacion");
                    var SeleccionaRuta = TranslateExtension.Translate("SeleccionaRuta");
                    var LabelOK = TranslateExtension.Translate("LabelOK");
                    await App.Current.MainPage.DisplayAlert($"{Infor}", $"{SeleccionaRuta}", $"{LabelOK}");
                }
                IsRunning = false;
            }, (mapa) => !IsRunning);

            HechoCommand = new Command(async () =>
            {
                if (IsRunning) return;

                // Indicar que la operación está en curso.
                IsRunning = true;
                // Validar los campos del formulario
                DescripcionAlarma.p_tipoalarma_id = TipoAlarmaSeleccionado.TipoalarmaId;
                DescripcionAlarma.idioma_descripcion = IdiomUtil.ObtenerCodigoDeIdioma();

                
                ResponseMessage response = await ApiService.DescribirAlarma(DescripcionAlarma);
                IsRunning = false;
                await App.Current.MainPage.DisplayAlert(LabelInformacion, Insertiondone, LabelOK);

                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Any())
                {
                    // Cerrar el PopUpPage
                    await App.Current.MainPage.Navigation.PopPopupAsync();
                }


            }, () => !IsRunning);
        }
    }
}

