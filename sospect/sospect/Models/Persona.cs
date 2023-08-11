using System;
namespace sospect.Models
{
    public class Persona
    {
        public long persona_id { get; set; }

        public string login { get; set; }

        public string user_id_thirdparty { get; set; }

        public int marca_bloqueo { get; set; }

        public string RegistrationId { get; set; }

        public string Plataforma { get; set; }

        public string Idioma { get; set; }
    }
}

