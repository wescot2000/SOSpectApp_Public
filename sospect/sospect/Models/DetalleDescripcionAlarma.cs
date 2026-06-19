// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace sospect.Models
{
    public partial class DetalleDescripcionAlarma : INotifyPropertyChanged
    {
        [JsonProperty("iddescripcion")]
        public long Iddescripcion { get; set; }

        [JsonProperty("alarma_id")]
        public long AlarmaId { get; set; }

        [JsonProperty("persona_id")]
        public long PersonaId { get; set; }

        [JsonProperty("descripcionalarma")]
        public string Descripcionalarma { get; set; }

        [JsonProperty("descripcionsospechoso")]
        public string Descripcionsospechoso { get; set; }

        [JsonProperty("descripcionvehiculo")]
        public string Descripcionvehiculo { get; set; }

        [JsonProperty("descripcionarmas")]
        public string Descripcionarmas { get; set; }

        [JsonProperty("flagEditado")]
        public bool FlagEditado { get; set; }

        [JsonProperty("propietario_descripcion")]
        public bool PropietarioDescripcion { get; set; }

        [JsonProperty("fechadescripcion")]
        public DateTime Fechadescripcion { get; set; }
        public DateTime FechadescripcionLocal => Fechadescripcion.ToLocalTime();

        [JsonProperty("esAlarmaActiva")]
        public bool esAlarmaActiva { get; set; }

        [JsonProperty("flag_red_confianza")]
        public bool flag_red_confianza { get; set; }

        private bool _flagRedConfianza;
        public bool FlagRedConfianza
        {
            get => _flagRedConfianza;
            set => SetProperty(ref _flagRedConfianza, value);
        }

        private string _textoFlagRedConfianza;
        public string TextoFlagRedConfianza
        {
            get => _textoFlagRedConfianza;
            set => SetProperty(ref _textoFlagRedConfianza, value);
        }

        private string _CalificacionOtrasDescripciones;

        [JsonProperty("calificacion_otras_descripciones")]
        public string CalificacionOtrasDescripciones
        {
            get => this._CalificacionOtrasDescripciones;
            set => this.SetProperty(ref this._CalificacionOtrasDescripciones, value);
        }

        private long _CalificacionDescripcion;

        [JsonProperty("calificaciondescripcion")]
        public long CalificacionDescripcion
        {
            get => this._CalificacionDescripcion;
            set => this.SetProperty(ref this._CalificacionDescripcion, value);
        }

        protected void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return;

            backingField = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Nueva propiedad para fotos
        private List<FotoDescripcionAlarma> _fotos = new List<FotoDescripcionAlarma>();

        [JsonProperty("fotos")]
        public List<FotoDescripcionAlarma> Fotos
        {
            get
            {
                System.Diagnostics.Debug.WriteLine($"[MAUI GETTER] Descripcion {Iddescripcion} - Getter llamado, count: {_fotos?.Count ?? 0}");
                return _fotos;
            }
            set
            {
                System.Diagnostics.Debug.WriteLine($"[MAUI SETTER] Descripcion {Iddescripcion} - Setter llamado con {value?.Count ?? 0} fotos");
                if (value != null && value.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[MAUI SETTER] Primera foto - ID: {value[0].FotoId}, URL: {value[0].UrlFoto}");
                }
                SetProperty(ref _fotos, value ?? new List<FotoDescripcionAlarma>());
            }
        }

        // Propiedad calculada para saber si hay fotos
        public bool TieneFotos => _fotos != null && _fotos.Any();

        // CATEGORÍA DE ALARMA (13-03-2026) - Para botón "Ver autoridades responsables"
        [JsonProperty("categoria_alarma_id")]
        public int? CategoriaAlarmaId { get; set; }
    }
}

