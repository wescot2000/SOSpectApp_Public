// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Views.Popups;

namespace sospect.Helpers
{
    // Enum para resultados de 3 opciones
    public enum ThreeOptionResult
    {
        Option1,    // Primera opción
        Option2,    // Segunda opción
        Option3,    // Tercera opción
        Cancelled   // Usuario cerró sin elegir
    }

    public static class ModernAlerts
    {
        private static bool _isPopupOpen = false;

        public static async Task ShowSuccess(string title, string message, string buttonText = null)
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;
            try
            {
                if (string.IsNullOrEmpty(buttonText))
                    buttonText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
                var popup = new ModernAlertPopup(ModernAlertConfig.Success(title, message, buttonText));
                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            finally { _isPopupOpen = false; }
        }

        public static async Task ShowError(string title, string message, string buttonText = null)
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;
            try
            {
                if (string.IsNullOrEmpty(buttonText))
                    buttonText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
                var popup = new ModernAlertPopup(ModernAlertConfig.Error(title, message, buttonText));
                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            finally { _isPopupOpen = false; }
        }

        public static async Task ShowInfo(string title, string message, string buttonText = null)
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;
            try
            {
                if (string.IsNullOrEmpty(buttonText))
                    buttonText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
                var popup = new ModernAlertPopup(ModernAlertConfig.Info(title, message, buttonText));
                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            finally { _isPopupOpen = false; }
        }

        public static async Task ShowWarning(string title, string message, string buttonText = null)
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;
            try
            {
                if (string.IsNullOrEmpty(buttonText))
                    buttonText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
                var popup = new ModernAlertPopup(ModernAlertConfig.Warning(title, message, buttonText));
                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            finally { _isPopupOpen = false; }
        }

        public static async Task<bool> ShowConfirmation(string title, string message, string yesText = null, string noText = null)
        {
            if (_isPopupOpen) return false;
            _isPopupOpen = true;
            try
            {
                if (string.IsNullOrEmpty(yesText))
                    yesText = await TranslateExtension.TranslateAsync("LabelSi") ?? "Sí";
                if (string.IsNullOrEmpty(noText))
                    noText = await TranslateExtension.TranslateAsync("LabelNo") ?? "No";

                bool result = false;
                var popup = new ModernAlertPopup(ModernAlertConfig.Confirmation(title, message, yesText, noText));
                popup.PrimaryButtonTapped += (s, e) => result = true;
                popup.SecondaryButtonTapped += (s, e) => result = false;

                await Application.Current.MainPage.ShowPopupAsync(popup);
                return result;
            }
            finally { _isPopupOpen = false; }
        }

        public static async Task<bool> ShowConfirmation(string title, string message, string yesText, string noText, bool useTranslations)
        {
            if (useTranslations)
            {
                return await ShowConfirmation(title, message, null, null);
            }
            else
            {
                if (_isPopupOpen) return false;
                _isPopupOpen = true;
                try
                {
                    bool result = false;
                    var popup = new ModernAlertPopup(ModernAlertConfig.Confirmation(title, message, yesText, noText));
                    popup.PrimaryButtonTapped += (s, e) => result = true;
                    popup.SecondaryButtonTapped += (s, e) => result = false;

                    await Application.Current.MainPage.ShowPopupAsync(popup);
                    return result;
                }
                finally { _isPopupOpen = false; }
            }
        }

        // NUEVO: Método para 3 opciones
        public static async Task<ThreeOptionResult> ShowThreeOptions(
            string title,
            string message,
            string option1Text,
            string option2Text,
            string option3Text)
        {
            if (_isPopupOpen) return ThreeOptionResult.Cancelled;
            _isPopupOpen = true;
            try
            {
                var result = ThreeOptionResult.Cancelled;

                var popup = new ModernAlertPopup(ModernAlertConfig.ThreeOptions(
                    title,
                    message,
                    option1Text,
                    option2Text,
                    option3Text
                ));

                popup.PrimaryButtonTapped += (s, e) => result = ThreeOptionResult.Option1;
                popup.SecondaryButtonTapped += (s, e) => result = ThreeOptionResult.Option2;
                popup.TertiaryButtonTapped += (s, e) => result = ThreeOptionResult.Option3;

                await Application.Current.MainPage.ShowPopupAsync(popup);
                return result;
            }
            finally { _isPopupOpen = false; }
        }

        /// <summary>
        /// Muestra un popup de procesamiento mientras se ejecuta una tarea.
        /// El popup se cierra automáticamente cuando la tarea termina.
        /// </summary>
        /// <param name="message">Mensaje a mostrar (ej: "Procesando foto...")</param>
        /// <param name="task">Tarea a ejecutar</param>
        public static async Task ShowProcessingAsync(string message, Func<Task> task)
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;

            var popup = new ProcessingPopup(message);
            Exception taskException = null;

            popup.Opened += async (s, e) =>
            {
                try
                {
                    await Task.Delay(50);
                    await task();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModernAlerts] Error en tarea: {ex.Message}");
                    taskException = ex;
                }
                finally
                {
                    _isPopupOpen = false;
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Task.Delay(50);
                            await popup.CloseAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModernAlerts] Error cerrando popup: {ex.Message}");
                    }
                }
            };

            await Application.Current.MainPage.ShowPopupAsync(popup);

            if (taskException != null)
                throw taskException;
        }

        /// <summary>
        /// Muestra un popup de procesamiento mientras se ejecuta una tarea con resultado.
        /// </summary>
        public static async Task<T> ShowProcessingAsync<T>(string message, Func<Task<T>> task)
        {
            if (_isPopupOpen) return default;
            _isPopupOpen = true;

            var popup = new ProcessingPopup(message);
            T result = default;
            Exception taskException = null;

            popup.Opened += async (s, e) =>
            {
                try
                {
                    await Task.Delay(50);
                    result = await task();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModernAlerts] Error en tarea: {ex.Message}");
                    taskException = ex;
                }
                finally
                {
                    _isPopupOpen = false;
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Task.Delay(50);
                            await popup.CloseAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModernAlerts] Error cerrando popup: {ex.Message}");
                    }
                }
            };

            await Application.Current.MainPage.ShowPopupAsync(popup);

            if (taskException != null)
                throw taskException;

            return result;
        }
    }
}

