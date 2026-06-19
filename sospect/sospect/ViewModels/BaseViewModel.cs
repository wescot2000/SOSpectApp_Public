// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

        //  NUEVOS: Para t�tulos y subt�tulos responsivos
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

        //  Enum para tama�os base
        private enum BaseFontSize
        {
            Micro = 10,
            Small = 14,
            Medium = 16,
            Large = 20,
            SubHeadline = 24,  //  AGREGADO
            Headline = 32      //  AGREGADO
        }

        //  M�todo que calcula tama�os responsivos
        private double GetResponsiveFontSize(BaseFontSize baseSize)
        {
            try
            {
                var density = DeviceDisplay.MainDisplayInfo.Density;
                var baseValue = (double)baseSize;

                // Factor de escala basado en la densidad de pantalla
                var scaleFactor = 1.0;

                // Ajustar seg�n la densidad de la pantalla
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
                    scaleFactor *= 1.2; // Tablets necesitan texto m�s grande
                }

                var calculatedSize = baseValue * scaleFactor;

                // L�mites m�nimos y m�ximos para accesibilidad
                var minSize = baseValue * 0.8;
                var maxSize = baseValue * 1.5;

                return Math.Max(minSize, Math.Min(maxSize, calculatedSize));
            }
            catch
            {
                // Fallback a valores seguros si hay alg�n problema
                return (double)baseSize;
            }
        }
    }
}

