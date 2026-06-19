// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class TrustedNetworkViewModel : BaseViewModel
    {
        private bool _showAddBasicDataButton;
        public bool ShowAddBasicDataButton
        {
            get => _showAddBasicDataButton;
            set => SetValue(ref _showAddBasicDataButton, value);
        }

        private bool _showModifyBasicDataButton;
        public bool ShowModifyBasicDataButton
        {
            get => _showModifyBasicDataButton;
            set => SetValue(ref _showModifyBasicDataButton, value);
        }

        private bool _showAddUserToTrustedNetworkButton;
        public bool ShowAddUserToTrustedNetworkButton
        {
            get => _showAddUserToTrustedNetworkButton;
            set => SetValue(ref _showAddUserToTrustedNetworkButton, value);
        }

        private bool _showListUsersInTrustedNetworkButton;
        public bool ShowListUsersInTrustedNetworkButton
        {
            get => _showListUsersInTrustedNetworkButton;
            set => SetValue(ref _showListUsersInTrustedNetworkButton, value);
        }

        public ICommand AddBasicDataCommand { get; }
        public ICommand ModifyBasicDataCommand { get; }
        public ICommand AddUserToTrustedNetworkCommand { get; }
        public ICommand ListUsersInTrustedNetworkCommand { get; }
        public ICommand ShowMoreInfoCommand { get; }
        public ICommand ShowAdvantagesCommand { get; }
        public ICommand ShowDisadvantagesCommand { get; }


        public TrustedNetworkViewModel(INavigation navigation)
        {
            InitializeAsync(navigation);
            AddBasicDataCommand = new Command(async () => await AddBasicData(navigation));
            AddUserToTrustedNetworkCommand = new Command(async () => await AddUserToTrustedNetwork(navigation));
            ListUsersInTrustedNetworkCommand = new Command(async () => await ListUsersInTrustedNetwork(navigation));

            ShowMoreInfoCommand = new Command(async () => await ShowMoreInfo(navigation));
            ShowAdvantagesCommand = new Command(async () => await ShowAdvantages(navigation));
            ShowDisadvantagesCommand = new Command(async () => await ShowDisadvantages(navigation));
        }

        private async Task InitializeAsync(INavigation navigation)
        {
            try
            {
                await HomeViewModel.InicializarParametrosUsuarioAsync();
                var parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                UpdateButtonVisibility(parametros.flag_red_confianza);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "TrustedNetworkViewModel", "InitializeAsync");
            }
        }

        private void UpdateButtonVisibility(bool isInTrustedNetwork)
        {
            ShowAddBasicDataButton = !isInTrustedNetwork;
            ShowModifyBasicDataButton = !isInTrustedNetwork;
            ShowAddUserToTrustedNetworkButton = isInTrustedNetwork;
            ShowListUsersInTrustedNetworkButton = isInTrustedNetwork;
        }

        private async Task ShowMoreInfo(INavigation navigation)
        {
            await navigation.PushAsync(new MoreInfoPage());
        }

        private async Task ShowAdvantages(INavigation navigation)
        {
            await navigation.PushAsync(new AdvantagesPage());
        }

        private async Task ShowDisadvantages(INavigation navigation)
        {
            await navigation.PushAsync(new DisadvantagesPage());
        }

        private async Task AddBasicData(INavigation navigation)
        {
            await navigation.PushAsync(new AgregarDatosBasicosPage());
        }

        private async Task AddUserToTrustedNetwork(INavigation navigation)
        {
            await navigation.PushAsync(new AgregarUsuarioRedConfianzaPage());
        }

        private async Task ListUsersInTrustedNetwork(INavigation navigation)
        {
            await navigation.PushAsync(new ListarMiRedConfianzaPage());
        }
    }
}


