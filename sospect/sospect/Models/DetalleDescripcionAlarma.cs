using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        public DateTimeOffset Fechadescripcion { get; set; }

        [JsonProperty("esAlarmaActiva")]
        public bool esAlarmaActiva { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _CalificacionOtrasDescripciones;

        [JsonProperty("calificacion_otras_descripciones")]
        public string CalificacionOtrasDescripciones
        {
            get => this._CalificacionOtrasDescripciones;
            set => this.SetValue(ref this._CalificacionOtrasDescripciones, value);
        }

        protected void SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return;
            }

            backingField = value;
            OnPropertyChanged(propertyName);
        }

        private long _CalificacionDescripcion;

        [JsonProperty("calificaciondescripcion")]
        public long CalificacionDescripcion
        {
            get => this._CalificacionDescripcion;
            set => this.SetValue(ref this._CalificacionDescripcion, value);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

