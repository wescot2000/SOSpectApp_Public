using System.Collections.Generic;
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
            try
            {
                var response = await ApiService.ObtenerValoresDeSubscripcion();
                
                if (response != null)
                {
                    foreach (var value in response)
                    {
                        SubscriptionValues.Add(value);
                    }
                }
            }
            catch (System.Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "SubscriptionValuesPageViewModel" },
                        { "Method", "ObtenerValoresDeSubscripcion" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
            
        }
    }
}