// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class TutorialPopupPageViewModel : BaseViewModel
    {
        private int _delay;
        public int Delay
        {
            get { return _delay; }
            set { SetProperty(ref _delay, value); }
        }

        private string _tutorialText;

        public string TutorialText
        {
            get { return _tutorialText; }
            set { SetProperty(ref _tutorialText, value); }
        }

        private double _frameTopMargin;

        public double FrameTopMargin
        {
            get { return _frameTopMargin; }
            set { SetProperty(ref _frameTopMargin, value); }
        }

        private double _frameLeftMargin;

        public double FrameLeftMargin
        {
            get { return _frameLeftMargin; }
            set { SetProperty(ref _frameLeftMargin, value); }
        }

        private double _frameBottomMargin;

        public double FrameBottomMargin
        {
            get { return _frameBottomMargin; }
            set { SetProperty(ref _frameBottomMargin, value); }
        }

        private double _frameRightMargin;

        public double FrameRightMargin
        {
            get { return _frameRightMargin; }
            set { SetProperty(ref _frameRightMargin, value); }
        }

        private TextAlignment _labelAlignment;

        public TextAlignment LabelAlignment
        {
            get { return _labelAlignment; }
            set { SetProperty(ref _labelAlignment, value); }
        }

        private double _frameWidthRequest;

        public double FrameWidthRequest
        {
            get { return _frameWidthRequest; }
            set { SetProperty(ref _frameWidthRequest, value); }
        }

        private double _frameHeightRequest;

        public double FrameHeightRequest
        {
            get { return _frameHeightRequest; }
            set { SetProperty(ref _frameHeightRequest, value); }
        }

        private LayoutOptions _frameHorizontalOptions;

        public LayoutOptions FrameHorizontalOptions
        {
            get { return _frameHorizontalOptions; }
            set { SetProperty(ref _frameHorizontalOptions, value); }
        }

        private LayoutOptions _frameVerticalOptions;

        public LayoutOptions FrameVerticalOptions
        {
            get { return _frameVerticalOptions; }
            set { SetProperty(ref _frameVerticalOptions, value); }
        }

        public Thickness FrameMargin
        {
            get
            {
                return new Thickness(FrameLeftMargin, FrameTopMargin, FrameRightMargin, FrameBottomMargin);
            }
        }
    }
}


