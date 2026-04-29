using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Devices;

namespace sospect.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return false;
            }
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool isRunning;
        public bool IsRunning
        {
            get => this.isRunning;
            set => this.SetProperty(ref this.isRunning, value);
        }

        private bool _EmptyState;
        public bool EmptyState
        {
            get => this._EmptyState;
            set => this.SetProperty(ref this._EmptyState, value);
        }

        protected void SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return;
            }
            backingField = value;
            OnPropertyChanged(propertyName);
        }

        //  AGREGADO: FontSizes responsivos basados en el tipo de dispositivo y DPI
        public double FontSizeLarge
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.Large);
            }
        }

        public double FontSizeMedium
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.Medium);
            }
        }

        public double FontSizeSmall
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.Small);
            }
        }

        public double FontSizeMicro
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.Micro);
            }
        }

        //  NUEVOS: Para títulos y subtítulos responsivos
        public double FontSizeHeadline
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.Headline);
            }
        }

        public double FontSizeSubHeadline
        {
            get
            {
                return GetResponsiveFontSize(BaseFontSize.SubHeadline);
            }
        }

        //  Enum para tamaños base
        private enum BaseFontSize
        {
            Micro = 10,
            Small = 14,
            Medium = 16,
            Large = 20,
            SubHeadline = 24,  //  AGREGADO
            Headline = 32      //  AGREGADO
        }

        //  Método que calcula tamaños responsivos
        private double GetResponsiveFontSize(BaseFontSize baseSize)
        {
            try
            {
                var density = DeviceDisplay.MainDisplayInfo.Density;
                var baseValue = (double)baseSize;

                // Factor de escala basado en la densidad de pantalla
                var scaleFactor = 1.0;

                // Ajustar según la densidad de la pantalla
                if (density <= 1.0) // Low DPI
                {
                    scaleFactor = 0.9;
                }
                else if (density <= 2.0) // Medium DPI
                {
                    scaleFactor = 1.0;
                }
                else if (density <= 3.0) // High DPI
                {
                    scaleFactor = 1.1;
                }
                else // Very High DPI
                {
                    scaleFactor = 1.2;
                }

                // Ajustar por tipo de dispositivo
                if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                {
                    scaleFactor *= 1.2; // Tablets necesitan texto más grande
                }

                var calculatedSize = baseValue * scaleFactor;

                // Límites mínimos y máximos para accesibilidad
                var minSize = baseValue * 0.8;
                var maxSize = baseValue * 1.5;

                return Math.Max(minSize, Math.Min(maxSize, calculatedSize));
            }
            catch
            {
                // Fallback a valores seguros si hay algún problema
                return (double)baseSize;
            }
        }
    }
}