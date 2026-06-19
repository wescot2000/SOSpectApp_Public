// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Threading;
using System.Threading.Tasks;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper para implementar debounce/throttle en eventos de alta frecuencia.
    /// Usado principalmente para eventos de cambio de región visible del mapa.
    /// OPTIMIZACIÓN: Reduce 285 eventos de VisibleRegionChanged a ~15-20
    /// </summary>
    public class DebounceHelper
    {
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();

        /// <summary>
        /// Ejecuta una acción después de un delay, cancelando ejecuciones previas pendientes.
        /// Si se llama múltiples veces dentro del delay, solo se ejecuta la última llamada.
        /// </summary>
        /// <param name="action">Acción a ejecutar</param>
        /// <param name="delayMs">Tiempo de espera en milisegundos (default: 500ms)</param>
        public async Task DebounceAsync(Func<Task> action, int delayMs = 500)
        {
            CancellationTokenSource newCts;

            lock (_lock)
            {
                // Cancelar cualquier ejecución pendiente
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                newCts = _cts;
            }

            try
            {
                await Task.Delay(delayMs, newCts.Token);

                // Solo ejecutar si no fue cancelado
                if (!newCts.Token.IsCancellationRequested)
                {
                    await action();
                }
            }
            catch (TaskCanceledException)
            {
                // Esperado cuando se cancela - ignorar silenciosamente
            }
            catch (ObjectDisposedException)
            {
                // Puede ocurrir si Cancel() se llama durante el Delay - ignorar
            }
        }

        /// <summary>
        /// Cancela cualquier ejecución pendiente y libera recursos.
        /// Llamar en OnDisappearing para evitar memory leaks.
        /// </summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}


