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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
