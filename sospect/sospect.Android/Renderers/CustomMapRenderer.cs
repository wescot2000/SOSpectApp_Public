using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Views;
using Android.Widget;
using sospect.CustomRenderers;
using sospect.Droid.Renderers;
using sospect.Popups;
using sospect.Views;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;
using Xamarin.Forms.Platform.Android;
using static Android.Gms.Maps.GoogleMap;

[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace sospect.Droid.Renderers
{
    public class CustomMapRenderer : MapRenderer, IOnMapReadyCallback, IInfoWindowAdapter
    {
        List<CustomPin> customPins;

        public CustomMapRenderer(Context context) : base(context)
        {
            customPins = new List<CustomPin>();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Map> e)
        {
            /***************************************************************************/
            // El codigo para el Tap sobre el mapa
            /***************************************************************************/
            if (_map != null)
                _map.MapClick -= GoogleMap_MapClick;

            base.OnElementChanged(e);

            if (e.OldElement != null)
            {

            }

            if (e.NewElement != null)
            {
                var formsMap = (CustomMap)e.NewElement;
                customPins = formsMap.CustomPins;
                Control.GetMapAsync(this);
            }

            if (Control != null)
                ((MapView)Control).GetMapAsync(this);

        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CustomMap.Pins))
            {
                if (sender is sospect.CustomRenderers.CustomMap map)
                {
                    if (map.Pins.Any())
                    {
                        customPins = (List<CustomPin>)map.Pins;
                    }
                }
            }
        }

        public Android.Views.View GetInfoContents(Marker marker)
        {
            //throw new NotImplementedException();
            return null;

        }

        public Bitmap ResizeBitmap(Bitmap originalBitmap, int width, int height)
        {
            return Bitmap.CreateScaledBitmap(originalBitmap, width, height, false);
        }


        private BitmapDescriptor CreateCustomMarker(int iconResourceId, int cantidadInteracciones, bool isBeingAttended, bool estadoAlarma)
        {
            // 1. Carga el pin original.
            Bitmap pinBitmap = BitmapFactory.DecodeResource(Resources, iconResourceId);

            // 2. Crear un Bitmap mutable.
            Bitmap resultBitmap = pinBitmap.Copy(Bitmap.Config.Argb8888, true);
            Canvas canvas = new Canvas(resultBitmap);

            // 3. Si la alarma está siendo atendida, añadir el icono del policía.
            // 3. Si la alarma está siendo atendida, añadir el icono del policía.
            if (isBeingAttended)
            {
                Bitmap policeBitmap;

                if (estadoAlarma)
                {
                    policeBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.man_officer_police_icon);
                }
                else
                {
                    policeBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.man_officer_police_icon_grey);
                }

                // Redimensionar el policeBitmap
                int newPoliceWidth = policeBitmap.Width * 2 / 3; // Reduciendo a un 66% del tamaño original como ejemplo.
                int newPoliceHeight = policeBitmap.Height * 2 / 3;
                policeBitmap = ResizeBitmap(policeBitmap, newPoliceWidth, newPoliceHeight);

                // Dibuja el policeBitmap en la posición deseada
                canvas.DrawBitmap(policeBitmap, pinBitmap.Width - policeBitmap.Width - 1, 1, null);
            }


            // 4. Añadir el badge de notificaciones.
            if (cantidadInteracciones > 0)
            {
                Paint paint = new Paint
                {
                    Color = estadoAlarma ? Android.Graphics.Color.Red : Android.Graphics.Color.Gray,
                    AntiAlias = true  // Para que los bordes del círculo sean suaves
                };

                float badgeDiameter = 40.0f;
                canvas.DrawCircle(2 + badgeDiameter / 2, 2 + badgeDiameter / 2, badgeDiameter / 2, paint);  // Ajustada la posición del badge

                // Configuración para el texto
                paint.Color = Android.Graphics.Color.White;
                paint.TextAlign = Paint.Align.Center;
                paint.TextSize = 20.0f;
                canvas.DrawText(cantidadInteracciones.ToString(), 2 + badgeDiameter / 2, (2 + badgeDiameter / 2) + (paint.TextSize / 3), paint); // Ajuste vertical para centrar el texto
            }

            return BitmapDescriptorFactory.FromBitmap(resultBitmap);
        }



        protected override MarkerOptions CreateMarker(Pin pin)
        {
            var marker = new MarkerOptions();
            marker.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));
            marker.SetTitle(pin.Label);
            marker.SetSnippet(pin.Address);

            if (pin is CustomPin cpin)
            {
                int iconResourceId = Resource.Drawable.user_location; // Imagen por defecto
                
                if (cpin.AlarmaCercana != null)
                {
                    if (cpin.AlarmaCercana.estado_alarma)
                    {
                        switch (cpin.TipoAlarma)
                        {
                            case 1:
                                iconResourceId = Resource.Drawable.WarningPin;
                                break;
                            case 2:
                                iconResourceId = Resource.Drawable.DangerPin;
                                break;
                            case 3:
                                iconResourceId = Resource.Drawable.AmarilloPleitoCallejero;
                                break;
                            case 4:
                                iconResourceId = Resource.Drawable.VerdeAzulMascotaPerdida;
                                break;
                            case 5:
                                iconResourceId = Resource.Drawable.AzulPersonaPerdida;
                                break;
                            case 6:
                                iconResourceId = Resource.Drawable.NegroDisturbiosProtestas;
                                break;
                            case 7:
                                iconResourceId = Resource.Drawable.VerdeAcompanamientoVisual;
                                break;
                            case 8:
                                iconResourceId = Resource.Drawable.PurpuraViolenciaIntrafamiliar;
                                break;
                            case 9:
                                iconResourceId = Resource.Drawable.DireccionEscapeSospechoso;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        if (cpin.TipoAlarma == 9)
                        {
                            iconResourceId = Resource.Drawable.DireccionEscapeSospechosoCerrada;
                        }
                        else if (cpin.TipoAlarma >= 1 && cpin.TipoAlarma <= 9)
                        {
                            iconResourceId = Resource.Drawable.ClosedAlarmPin;
                        }
                        else
                        {
                            iconResourceId = Resource.Drawable.user_location; // Garantizar que el ícono predeterminado se utilice si las otras condiciones no se cumplen
                        }
                    }
                    var customMarker = CreateCustomMarker(iconResourceId, cpin.AlarmaCercana.cantidad_interacciones, cpin.AlarmaCercana.flag_alarma_siendo_atendida, cpin.AlarmaCercana.estado_alarma);
                    marker.SetIcon(customMarker);
                }
                else
                {
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.user_location));
                }
            }
            return marker;
        }


        /*********************************************************************/
        // Codigo para el manejo del Tap sobre el mapa en Android
        /*********************************************************************/
        // uso Google map nativo en Android
        private GoogleMap _map;

        protected override void OnMapReady(GoogleMap googleMap)
        {
            _map = googleMap;

            if (_map != null)
            {
                _map.MapClick += GoogleMap_MapClick;
                _map.InfoWindowClick += OnInfoWindowClick;
                _map.SetInfoWindowAdapter(this);
                //_map.UiSettings.ZoomControlsEnabled = false;
                //_map.UiSettings.RotateGesturesEnabled = false;
                //_map.UiSettings.SetAllGesturesEnabled(false);
            }

        }

        private void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs eventArg)
        {
            if (eventArg.Marker.Title == "User")
            {
                return;
            }

            string idAlarma = eventArg.Marker.Title.Split(' ')[1];
            App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse(idAlarma)));
        }

        public Android.Views.View GetInfoWindow(Marker marker)
        {

            return null;
        }

        private void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            ((CustomMap)Element).OnTap(new Position(e.Point.Latitude, e.Point.Longitude));
        }
    }
}
