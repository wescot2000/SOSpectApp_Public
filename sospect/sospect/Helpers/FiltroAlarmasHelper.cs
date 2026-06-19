// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Linq;
using sospect.Models;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper para filtrado de alarmas por tipo
    /// Implementa el diseño de filtrado Twitter/X para Mapa y "Para ti"
    /// NO se debe usar en "Siguiendo / En tu área" (feed de prioridad crítica)
    /// Fecha: 2025-12-20
    /// </summary>
    public static class FiltroAlarmasHelper
    {
        /// <summary>
        /// Filtra una lista de alarmas basándose en los tipos habilitados en App.TiposAlarmaDisponibles
        /// Si no hay filtros configurados, retorna todas las alarmas
        /// </summary>
        /// <param name="alarmas">Lista de alarmas a filtrar</param>
        /// <returns>Lista filtrada de alarmas</returns>
        public static List<AlarmaCercana> FiltrarPorTipo(List<AlarmaCercana> alarmas)
        {
            if (alarmas == null || alarmas.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[FiltroHelper] No hay alarmas para filtrar");
                return alarmas ?? new List<AlarmaCercana>();
            }

            // Obtener tipos habilitados
            var tiposHabilitados = App.GetTiposHabilitados();

            // Si no hay filtros (HashSet vacío), retornar todas las alarmas
            if (tiposHabilitados.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[FiltroHelper] Sin filtros activos - {alarmas.Count} alarmas sin filtrar");
                return alarmas;
            }

            // Filtrar por tipos habilitados
            var alarmasFiltradas = alarmas
                .Where(a => tiposHabilitados.Contains((int)a.tipoalarma_id))
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[FiltroHelper] Filtrado: {alarmas.Count} alarmas → {alarmasFiltradas.Count} después de filtro");
            System.Diagnostics.Debug.WriteLine($"[FiltroHelper] Tipos habilitados: [{string.Join(", ", tiposHabilitados)}]");

            return alarmasFiltradas;
        }

        /// <summary>
        /// Filtra una ObservableCollection de alarmas
        /// </summary>
        public static List<AlarmaCercana> FiltrarPorTipo(System.Collections.ObjectModel.ObservableCollection<AlarmaCercana> alarmas)
        {
            if (alarmas == null)
                return new List<AlarmaCercana>();

            return FiltrarPorTipo(alarmas.ToList());
        }

        /// <summary>
        /// Verifica si una alarma específica debe mostrarse según el filtro actual
        /// </summary>
        /// <param name="alarma">Alarma a verificar</param>
        /// <returns>True si la alarma debe mostrarse, False si debe ocultarse</returns>
        public static bool DebeMotrarAlarma(AlarmaCercana alarma)
        {
            if (alarma == null)
                return false;

            return App.EsTipoHabilitado((int)alarma.tipoalarma_id);
        }

        /// <summary>
        /// Cuenta cuántos tipos están actualmente deshabilitados
        /// Útil para mostrar indicadores visuales en la UI
        /// </summary>
        public static int ContarTiposDeshabilitados()
        {
            if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                return 0;

            return App.TiposAlarmaDisponibles.Count(t => !t.EstaHabilitado);
        }

        /// <summary>
        /// Cuenta cuántos tipos están actualmente habilitados
        /// </summary>
        public static int ContarTiposHabilitados()
        {
            if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                return 0;

            return App.TiposAlarmaDisponibles.Count(t => t.EstaHabilitado);
        }

        /// <summary>
        /// Verifica si hay algún filtro activo (al menos un tipo deshabilitado)
        /// </summary>
        public static bool HayFiltrosActivos()
        {
            return ContarTiposDeshabilitados() > 0;
        }

        /// <summary>
        /// Obtiene una descripción textual del estado del filtro
        /// Ej: "Mostrando 10 de 13 tipos" o "Mostrando todos los tipos"
        /// </summary>
        public static string ObtenerTextoEstadoFiltro()
        {
            if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                return "Filtro no disponible";

            int total = App.TiposAlarmaDisponibles.Count;
            int habilitados = ContarTiposHabilitados();

            if (habilitados == total)
                return $"Mostrando todos los tipos ({total})";

            return $"Mostrando {habilitados} de {total} tipos";
        }
    }
}


