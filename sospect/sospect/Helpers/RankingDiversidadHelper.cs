using System;
using System.Collections.Generic;
using System.Linq;
using sospect.Models;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper para reordenamiento con diversidad en feed "Para ti".
    /// Implementa Reglas 25 (tendencia por tipo) y 26 (diversidad de contenido)
    /// del Manual Sistema de Ranking y Relevancia (0622).
    ///
    /// Diseño:
    /// - Las alarmas de seguridad personal NO se reordenan (mantienen orden SQL)
    /// - Solo las alarmas virales (flag_seguridad_personal=false) se reordenan
    /// - El seed del Random se genera una vez por sesión (Guid estático): el orden
    ///   cambia cada vez que se cierra y abre la app, y también cada vez que el
    ///   usuario hace pull-to-refresh explícito (RegenerarSeed)
    /// - El factor de variación es 0.3 (30%): introduce variación sin destruir el ranking base
    /// - Cada 5 alarmas consecutivas del mismo tipo se intercala una de otro tipo (Regla 26)
    /// </summary>
    public static class RankingDiversidadHelper
    {
        // Factor de variación aleatoria sobre el ranking base (200% — aumentado de 0.3 el 21022026)
        // Con 2.0: una alarma con ranking 100 puede puntuar entre 100 y 300;
        // una con ranking 50 entre 50 y 150, creando solapamiento real para variedad sin perder relevancia.
        private const double FACTOR_VARIACION = 2.0;

        // Cantidad máxima de alarmas consecutivas del mismo tipo permitida
        private const int MAX_CONSECUTIVAS_MISMO_TIPO = 5;

        // Seed por sesión: se genera cuando la clase se carga en memoria y se renueva
        // en cada pull-to-refresh explícito (RegenerarSeed). Los polling automáticos
        // (movimiento, inactividad) no lo renuevan, manteniendo estabilidad entre updates silenciosos.
        private static int _seedSesion = Guid.NewGuid().GetHashCode();

        /// <summary>
        /// Renueva el seed de la sesión. Se llama desde pull-to-refresh
        /// para que el siguiente AplicarDiversidad produzca un orden diferente.
        /// </summary>
        public static void RegenerarSeed()
        {
            _seedSesion = Guid.NewGuid().GetHashCode();
            System.Diagnostics.Debug.WriteLine("[RankingDiversidad] Seed regenerado por pull-to-refresh");
        }

        /// <summary>
        /// Método principal: recibe la lista ya filtrada por tipos (salida de FiltroAlarmasHelper)
        /// y retorna la lista reordenada con diversidad.
        /// Las alarmas de seguridad personal siempre van arriba en su orden original.
        /// </summary>
        public static List<AlarmaCercana> AplicarDiversidad(List<AlarmaCercana> alarmas)
        {
            if (alarmas == null || alarmas.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[RankingDiversidad] Lista nula o vacía, sin reordenamiento");
                return alarmas ?? new List<AlarmaCercana>();
            }

            // Separar seguridad personal (orden original intacto) de virales (se reordenan)
            var seguridadPersonal = alarmas.Where(a => a.flag_seguridad_personal).ToList();
            var virales = alarmas.Where(a => !a.flag_seguridad_personal).ToList();

            System.Diagnostics.Debug.WriteLine(
                $"[RankingDiversidad] Separación: {seguridadPersonal.Count} seguridad personal, {virales.Count} virales");

            // Si no hay virales, no hay nada que reordenar
            if (virales.Count == 0)
                return alarmas;

            // Random con seed por sesión: consistente dentro de la sesión
            var rng = new Random(_seedSesion);

            // Paso 1: Weighted shuffle sobre virales
            // Cada alarma obtiene ScoreAjustado = ranking_relevancia * (1 + random * 0.3)
            // Esto introduce hasta 30% de variación sin destruir completamente el ranking base
            var viralesReordenadas = virales
                .Select(a => new
                {
                    Alarma = a,
                    ScoreAjustado = (double)a.ranking_relevancia * (1.0 + rng.NextDouble() * FACTOR_VARIACION)
                })
                .OrderByDescending(x => x.ScoreAjustado)
                .Select(x => x.Alarma)
                .ToList();

            // Paso 2: Aplicar regla de diversidad de tipos (Regla 26)
            var viralesConDiversidad = AplicarMaxConsecutivas(viralesReordenadas);

            // Combinar: seguridad personal primero (orden original), virales con diversidad después
            var resultado = new List<AlarmaCercana>(seguridadPersonal.Count + viralesConDiversidad.Count);
            resultado.AddRange(seguridadPersonal);
            resultado.AddRange(viralesConDiversidad);

            System.Diagnostics.Debug.WriteLine(
                $"[RankingDiversidad] Resultado final: {resultado.Count} alarmas " +
                $"(seguridad: {seguridadPersonal.Count}, virales reordenadas: {viralesConDiversidad.Count})");

            return resultado;
        }

        /// <summary>
        /// Intercala alarmas para evitar que un solo tipo monopolice posiciones consecutivas.
        /// Si los últimos MAX_CONSECUTIVAS_MISMO_TIPO elementos son del mismo tipoalarma_id,
        /// busca la siguiente alarma de otro tipo en la lista de pendientes y la intercala.
        /// </summary>
        private static List<AlarmaCercana> AplicarMaxConsecutivas(List<AlarmaCercana> alarmas)
        {
            if (alarmas.Count <= MAX_CONSECUTIVAS_MISMO_TIPO)
                return alarmas; // Lista muy corta, no hay riesgo de monopolio

            var resultado = new List<AlarmaCercana>();
            var pendientes = new List<AlarmaCercana>(alarmas); // copia de trabajo
            int intercalaciones = 0;

            while (pendientes.Count > 0)
            {
                var siguiente = pendientes[0];
                pendientes.RemoveAt(0);

                // Verificar si agregar esta alarma crearía una secuencia de MAX+1 del mismo tipo
                if (resultado.Count >= MAX_CONSECUTIVAS_MISMO_TIPO)
                {
                    var ultimasN = resultado.GetRange(
                        resultado.Count - MAX_CONSECUTIVAS_MISMO_TIPO,
                        MAX_CONSECUTIVAS_MISMO_TIPO);

                    bool todosMismoTipo = ultimasN.All(a => a.tipoalarma_id == siguiente.tipoalarma_id);

                    if (todosMismoTipo)
                    {
                        // Buscar la primera alarma en pendientes de OTRO tipo
                        int otroTipoIdx = pendientes.FindIndex(a => a.tipoalarma_id != siguiente.tipoalarma_id);

                        if (otroTipoIdx >= 0)
                        {
                            // Intercalar: meter la de otro tipo primero, devolver la actual al frente de pendientes
                            var intercalar = pendientes[otroTipoIdx];
                            pendientes.RemoveAt(otroTipoIdx);
                            resultado.Add(intercalar);
                            pendientes.Insert(0, siguiente); // devolver la actual
                            intercalaciones++;
                            continue;
                        }
                        // Si no hay otro tipo disponible, agregar de todas formas (no hay alternativa)
                    }
                }

                resultado.Add(siguiente);
            }

            if (intercalaciones > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RankingDiversidad] Diversidad aplicada: {intercalaciones} intercalación(es) de tipos");
            }

            return resultado;
        }
    }
}
