// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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
        public string Pais { get; set; }

        // ❌ CAMPOS ELIMINADOS (Movidos a modelo Emprendimiento.cs - 13-01-2026):
        // public bool es_proveedor { get; set; }
        // public string url_logo_emprendimiento { get; set; }
        // public DateTime? fecha_actualizacion_logo { get; set; }
        // public decimal reputacion_promedio { get; set; }
        // public int total_calificaciones { get; set; }
        // public int promedio_tiempo_respuesta_minutos { get; set; }
        // public int promedio_tiempo_entrega_horas { get; set; }
        // public decimal porcentaje_satisfaccion { get; set; }
        // public int total_chats_mes_actual { get; set; }
        // public int total_transacciones_exitosas { get; set; }
        // public List<Badge> badges_ganados { get; set; }
    }
}



