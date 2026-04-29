using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace sospect.Views.Popups
{
    public partial class AceptarTerminosChatPopup : Popup
    {
        // Flag para evitar múltiples cierres
        private bool _isClosing = false;

        // Propiedad pública para retornar el resultado
        public bool UsuarioAceptoTerminos { get; private set; }

        public AceptarTerminosChatPopup()
        {
            InitializeComponent();
            UsuarioAceptoTerminos = false; // Por defecto, no aceptó
        }

        private void OnCheckboxChanged(object sender, CheckedChangedEventArgs e)
        {
            // Contar cuántos términos están aceptados
            int count = 0;
            if (Check1.IsChecked) count++;
            if (Check2.IsChecked) count++;
            if (Check3.IsChecked) count++;
            if (Check4.IsChecked) count++;

            // Actualizar contador
            LblContador.Text = $"{count} de 4 términos aceptados";
            LblContador.TextColor = count == 4
                ? Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50")
                : Microsoft.Maui.Graphics.Color.FromArgb("#FF5722");

            // Habilitar botón SOLO si los 4 están marcados (NON-NEGOTIABLE según manual)
            bool todosAceptados = Check1.IsChecked && Check2.IsChecked && Check3.IsChecked && Check4.IsChecked;
            BtnContinuar.IsEnabled = todosAceptados;
            BtnContinuar.Opacity = todosAceptados ? 1.0 : 0.5;
        }

        private async void OnCancelarClicked(object sender, System.EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                // Usuario rechazó términos
                UsuarioAceptoTerminos = false;
                await CloseAsync();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
            }
        }

        private async void OnContinuarClicked(object sender, System.EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                // Usuario aceptó TODOS los 4 términos (validado por IsEnabled del botón)
                UsuarioAceptoTerminos = true;
                await CloseAsync();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
            }
        }
    }
}
