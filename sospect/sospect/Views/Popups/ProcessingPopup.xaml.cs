// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Views;
using System;
using System.Threading.Tasks;

namespace sospect.Views.Popups
{
    public partial class ProcessingPopup : Popup
    {
        private TaskCompletionSource<bool> _closeTcs = new TaskCompletionSource<bool>();

        public ProcessingPopup(string message)
        {
            InitializeComponent();
            LblMessage.Text = message;
        }

        /// <summary>
        /// Ejecuta una tarea mientras el popup está visible y luego lo cierra.
        /// </summary>
        public async Task ExecuteAndCloseAsync(Func<Task> task)
        {
            try
            {
                await task();
            }
            finally
            {
                // Cerrar el popup después de un pequeño delay
                await Task.Delay(100);
                await CloseAsync();
            }
        }

        /// <summary>
        /// Ejecuta una tarea con resultado mientras el popup está visible y luego lo cierra.
        /// </summary>
        public async Task<T> ExecuteAndCloseAsync<T>(Func<Task<T>> task)
        {
            try
            {
                return await task();
            }
            finally
            {
                await Task.Delay(100);
                await CloseAsync();
            }
        }
    }
}


