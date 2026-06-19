// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// ViewModels/ConfiguracionParaTiViewModel.cs
// Creado: 23-02-2026
// Propósito: ViewModel para la página de personalización del feed "Para Ti".
// Permite al usuario seleccionar países de interés y la cantidad de alarmas por refresco.
// MODIFICADO 2026-02-26: Cargar/guardar países y límite desde BD (actualizaparametrosnotificacion).
// MODIFICADO 2026-02-26: Usar ISO alpha-2 en paises_feed_filtro + PaisId en PaisSeleccionable.
// MODIFICADO 2026-02-28: Logs de diagnóstico detallados + fix checkbox "Todos" nunca se activa por defecto.
// MODIFICADO 2026-02-28b: Usar fecha_act_configuracion_notif para distinguir "nunca configuró" vs "eligió todos".

using System.Collections.ObjectModel;
using System.Windows.Input;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;

namespace sospect.ViewModels
{
    public class ConfiguracionParaTiViewModel : BaseViewModel
    {
        // ─── ESTADO CARGADO DESDE BD (para no sobrescribir otros campos al guardar) ─
        private ConsultaConfNotifResponse? _configActual;

        // ─── PROPIEDADES ──────────────────────────────────────────────────────────

        private ObservableCollection<PaisSeleccionable> _paises = new();
        public ObservableCollection<PaisSeleccionable> Paises
        {
            get => _paises;
            set => SetProperty(ref _paises, value);
        }

        // FIX 2026-02-27: Lista como strings para evitar el problema de box-equality del Picker de MAUI con int.
        // SelectedItem en MAUI compara por referencia para tipos en caja (int boxed), por lo que
        // nunca matchea aunque el valor sea igual. Usando string se garantiza igualdad por valor.
        private static readonly int[] _valoresLimite = { 10, 20, 25, 50, 75, 100, 125, 150, 175, 200, 300, 500 };

        private ObservableCollection<string> _limitesOptions = new(_valoresLimite.Select(v => v.ToString()));
        public ObservableCollection<string> LimitesOptions
        {
            get => _limitesOptions;
            set => SetProperty(ref _limitesOptions, value);
        }

        private string _limiteSeleccionado = "100";
        public string LimiteSeleccionado
        {
            get => _limiteSeleccionado;
            set
            {
                if (SetProperty(ref _limiteSeleccionado, value))
                    OnPropertyChanged(nameof(MostrarAdvertenciaRendimiento));
            }
        }

        // Valor entero para usar en el request al API
        public int LimiteSeleccionadoInt =>
            int.TryParse(LimiteSeleccionado, out var v) ? v : 100;

        public bool MostrarAdvertenciaRendimiento =>
            int.TryParse(LimiteSeleccionado, out var v) && v > 100;

        // FIX 2026-02-27: Valor inicial false. La casilla "Todos" se activa solo si el usuario
        // ya configuró antes (fecha_act_configuracion_notif != null) y no tiene filtro de países.
        // MODIFICADO 2026-02-28c: cuando "Todos" = true, se marcan TAMBIÉN los checkboxes individuales.
        private bool _todosLosPaisesSeleccionados = false;
        public bool TodosLosPaisesSeleccionados
        {
            get => _todosLosPaisesSeleccionados;
            set
            {
                if (SetProperty(ref _todosLosPaisesSeleccionados, value) && value)
                {
                    // Marcar todos los países individuales también (UX: el usuario ve todos marcados)
                    foreach (var pais in Paises)
                        pais.Seleccionado = true;
                }
            }
        }

        // ─── COMANDOS ─────────────────────────────────────────────────────────────

        public ICommand GuardarCommand { get; }
        public ICommand TogglePaisCommand { get; }

        // ─── CONSTRUCTOR ──────────────────────────────────────────────────────────

        public ConfiguracionParaTiViewModel()
        {
            GuardarCommand = new Command(async () => await GuardarAsync());
            TogglePaisCommand = new Command<PaisSeleccionable>(TogglePais);
            _ = CargarAsync();
        }

        // ─── MÉTODOS ──────────────────────────────────────────────────────────────

        private async Task CargarAsync()
        {
            try
            {
                IsRunning = true;

                System.Diagnostics.Debug.WriteLine("[ConfigParaTi] CargarAsync — inicio");

                if (App.persona?.user_id_thirdparty == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ConfigParaTi] App.persona es null — no se puede cargar");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] Cargando config para user: {App.persona.user_id_thirdparty}");

                // Cargar configuración actual desde BD
                _configActual = await ApiService.ConsultarConfiguracionNotificaciones(App.persona.user_id_thirdparty);

                if (_configActual == null)
                    System.Diagnostics.Debug.WriteLine("[ConfigParaTi] ConsultarConfiguracionNotificaciones retornó NULL");
                else
                    System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] Config cargada: pais_usuario={_configActual.pais_usuario}, limite={_configActual.limite_alarmas_feed}, paises_feed_filtro={(_configActual.paises_feed_filtro == null ? "NULL" : string.Join(",", _configActual.paises_feed_filtro))}");

                // Cargar límite desde BD — guardarlo como string para que el Picker lo matchee correctamente
                var limiteDb = (_configActual?.limite_alarmas_feed ?? 0) > 0
                    ? _configActual!.limite_alarmas_feed!.Value
                    : 100;
                // Si el valor de BD no está en la lista, usar 100
                LimiteSeleccionado = _valoresLimite.Contains(limiteDb)
                    ? limiteDb.ToString()
                    : "100";
                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] LimiteSeleccionado={LimiteSeleccionado}");

                // Cargar países del API
                System.Diagnostics.Debug.WriteLine("[ConfigParaTi] Llamando ObtenerPaisesConUsuarios...");
                var paisesApi = await ApiService.ObtenerPaisesConUsuarios();
                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] ObtenerPaisesConUsuarios retornó {paisesApi?.Count ?? 0} países");

                if (paisesApi == null || paisesApi.Count == 0)
                    System.Diagnostics.Debug.WriteLine("[ConfigParaTi] ADVERTENCIA: La lista de países está vacía. Verificar que el API esté desplegado y que personas.pais tenga datos en BD dev.");

                // Leer selección previa desde BD (ahora son ISO alpha-2: ["CO","MX"])
                var paisesGuardados = _configActual?.paises_feed_filtro ?? new List<string>();
                bool sinFiltro = !paisesGuardados.Any();
                // País del usuario (ISO alpha-2) para preseleccionar si aún no tiene filtro configurado
                string paisUsuario = _configActual?.pais_usuario ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] sinFiltro={sinFiltro}, paisUsuario={paisUsuario}, paisesGuardados={string.Join(",", paisesGuardados)}");

                var lista = (paisesApi ?? new List<sospect.Models.PaisUsuarios>()).Select(p => new PaisSeleccionable
                {
                    PaisId = p.pais_id ?? string.Empty,          // ISO alpha-2: "CO", "MX"
                    Pais = p.pais ?? string.Empty,               // Nombre legible: "Colombia"
                    CantidadUsuarios = p.cantidad_usuarios,
                    // Preseleccionar si: pais_id está en el filtro guardado (ISO alpha-2),
                    //   o (sin filtro y el pais_id coincide con el país del usuario)
                    Seleccionado = paisesGuardados.Contains(p.pais_id ?? string.Empty)
                                || (sinFiltro && !string.IsNullOrEmpty(paisUsuario)
                                    && string.Equals(p.pais_id, paisUsuario, StringComparison.OrdinalIgnoreCase))
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] Lista construida: {lista.Count} países, {lista.Count(p => p.Seleccionado)} seleccionados");

                Paises = new ObservableCollection<PaisSeleccionable>(lista);

                // FIX 2026-02-28b: Distinguir "nunca configuró" vs "eligió ver todos los países".
                // - Si fecha_act_configuracion_notif IS NULL → primer acceso, nunca guardó → "Todos" = false
                // - Si ya guardó antes (fecha != null) y paises_feed_filtro IS NULL → eligió "Todos" → true
                // - Si ya guardó antes y tiene países específicos → mostrarlos seleccionados, "Todos" = false
                bool hayConfiguracionPrevia = _configActual?.fecha_act_configuracion_notif != null;
                TodosLosPaisesSeleccionados = hayConfiguracionPrevia && !paisesGuardados.Any();
                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] hayConfiguracionPrevia={hayConfiguracionPrevia}, TodosLosPaisesSeleccionados={TodosLosPaisesSeleccionados}");
                System.Diagnostics.Debug.WriteLine("[ConfigParaTi] CargarAsync — completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigParaTi] CargarAsync EXCEPCIÓN: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ConfiguracionParaTiViewModel", "CargarAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void TogglePais(PaisSeleccionable pais)
        {
            if (pais == null) return;
            pais.Seleccionado = !pais.Seleccionado;
            // Cualquier toque en un país individual desactiva "Todos" (si estaba activo).
            // Usamos el campo privado para el if y la propiedad pública para asignar:
            // con value=false el setter NO dispara mark-all, por lo que no hay ciclo.
            // NOTA: se eliminó la auto-activación de "Todos" cuando quedan 0 seleccionados,
            // porque con el nuevo setter eso crearía un ciclo (todos se volverían a marcar).
            if (_todosLosPaisesSeleccionados)
                TodosLosPaisesSeleccionados = false;
        }

        private async Task GuardarAsync()
        {
            try
            {
                IsRunning = true;

                if (App.persona?.user_id_thirdparty == null || _configActual == null) return;

                // Guardar ISO alpha-2 en paises_feed_filtro (ej: ["CO","MX"])
                var paisesSeleccionados = TodosLosPaisesSeleccionados
                    ? null  // null = sin filtro = todos los países
                    : Paises.Where(p => p.Seleccionado).Select(p => p.PaisId).ToList();

                // Mantener todos los demás valores de configuración intactos
                var request = new ActualizaConfNotifRequest
                {
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_notif_alarma_cercana_habilitada = _configActual.notif_alarma_cercana_habilitada,
                    p_notif_alarma_protegido_habilitada = _configActual.notif_alarma_protegido_habilitada,
                    p_notif_alarma_zona_vigilancia_habilitada = _configActual.notif_alarma_zona_vigilancia_habilitada,
                    p_notif_alarma_policia_habilitada = _configActual.notif_alarma_policia_habilitada,
                    p_dias_notif_policia_apagada = _configActual.dias_notif_policia_apagada,
                    p_limite_alarmas_feed = LimiteSeleccionadoInt,
                    p_intervalo_background_minutos = _configActual.intervalo_background_minutos > 0
                        ? _configActual.intervalo_background_minutos
                        : 5,
                    p_paises_feed_filtro = paisesSeleccionados
                };

                var response = await ApiService.ActualizarConfiguracionNotificaciones(request);

                if (response?.IsSuccess == true)
                {
                    // Actualizar límite en Preferences (para que App lo lea de inmediato)
                    var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosGuardados))
                    {
                        var parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                        if (parametros != null)
                        {
                            parametros.LimiteAlarmasFeed = LimiteSeleccionadoInt;
                            Preferences.Set("ParametrosUsuario", JsonConvert.SerializeObject(parametros));
                        }
                    }

                    var labelExito = TranslateExtension.Translate("LabelExito");
                    var lblGuardado = TranslateExtension.Translate("LblConfNotifGuardadas");
                    await ModernAlerts.ShowSuccess(labelExito, lblGuardado);
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
                else
                {
                    var lblError = TranslateExtension.Translate("MensajeError");
                    await ModernAlerts.ShowWarning(TranslateExtension.Translate("LabelInformacion"), lblError);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ConfiguracionParaTiViewModel", "GuardarAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Modelo de país con estado de selección para el CollectionView
    /// MODIFICADO 2026-02-26: agrega PaisId (ISO alpha-2) para guardar en paises_feed_filtro
    /// </summary>
    public class PaisSeleccionable : BaseViewModel
    {
        public string PaisId { get; set; } = string.Empty;      // ISO alpha-2: "CO", "MX"
        public string Pais { get; set; } = string.Empty;        // Nombre legible: "Colombia"
        public int CantidadUsuarios { get; set; }

        private bool _seleccionado;
        public bool Seleccionado
        {
            get => _seleccionado;
            set => SetProperty(ref _seleccionado, value);
        }

        public string PaisConCantidad => $"{Pais} ({CantidadFormateada})";

        public string CantidadFormateada => CantidadUsuarios >= 1000
            ? $"{CantidadUsuarios / 1000.0:0.#}k"
            : CantidadUsuarios.ToString();
    }
}


