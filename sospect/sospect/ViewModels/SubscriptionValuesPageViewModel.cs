using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using Microsoft.Maui.Controls;

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
                CrashlyticsHelper.LogError(ex, "SubscriptionValuesPageViewModel", "LoadSubscriptionValuesAsync");
            }
            finally
            {
                IsRunning = false;
            }
            
        }
    }
}
