// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.ViewModels;
#if ANDROID
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using AndroidSpecific = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
#endif

namespace sospect.Views
{
    public partial class SospectTabs : Microsoft.Maui.Controls.TabbedPage
    {
        private bool _describirPageLoaded = false;
        private ContentPage _placeholderPage;

        public SospectTabs()
        {
            InitializeComponent();

#if ANDROID
            AndroidSpecific.TabbedPage.SetToolbarPlacement(this, AndroidSpecific.ToolbarPlacement.Bottom);
            AndroidSpecific.TabbedPage.SetIsSwipePagingEnabled(this, false);
            System.Diagnostics.Debug.WriteLine("SospectTabs: Swipe entre tabs deshabilitado en Android");
#endif
#if IOS
            System.Diagnostics.Debug.WriteLine("SospectTabs: Configuraci�n iOS aplicada");
#endif

            // SOLUCI�N: Reemplazar DescribirPage con placeholder para evitar inicializaci�n prematura
            ReplaceDescribirPageWithPlaceholder();

            // Suscribir al mensaje de visibilidad de barra (scroll estilo X/Twitter)
            MessagingCenter.Subscribe<object, bool>(this, "TabBarVisible", (sender, visible) =>
            {
                MainThread.BeginInvokeOnMainThread(() => SetTabBarVisibility(visible));
            });
        }

        private void SetTabBarVisibility(bool visible)
        {
#if ANDROID
            try
            {
                if (Handler?.PlatformView is Android.Views.View root)
                {
                    var bnv = FindBottomNavigationView(root);
                    if (bnv != null)
                    {
                        bnv.Visibility = visible
                            ? Android.Views.ViewStates.Visible
                            : Android.Views.ViewStates.Gone;
                        System.Diagnostics.Debug.WriteLine($"SospectTabs: BottomNavigationView visibility={visible}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SospectTabs: BottomNavigationView no encontrado en árbol de vistas");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SospectTabs: Error al cambiar visibilidad de tab bar: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "SospectTabs", "SetTabBarVisibility");
            }
#endif
        }

#if ANDROID
        private Google.Android.Material.BottomNavigation.BottomNavigationView? FindBottomNavigationView(Android.Views.View view)
        {
            if (view is Google.Android.Material.BottomNavigation.BottomNavigationView bnv)
                return bnv;
            if (view is Android.Views.ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    var child = vg.GetChildAt(i);
                    if (child != null)
                    {
                        var found = FindBottomNavigationView(child);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }
#endif

        private void ReplaceDescribirPageWithPlaceholder()
        {
            // Encontrar el NavigationPage que contiene DescribirPage
            NavigationPage describirNavPage = null;
            int describirIndex = -1;

            for (int i = 0; i < Children.Count; i++)
            {
                // CORRECCI�N: Buscar NavigationPage, no DescribirPage directamente
                if (Children[i] is NavigationPage navPage && navPage.CurrentPage is DescribirPage)
                {
                    describirNavPage = navPage;
                    describirIndex = i;
                    break;
                }
            }

            if (describirNavPage != null && describirIndex >= 0)
            {
                // Crear placeholder simple que no ejecute c�digo complejo
                _placeholderPage = new ContentPage
                {
                    Content = new StackLayout
                    {
                        Children = {
                            new ActivityIndicator
                            {
                                IsRunning = true,
                                VerticalOptions = LayoutOptions.Center,
                                HorizontalOptions = LayoutOptions.Center,
                                Color = Colors.Blue
                            },
                            new Label
                            {
                                Text = "Cargando...",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                TextColor = Colors.Gray,
                                FontSize = 14
                            }
                        },
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }
                };

                // CORRECCI�N: Crear NavigationPage que envuelva el placeholder
                var placeholderNavPage = new NavigationPage(_placeholderPage)
                {
                    Title = describirNavPage.Title,
                    IconImageSource = describirNavPage.IconImageSource,
                    BarBackgroundColor = describirNavPage.BarBackgroundColor,
                    BarTextColor = describirNavPage.BarTextColor
                };

                // Reemplazar NavigationPage completo con placeholder que tambi�n es NavigationPage
                Children.RemoveAt(describirIndex);
                Children.Insert(describirIndex, placeholderNavPage);

                System.Diagnostics.Debug.WriteLine("SospectTabs: DescribirPage reemplazada con placeholder dentro de NavigationPage");
            }
        }

        protected override void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();

            // CORRECCI�N: Verificar si el CurrentPage es el NavigationPage del placeholder
            if (!_describirPageLoaded && CurrentPage is NavigationPage navPage && navPage.CurrentPage == _placeholderPage)
            {
                // Usar Device.BeginInvokeOnMainThread para asegurar que la UI est� lista
                Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(async () =>
                {
                    await LoadDescribirPageAsync();
                });
            }

            // Fix tab Map: siempre garantizar que HomePage muestre el mapa al seleccionar el tab
            if (CurrentPage is NavigationPage mapNavPage && mapNavPage.CurrentPage is HomePage homePage)
            {
                // Restaurar las barras (por si estaban ocultas en el feed de Describir)
                MainThread.BeginInvokeOnMainThread(() => SetTabBarVisibility(true));

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await homePage.EnsureMapVisible();
                });
            }
        }

        private async Task LoadDescribirPageAsync()
        {
            if (_describirPageLoaded || _placeholderPage == null)
                return;

            try
            {
                _describirPageLoaded = true;
                System.Diagnostics.Debug.WriteLine("SospectTabs: Iniciando carga diferida de DescribirPage");

                // Crear la p�gina real
                var describirPage = new DescribirPage();

                // CORRECCI�N: Encontrar el NavigationPage que contiene el placeholder
                NavigationPage placeholderNavPage = null;
                int index = -1;

                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i] is NavigationPage navPage && navPage.CurrentPage == _placeholderPage)
                    {
                        placeholderNavPage = navPage;
                        index = i;
                        break;
                    }
                }

                if (index >= 0 && placeholderNavPage != null)
                {
                    // CORRECCI�N: Crear nuevo NavigationPage con DescribirPage
                    var describirNavPage = new NavigationPage(describirPage)
                    {
                        Title = placeholderNavPage.Title,
                        IconImageSource = placeholderNavPage.IconImageSource,
                        BarBackgroundColor = placeholderNavPage.BarBackgroundColor,
                        BarTextColor = placeholderNavPage.BarTextColor
                    };

                    // Reemplazar NavigationPage del placeholder con NavigationPage de DescribirPage
                    Children.RemoveAt(index);
                    Children.Insert(index, describirNavPage);
                    CurrentPage = describirNavPage;

                    System.Diagnostics.Debug.WriteLine("SospectTabs: DescribirPage cargada exitosamente dentro de NavigationPage");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "SospectTabs", "LoadDescribirPageAsync");
                System.Diagnostics.Debug.WriteLine($"SospectTabs: Error cargando DescribirPage: {ex.Message}");

                // Fallback: mantener placeholder pero mostrar error
                if (_placeholderPage?.Content is StackLayout stack)
                {
                    stack.Children.Clear();
                    stack.Children.Add(new Label
                    {
                        Text = "Error cargando p�gina",
                        TextColor = Colors.Red,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    });
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("SospectTabs: OnAppearing");
            // ELIMINADO: Las suscripciones a AlarmaLanzadaExitosamente y RefrescarConGestos
            // causaban un bucle infinito porque reenviaban los mismos mensajes que recibían.
            // LanzarAlarmaViewModel ahora llama directamente a homePage.RefrescarDespuesDeAlarma()
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine("SospectTabs: OnDisappearing");
            MessagingCenter.Unsubscribe<object, bool>(this, "TabBarVisible");
        }
    }
}

