// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace sospect.Views.Popups
{
    public partial class ModernAlertPopup : Popup
    {
        public event EventHandler PrimaryButtonTapped;
        public event EventHandler SecondaryButtonTapped;
        public event EventHandler TertiaryButtonTapped; // NUEVO

        public ModernAlertPopup(ModernAlertConfig config)
        {
            InitializeComponent();
            BindingContext = config;
        }

        private async void OnButtonTapped(object sender, EventArgs e)
        {
            PrimaryButtonTapped?.Invoke(this, EventArgs.Empty);
            await CloseAsync();
        }

        private async void OnSecondaryButtonTapped(object sender, EventArgs e)
        {
            SecondaryButtonTapped?.Invoke(this, EventArgs.Empty);
            await CloseAsync();
        }

        // NUEVO: Handler para tercer botón
        private async void OnTertiaryButtonTapped(object sender, EventArgs e)
        {
            TertiaryButtonTapped?.Invoke(this, EventArgs.Empty);
            await CloseAsync();
        }
    }

    public class ModernAlertConfig
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public string TertiaryButtonText { get; set; } // NUEVO
        public bool HasSecondaryButton { get; set; }
        public bool HasTertiaryButton { get; set; } // NUEVO
        public Color HeaderColor { get; set; }
        public Color ButtonColor { get; set; }
        public string IconText { get; set; }

        public static ModernAlertConfig Success(string title, string message, string buttonText)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = buttonText,
                HeaderColor = Color.FromArgb("#4CAF50"),
                ButtonColor = Color.FromArgb("#4CAF50"),
                IconText = "✓",
                HasSecondaryButton = false,
                HasTertiaryButton = false
            };
        }

        public static ModernAlertConfig Error(string title, string message, string buttonText)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = buttonText,
                HeaderColor = Color.FromArgb("#F44336"),
                ButtonColor = Color.FromArgb("#F44336"),
                IconText = "✗",
                HasSecondaryButton = false,
                HasTertiaryButton = false
            };
        }

        public static ModernAlertConfig Info(string title, string message, string buttonText)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = buttonText,
                HeaderColor = Color.FromArgb("#2196F3"),
                ButtonColor = Color.FromArgb("#2196F3"),
                IconText = "ℹ",
                HasSecondaryButton = false,
                HasTertiaryButton = false
            };
        }

        public static ModernAlertConfig Warning(string title, string message, string buttonText)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = buttonText,
                HeaderColor = Color.FromArgb("#FF9800"),
                ButtonColor = Color.FromArgb("#FF9800"),
                IconText = "⚠",
                HasSecondaryButton = false,
                HasTertiaryButton = false
            };
        }

        public static ModernAlertConfig Confirmation(string title, string message, string primaryButtonText, string secondaryButtonText)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                HeaderColor = Color.FromArgb("#2196F3"),
                ButtonColor = Color.FromArgb("#2196F3"),
                IconText = "?",
                HasSecondaryButton = true,
                HasTertiaryButton = false
            };
        }

        // NUEVO: Configuración para 3 opciones
        public static ModernAlertConfig ThreeOptions(string title, string message, string option1Text, string option2Text, string option3Text)
        {
            return new ModernAlertConfig
            {
                Title = title,
                Message = message,
                ButtonText = option1Text,
                SecondaryButtonText = option2Text,
                TertiaryButtonText = option3Text,
                HeaderColor = Color.FromArgb("#2196F3"),
                ButtonColor = Color.FromArgb("#2196F3"),
                IconText = "?",
                HasSecondaryButton = true,
                HasTertiaryButton = true
            };
        }
    }
}

