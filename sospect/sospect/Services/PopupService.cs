using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace sospect.Services
{
    public interface IPopupService
    {
        Task PushAsync(Popup popup);
        Task PopAsync();
    }

    public class PopupService : IPopupService
    {
        public async Task PushAsync(Popup popup)
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
            {
                await currentPage.ShowPopupAsync(popup);
            }
        }

        public async Task PopAsync()
        {
            // En CommunityToolkit.Maui, los popups se cierran automáticamente
            await Task.CompletedTask;
        }

        private Page GetCurrentPage()
        {
            var mainPage = Application.Current?.MainPage;

            // Navegar hasta la página más profunda
            Page currentPage = mainPage;

            while (currentPage != null)
            {
                if (currentPage is NavigationPage navPage && navPage.CurrentPage != null)
                {
                    currentPage = navPage.CurrentPage;
                }
                else if (currentPage is TabbedPage tabbedPage && tabbedPage.CurrentPage != null)
                {
                    currentPage = tabbedPage.CurrentPage;
                }
                else if (currentPage is FlyoutPage flyoutPage && flyoutPage.Detail != null)
                {
                    currentPage = flyoutPage.Detail;
                }
                else
                {
                    break;
                }
            }

            return currentPage;
        }
    }
}