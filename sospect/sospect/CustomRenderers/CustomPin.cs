using sospect.Models;
using System;
using Xamarin.Forms.Maps;

namespace sospect.CustomRenderers
{
    public class CustomPin : Pin
    {
        public string Id;
        public string IdAlarma { get; set; }
        public long TipoAlarma { get; set; }
        public bool FlagPropietarioAlarma { get; set; }
        public AlarmaCercana AlarmaCercana { get; set; }

    }
}

