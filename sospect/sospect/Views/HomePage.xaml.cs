﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Services;
using sospect.CustomRenderers;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Popups;
using sospect.Services;
using sospect.Utils;
using sospect.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Shapes;

namespace sospect.Views
{
    public partial class HomePage : ContentPage
    {

        CancellationTokenSource cts;
        Persona persona;
        private Location _lastLocation;
        private DateTime _lastLocationFetchTime;

        public HomePage(ObservableCollection<AlarmaCercana> alarma)
        {
            InitializeComponent();
            BindingContext = new HomeViewModel(false);
            Task.Run(() => PintarAlarma(alarma));
        }

        private async Task PintarAlarma(ObservableCollection<AlarmaCercana> alarma)
        {
            try
            {
                await Task.Delay(2000);
                if (alarma != null && alarma.Any())
                {
                    map.CustomPins = new List<CustomPin>();

                    foreach (var item in alarma)
                    {
                        var LabelAlarma = TranslateExtension.Translate("LabelAlarma");
                        var LabelMetros = TranslateExtension.Translate("LabelMetros");
                        CustomPin AlarmaPin = new CustomPin()
                        {
                            MarkerId = item.alarma_id,
                            Label = LabelAlarma + " " + item.alarma_id.ToString(),
                            TipoAlarma = item.tipoalarma_id,
                            Type = PinType.Generic,
                            Address = $"{item.descripciontipoalarma}. {item.distancia_en_metros} {LabelMetros}",
                            Position = new Position((double)item.latitud_alarma, (double)item.longitud_alarma),
                            FlagPropietarioAlarma = item.flag_propietario_alarma,
                            AlarmaCercana = item
                        };

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            map.CustomPins.Add(AlarmaPin);
                            map.Pins.Add(AlarmaPin);
                            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position((double)item.latitud_alarma, (double)item.longitud_alarma), new Distance(100)));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
            }
            
        }

        public HomePage()
        {
            this.persona = App.persona;

            InitializeComponent();

            MessagingCenter.Subscribe<object, CustomPin>(this, "InfoWindowClicked", (sender, customPin) =>
            {
                if (customPin.FlagPropietarioAlarma)
                {
                    Navigation.PushModalAsync(new DescribirAlarmaPopUp(customPin.AlarmaCercana));
                }
                else
                {
                    Navigation.PushModalAsync(new ConfirmarAlarmaPage());
                }
            });

            NavigationPage.SetHasNavigationBar(this, false);

            MessagingCenter.Subscribe<object, string>(this, "Refrescar", async (sender, cadenaVacia) =>
            {
                await ObtenerPines();
            });

            MessagingCenter.Subscribe<IBackgroundService, List<AlarmaCercana>>(this, "", (sender, arg) =>
            {
                PintarAlarmasEnMapa(arg);
            });

            BindingContext = new HomeViewModel(true);
        }

        private void AlRecibirMensaje()
        {
            if (BindingContext is HomeViewModel vm)
            {
                vm.NumeroDeNotificaciones += 1;
            }
        }

        private void PintarAlarmasEnMapa(List<AlarmaCercana> arg)
        {
            Dictionary<long, CustomPin> pins = new Dictionary<long, CustomPin>();
            map.CustomPins = new List<CustomPin>();

            map.CustomPins.Clear();
            map.Pins.Clear();

            map.MapElements.Clear();

            foreach (var item in arg)
            {
                var LabelAlarma = TranslateExtension.Translate("LabelAlarma");
                var LabelMetros = TranslateExtension.Translate("LabelMetros");
                CustomPin AlarmaPin = new CustomPin()
                {
                    MarkerId = item.alarma_id,
                    Id = item.alarma_id.ToString(),
                    Label = LabelAlarma + " " + item.alarma_id.ToString(),
                    TipoAlarma = item.tipoalarma_id,
                    Type = PinType.Generic,
                    Address = $"{item.descripciontipoalarma}. {item.distancia_en_metros} {LabelMetros}",
                    Position = new Position((double)item.latitud_alarma, (double)item.longitud_alarma),
                    FlagPropietarioAlarma = item.flag_propietario_alarma,
                    AlarmaCercana = item
                };

                Device.BeginInvokeOnMainThread(() =>
                {
                    map.CustomPins.Add(AlarmaPin);
                    map.Pins.Add(AlarmaPin);
                });

                // Agrega cada AlarmaPin al diccionario
                pins[item.alarma_id] = AlarmaPin;
            }

            foreach (var pin in pins)
            {
                if (pin.Value.AlarmaCercana.alarma_id_padre != null && pins.ContainsKey(pin.Value.AlarmaCercana.alarma_id_padre.Value))
                {
                    var pin1 = pin.Value.Position;
                    var pin2 = pins[pin.Value.AlarmaCercana.alarma_id_padre.Value].Position;

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var polyline = new Xamarin.Forms.Maps.Polyline
                        {
                            StrokeColor = Color.Red,
                            StrokeWidth = 4
                        };
                        polyline.Geopath.Add(pin1);
                        polyline.Geopath.Add(pin2);

                        map.MapElements.Add(polyline);
                    });
                }
            }

            if (App.ubicacionActual != null)
            {
                var LabelUsuario = TranslateExtension.Translate("LabelUsuario");
                var LabelTuUbicacion = TranslateExtension.Translate("LabelTuUbicacion");
                CustomPin UserPin = new CustomPin()
                {
                    MarkerId = "User",
                    Id = "User",
                    Label = LabelUsuario,
                    Type = PinType.Generic,
                    Address = LabelTuUbicacion,
                    Position = new Position(App.ubicacionActual.latitud, App.ubicacionActual.longitud)
                };

                map.MapElements.Add(new Circle()
                {
                    FillColor = Color.FromRgba(255, 255, 0, 0.1),
                    Radius = new Distance(100),
                    Center = UserPin.Position,
                    StrokeWidth = 3,
                    StrokeColor = Color.Blue
                });

                Device.BeginInvokeOnMainThread(() =>
                {
                    map.CustomPins.Add(UserPin);
                    map.Pins.Add(UserPin);
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(App.ubicacionActual.latitud, App.ubicacionActual.longitud), new Distance(100)));
                });

            }
            App.CustomPins = map.CustomPins;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<object, CustomPin>(this, "InfoWindowClicked");
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            //Xamarin.Essentials.Preferences.Set("HasSeenTutorial", false);

            var hasSeenTutorial = Xamarin.Essentials.Preferences.Get("HasSeenTutorial", false);
            if (!hasSeenTutorial)
            {
                var LblHolaBienvenido = TranslateExtension.Translate("LblHolaBienvenido");
                var tutorialPage = new TutorialPopupPage();
                var viewModel = new TutorialPopupPageViewModel
                {
                    TutorialText = LblHolaBienvenido,
                    FrameTopMargin = 230,
                    FrameLeftMargin = 0,
                    FrameBottomMargin = 0,
                    FrameRightMargin = 0,
                    LabelAlignment = "Start",
                    FrameWidthRequest = 280,
                    FrameHeightRequest = 470,
                    FrameHorizontalOptions = LayoutOptions.Center,
                    FrameVerticalOptions = LayoutOptions.Start
                };
                tutorialPage.BindingContext = viewModel;
                tutorialPage.OnPopupClosed += ShowCeroTutorialPopup;
                await PopupNavigation.Instance.PushAsync(tutorialPage);
            }

            if (BindingContext is HomeViewModel vm && !vm.ShowUIButtons)
            {
                return;
            }

            bool shouldFetchLocation = false;

            var currentLocation = await GetCurrentLocation();

            if (_lastLocation == null)
            {
                shouldFetchLocation = true;
            }
            else if (Location.CalculateDistance(currentLocation, _lastLocation, DistanceUnits.Kilometers) > 0.07)
            {
                shouldFetchLocation = true;
            }

            else if (DateTime.Now - _lastLocationFetchTime > TimeSpan.FromMinutes(30))
            {
                shouldFetchLocation = true;
            }

            if (shouldFetchLocation)
            {
                if (App.persona is null)
                {
                    App.persona = JsonConvert.DeserializeObject<Persona>(Preferences.Get("User", ""));
                }

                await ObtenerPines();
                await HomeViewModel.InicializarParametrosUsuarioAsync();
                _lastLocation = currentLocation;
                _lastLocationFetchTime = DateTime.Now;
                try
                {
                    ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

                    if (parametros.FlagUsuarioDebeFirmarCto)
                    {
                        Application.Current.MainPage = new NavigationPage(new TermsAndConditionsPage()) { BarBackgroundColor = Color.Black };
                        return;
                    }

                    if (parametros.FlagBloqueoUsuario)
                    {
                        var LblUsuarioBloqueado = TranslateExtension.Translate("LblUsuarioBloqueado");
                        var LabelError = TranslateExtension.Translate("LabelError");
                        var LabelOK = TranslateExtension.Translate("LabelOK");
                        Application.Current.MainPage.DisplayAlert(LabelError, LblUsuarioBloqueado, LabelOK);
                        Application.Current.MainPage = new NavigationPage(new SuspendedAccountPage()) { BarBackgroundColor = Color.Black };
                        return;
                    }
                }
                catch (Exception)
                {
                    Application.Current.MainPage = new NavigationPage(new InternetRequiredForApp()) { BarBackgroundColor = Color.Black };
                }

               


            }

        }

        private async void ShowCeroTutorialPopup()
        {
            var LblParaIniciar = TranslateExtension.Translate("LblParaIniciar");
            var tutorialPage0 = new TutorialPopupPage();
            var viewModel0 = new TutorialPopupPageViewModel
            {
                TutorialText = LblParaIniciar,
                FrameTopMargin = 320,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 150,
                FrameHeightRequest = 90,
                FrameHorizontalOptions = LayoutOptions.End,
                FrameVerticalOptions = LayoutOptions.Center
            };
            tutorialPage0.BindingContext = viewModel0;
            tutorialPage0.OnPopupClosed += ShowFirstTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage0);
        }

        private async void ShowFirstTutorialPopup()
        {
            var LblDescripcionMapa = TranslateExtension.Translate("LblDescripcionMapa");
            var tutorialPage1 = new TutorialPopupPage();
            var viewModel1 = new TutorialPopupPageViewModel
            {
                TutorialText = LblDescripcionMapa,
                FrameTopMargin = 320,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 200,
                FrameHeightRequest = 220,
                FrameHorizontalOptions = LayoutOptions.Start,
                FrameVerticalOptions = LayoutOptions.Center
            };
            tutorialPage1.BindingContext = viewModel1;
            tutorialPage1.OnPopupClosed += ShowSecondTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage1);
        }

        private async void ShowSecondTutorialPopup()
        {
            var LblAlarmaNoGrave = TranslateExtension.Translate("LblAlarmaNoGrave");
            var tutorialPage2 = new TutorialPopupPage();
            var viewModel2 = new TutorialPopupPageViewModel
            {
                TutorialText = LblAlarmaNoGrave,
                FrameTopMargin = 240,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 180,
                FrameHeightRequest = 250,
                FrameHorizontalOptions = LayoutOptions.End,
                FrameVerticalOptions = LayoutOptions.Center
            };
            tutorialPage2.BindingContext = viewModel2;
            tutorialPage2.OnPopupClosed += ShowThirdTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage2);
        }

        private async void ShowThirdTutorialPopup()
        {
            var LblCrimenCometido = TranslateExtension.Translate("LblCrimenCometido");
            var tutorialPage3 = new TutorialPopupPage();
            var viewModel3 = new TutorialPopupPageViewModel
            {
                TutorialText = LblCrimenCometido,
                FrameTopMargin = 220,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 180,
                FrameHeightRequest = 250,
                FrameHorizontalOptions = LayoutOptions.Start,
                FrameVerticalOptions = LayoutOptions.Center
            };
            tutorialPage3.BindingContext = viewModel3;
            tutorialPage3.OnPopupClosed += ShowFourthTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage3);
        }

        private async void ShowFourthTutorialPopup()
        {
            var LblDescripcionAlarmas = TranslateExtension.Translate("LblDescripcionAlarmas");
            var tutorialPage4 = new TutorialPopupPage();
            var viewModel4 = new TutorialPopupPageViewModel
            {
                TutorialText = LblDescripcionAlarmas,
                FrameTopMargin = 280,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 200,
                FrameHeightRequest = 280,
                FrameHorizontalOptions = LayoutOptions.End,
                FrameVerticalOptions = LayoutOptions.Center
            };
            tutorialPage4.BindingContext = viewModel4;
            tutorialPage4.OnPopupClosed += ShowFifthTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage4);
        }

        private async void ShowFifthTutorialPopup()
        {
            var LblDescripcionMensajes = TranslateExtension.Translate("LblDescripcionMensajes");
            var tutorialPage5 = new TutorialPopupPage();
            var viewModel5 = new TutorialPopupPageViewModel
            {
                TutorialText = LblDescripcionMensajes,
                FrameTopMargin = 260,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 170,
                FrameHeightRequest = 190,
                FrameHorizontalOptions = LayoutOptions.End,
                FrameVerticalOptions = LayoutOptions.Start
            };
            tutorialPage5.BindingContext = viewModel5;
            tutorialPage5.OnPopupClosed += ShowSixthTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage5);

        }

        private async void ShowSixthTutorialPopup()
        {
            var LblDescripcionMenu = TranslateExtension.Translate("LblDescripcionMenu");
            var tutorialPage6 = new TutorialPopupPage();
            var viewModel6 = new TutorialPopupPageViewModel
            {
                TutorialText = LblDescripcionMenu,
                FrameTopMargin = 260,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 200,
                FrameHeightRequest = 220,
                FrameHorizontalOptions = LayoutOptions.Start,
                FrameVerticalOptions = LayoutOptions.Start
            };
            tutorialPage6.BindingContext = viewModel6;
            tutorialPage6.OnPopupClosed += ShowSeventhTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage6);

        }

        private async void ShowSeventhTutorialPopup()
        {
            var LblRecomendaciones = TranslateExtension.Translate("LblRecomendaciones");
            var tutorialPage7 = new TutorialPopupPage();
            var viewModel7 = new TutorialPopupPageViewModel
            {
                TutorialText = LblRecomendaciones,
                FrameTopMargin = 220,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 220,
                FrameHeightRequest = 270,
                FrameHorizontalOptions = LayoutOptions.Center,
                FrameVerticalOptions = LayoutOptions.Start
            };
            tutorialPage7.BindingContext = viewModel7;
            tutorialPage7.OnPopupClosed += ShowEigthTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage7);

        }

        private async void ShowEigthTutorialPopup()
        {
            var LblRecomendacionImportante = TranslateExtension.Translate("LblRecomendacionImportante");
            var tutorialPage8 = new TutorialPopupPage();
            var viewModel8 = new TutorialPopupPageViewModel
            {
                TutorialText = LblRecomendacionImportante,
                FrameTopMargin = 230,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 220,
                FrameHeightRequest = 280,
                FrameHorizontalOptions = LayoutOptions.Center,
                FrameVerticalOptions = LayoutOptions.Start
            };
            tutorialPage8.BindingContext = viewModel8;
            tutorialPage8.OnPopupClosed += ShowNinethTutorialPopup;
            await PopupNavigation.Instance.PushAsync(tutorialPage8);
        }

        private async void ShowNinethTutorialPopup()
        {
            var LblPromocion = TranslateExtension.Translate("LblPromocion");
            var tutorialPage8 = new TutorialPopupPage();
            var viewModel8 = new TutorialPopupPageViewModel
            {
                TutorialText = LblPromocion,
                FrameTopMargin = 250,
                FrameLeftMargin = 0,
                FrameBottomMargin = 0,
                FrameRightMargin = 0,
                LabelAlignment = "Start",
                FrameWidthRequest = 200,
                FrameHeightRequest = 210,
                FrameHorizontalOptions = LayoutOptions.Center,
                FrameVerticalOptions = LayoutOptions.Start
            };
            tutorialPage8.BindingContext = viewModel8;
            await PopupNavigation.Instance.PushAsync(tutorialPage8);
            // Restaurar el estilo original del botón cuando el tutorial se cierre
            BotonRefrescar.BackgroundColor = Color.DarkBlue;
            Xamarin.Essentials.Preferences.Set("HasSeenTutorial", true);
        }

        async Task<Location> GetCurrentLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                cts = new CancellationTokenSource();

                bool gpsStatus = DependencyService.Get<ILocationSettings>().IsGpsAvailable();
                if (!gpsStatus)
                {
                    var location = await Geolocation.GetLastKnownLocationAsync();

                    if (location != null)
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(location.Latitude, location.Longitude), new Distance(100)));
                        App.ubicacionActual.latitud = location.Latitude;
                        App.ubicacionActual.longitud = location.Longitude;

                        return location;
                    }
                    else
                    {
                        DependencyService.Get<ILocationSettings>().OpenSettings();
                        return null;
                    }
                }
                else
                {
                    var location = await Geolocation.GetLocationAsync(request, cts.Token);

                    if (location != null)
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(location.Latitude, location.Longitude), new Distance(100)));
                        App.ubicacionActual.latitud = location.Latitude;
                        App.ubicacionActual.longitud = location.Longitude;
                        return location;
                    }
                    return null;
                }

            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
                return null;
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
                return null;
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                return null;
            }
            catch (Exception ex)
            {
                // Unable to get location
                return null;
            }
        }

        private async void AbrirMenu_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MenuPage());
        }

        private async void AbrirMensajes_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MensajesPage((HomeViewModel)BindingContext));
        }

        bool isBusy = false;

        async void ReportarAlarma_Clicked(System.Object sender, System.EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            await PopupNavigation.Instance.PushAsync(new ConfirmarLanzarAlarmaEnUbicacionActual(App.ubicacionActual.latitud, App.ubicacionActual.longitud, 1));
            IsBusy = false;
        }

        string CuentaRegresivaRefrescar;
        bool IsTimeRunning = false;

        async void RefreshButton_Clicked(System.Object sender, System.EventArgs e)
        {
            IsTimeRunning = true;
            BotonContador.IsVisible = true;
            BotonRefrescar.IsVisible = false;
            CuentaRegresivaRefrescar = "60";
            BotonContador.Text = CuentaRegresivaRefrescar;

            await ObtenerPines();

            Device.StartTimer(new TimeSpan(0, 0, 1), () =>
            {
                // do something every 1 seconds
                Device.BeginInvokeOnMainThread(async () =>
                {
                    // interact with UI elements
                    CuentaRegresivaRefrescar = (int.Parse(CuentaRegresivaRefrescar) - 1).ToString();
                    BotonContador.Text = CuentaRegresivaRefrescar;
                    if (CuentaRegresivaRefrescar == "0")
                    {
                        IsTimeRunning = false;
                        BotonContador.IsVisible = false;
                        BotonRefrescar.IsVisible = true;
                    }
                });
                return IsTimeRunning; // runs again, or false to stop
            });
        }

        private async Task ObtenerPines()
        {
            if (BindingContext is HomeViewModel vm)
            {
                vm.IsRunning = true;

                if (App.ubicacionActual != null)
                {
                    App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                    List<AlarmaCercana> alarmasCercanas = await ApiService.ActualizarUbicacion(App.ubicacionActual);
                    PintarAlarmasEnMapa(alarmasCercanas);
                }
                else
                {
                    App.ubicacionActual = await LocationUtils.ObtenerUbicacionActual();
                    App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                    List<AlarmaCercana> alarmasCercanas = await ApiService.ActualizarUbicacion(App.ubicacionActual);
                    PintarAlarmasEnMapa(alarmasCercanas);
                }

                vm.IsRunning = false;
            }

        }

        async void ReportarCrimen_Clicked(System.Object sender, System.EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            await PopupNavigation.Instance.PushAsync(new ConfirmarLanzarAlarmaEnUbicacionActual(App.ubicacionActual.latitud, App.ubicacionActual.longitud, 2));
            IsBusy = false;
        }
    }

}
