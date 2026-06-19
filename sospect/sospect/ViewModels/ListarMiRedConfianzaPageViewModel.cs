// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using sospect.Helpers;

namespace sospect.ViewModels
{
    public class ListarMiRedConfianzaPageViewModel : BaseViewModel
    {
        private bool _isListEmpty;
        private ObservableCollection<UsuarioRedConfianza> _usuariosRedConfianza;
        private ParametrosUsuario _parametros;

        public ObservableCollection<UsuarioRedConfianza> UsuariosRedConfianza
        {
            get => _usuariosRedConfianza;
            set => SetProperty(ref _usuariosRedConfianza, value);
        }

        public bool IsListEmpty
        {
            get => _isListEmpty;
            set => SetProperty(ref _isListEmpty, value);
        }

        public ListarMiRedConfianzaPageViewModel()
        {
            _parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
            UsuariosRedConfianza = new ObservableCollection<UsuarioRedConfianza>();
            _ = Task.Run(() => LoadUsuariosRedConfianza());
        }

        private async Task LoadUsuariosRedConfianza()
        {
            IsRunning = true;
            try
            {
                var request = new ListarUsuariosRedConfianzaRequest
                {
                    UserIdThirdpartyLider = App.persona.user_id_thirdparty
                };

                var usuarios = await ApiService.ListarUsuariosRedConfianza(request);

                if (usuarios != null && usuarios.Count > 0)
                {
                    foreach (var usuario in usuarios)
                    {
                        UsuariosRedConfianza.Add(usuario);
                    }
                    IsListEmpty = false;
                }
                else
                {
                    IsListEmpty = true;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ListarMiRedConfianzaPageViewModel", "LoadUsuariosRedConfianza");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}


