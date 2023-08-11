using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sospect.Models;
using sospect.Services;
using sospect.Views;

namespace sospect.ViewModels
{
    public class SubscriptionValuesPageViewModel : BaseViewModel
    {
        private ObservableCollection<SubscriptionValue> _subscriptionValues;
        public ObservableCollection<SubscriptionValue> SubscriptionValues
        {
            get => _subscriptionValues;
            set => SetProperty(ref _subscriptionValues, value);
        }

        public SubscriptionValuesPageViewModel()
        {
            SubscriptionValues = new ObservableCollection<SubscriptionValue>();
            LoadSubscriptionValuesAsync();
        }

        private async void LoadSubscriptionValuesAsync()
        {
            IsRunning = true;
            var response = await ApiService.ObtenerValoresDeSubscripcion();
            IsRunning = false;

            if (response != null)
            {
                foreach (var value in response)
                {
                    SubscriptionValues.Add(value);
                }
            }
        }
    }
}