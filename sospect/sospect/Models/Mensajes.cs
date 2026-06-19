// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace sospect.Models
{
    public class Mensajes : INotifyPropertyChanged
    {
        public long mensaje_id { get; set; }
        public string asunto { get; set; }
        private bool _estado;

        public bool estado
        {
            get { return _estado; }
            set
            {
                if (_estado != value)
                {
                    _estado = value;
                    OnPropertyChanged();
                }
            }
        }
        public DateTime fecha_mensaje { get; set; }
        // Rediseño 2026-02-08: Metadata para preview enriquecido en lista
        public int? tipoalarma_id { get; set; }
        public string url_foto { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


