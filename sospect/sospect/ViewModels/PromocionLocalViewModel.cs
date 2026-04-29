using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using CommunityToolkit.Mvvm.Input;

namespace sospect.ViewModels
{
#if ANDROID
    [Android.Runtime.Preserve(AllMembers = true)]
#elif IOS || MACCATALYST
    [Foundation.Preserve(AllMembers = true)]
#endif
    public class PromocionLocalViewModel : BaseViewModel
    {
        #region Properties

        private double _latitud;
        public double Latitud
        {
            get => _latitud;
            set => SetValue(ref _latitud, value);
        }

        private double _longitud;
        public double Longitud
        {
            get => _longitud;
            set => SetValue(ref _longitud, value);
        }

        private int _saldoInicial;
        public int SaldoInicial
        {
            get => _saldoInicial;
            set => SetValue(ref _saldoInicial, value);
        }

        private int _costoActual = 40;
        public int CostoActual
        {
            get => _costoActual;
            set
            {
                SetValue(ref _costoActual, value);
                OnPropertyChanged(nameof(SaldoResultante));
            }
        }

        public int SaldoResultante => _saldoInicial - _costoActual;

        private int _radioMetros = 100;
        public int RadioMetros
        {
            get => _radioMetros;
            set
            {
                SetValue(ref _radioMetros, value);
                CalcularCosto();
                OnPropertyChanged(nameof(LabelRadio));
            }
        }

        public string LabelRadio => $"Radius: {_radioMetros} meters";

        private int _duracionDias = 1;
        public int DuracionDias
        {
            get => _duracionDias;
            set
            {
                SetValue(ref _duracionDias, value);
                CalcularCosto();
                OnPropertyChanged(nameof(LabelDuracion));
            }
        }

        public string LabelDuracion => $"Duration: {_duracionDias} days";

        private bool _logoHabilitado = false;
        public bool LogoHabilitado
        {
            get => _logoHabilitado;
            set
            {
                SetValue(ref _logoHabilitado, value);
                CalcularCosto();
            }
        }

        private bool _contactoHabilitado = false;
        public bool ContactoHabilitado
        {
            get => _contactoHabilitado;
            set
            {
                SetValue(ref _contactoHabilitado, value);
                CalcularCosto();
            }
        }

        private bool _domicilioHabilitado = false;
        public bool DomicilioHabilitado
        {
            get => _domicilioHabilitado;
            set
            {
                SetValue(ref _domicilioHabilitado, value);
                CalcularCosto();
            }
        }

        private bool _aceptoTerminosChat = false;
        public bool AceptoTerminosChat
        {
            get => _aceptoTerminosChat;
            set => SetValue(ref _aceptoTerminosChat, value);
        }

        private int _usuariosPush = 0;
        public int UsuariosPush
        {
            get => _usuariosPush;
            set
            {
                SetValue(ref _usuariosPush, value);
                CalcularCosto();
                OnPropertyChanged(nameof(LabelUsuariosPush));
            }
        }

        public string LabelUsuariosPush => $"Notify {_usuariosPush} users";

        // Texto personalizado para la notificación push - AGREGADO 2026-01-29
        private string _textoPush;
        public string TextoPush
        {
            get => _textoPush;
            set => SetValue(ref _textoPush, value);
        }

        private int _usuariosDisponibles = 0;
        public int UsuariosDisponibles
        {
            get => _usuariosDisponibles;
            set
            {
                SetValue(ref _usuariosDisponibles, value);
                OnPropertyChanged(nameof(LabelUsuariosDisponibles));
            }
        }

        public string LabelUsuariosDisponibles => $"Users available in {_radioMetros}m: {_usuariosDisponibles}";

        private string _descripcion;
        public string Descripcion
        {
            get => _descripcion;
            set => SetValue(ref _descripcion, value);
        }

        private List<ConteoUsuariosPorRadio> _conteosUsuarios = new List<ConteoUsuariosPorRadio>();

        // Parámetros de costos (desde parametros_usuario) - Públicos para acceso desde code-behind
        public int CostoBasePromocion { get; private set; } = 10;
        public int CostoPor500mExtra { get; private set; } = 5;
        public int CostoPorDiaExtra { get; private set; } = 2;
        public int CostoLogo { get; private set; } = 2;
        public int CostoContacto { get; private set; } = 1;
        public int CostoDomicilio { get; private set; } = 1;
        public int CostoPor50UsuariosPush { get; private set; } = 5;

        // Multimedia
        private List<string> _rutasMultimedia = new List<string>();
        private string _rutaLogo = null;
        private bool _usarLogoPersonalizado = false;

        // Datos del NIT y emprendimiento
        private string _nitCedulaPropietario;
        public string NitCedulaPropietario
        {
            get => _nitCedulaPropietario;
            set => SetValue(ref _nitCedulaPropietario, value);
        }

        private string _nombreEmprendimiento;
        public string NombreEmprendimiento
        {
            get => _nombreEmprendimiento;
            set => SetValue(ref _nombreEmprendimiento, value);
        }

        private string _nombrePropietario;
        public string NombrePropietario
        {
            get => _nombrePropietario;
            set => SetValue(ref _nombrePropietario, value);
        }

        private bool _declaroPropietario;
        public bool DeclaroPropietario
        {
            get => _declaroPropietario;
            set => SetValue(ref _declaroPropietario, value);
        }

        // Control de UI
        private bool _camposEmprendimientoHabilitados = false;
        public bool CamposEmprendimientoHabilitados
        {
            get => _camposEmprendimientoHabilitados;
            set => SetValue(ref _camposEmprendimientoHabilitados, value);
        }

        private bool _logoHabilitadoEdicion = false;
        public bool LogoHabilitadoEdicion
        {
            get => _logoHabilitadoEdicion;
            set => SetValue(ref _logoHabilitadoEdicion, value);
        }

        private bool _mostrarCheckboxPropietario = false;
        public bool MostrarCheckboxPropietario
        {
            get => _mostrarCheckboxPropietario;
            set => SetValue(ref _mostrarCheckboxPropietario, value);
        }

        private string _mensajeVerificacionNIT;
        public string MensajeVerificacionNIT
        {
            get => _mensajeVerificacionNIT;
            set => SetValue(ref _mensajeVerificacionNIT, value);
        }

        private Color _colorMensajeNIT;
        public Color ColorMensajeNIT
        {
            get => _colorMensajeNIT;
            set => SetValue(ref _colorMensajeNIT, value);
        }

        // Estado interno
        private bool _esNuevoEmprendimiento = false;
        private bool _esPropietario = false;
        private long _idEmprendimiento = 0;
        public long IdEmprendimiento => _idEmprendimiento; // Expuesto para acceso desde la vista (pre-llenado)
        public string _urlLogoExistente = null; // PUBLIC para acceso desde PromocionLocalPage.xaml.cs

        private bool _nitVerificado = false;
        public bool NitVerificado
        {
            get => _nitVerificado;
            set => SetValue(ref _nitVerificado, value);
        }

        #endregion

        #region Commands

        public IRelayCommand VerificarNITCommand { get; }
        public IRelayCommand LanzarPromocionCommand { get; }
        public IRelayCommand CancelarCommand { get; }

        #endregion

        #region Constructor

        public PromocionLocalViewModel(double latitud, double longitud)
        {
            _latitud = latitud;
            _longitud = longitud;

            // Obtener saldo y parámetros de usuario desde Preferences
            var parametrosJson = Preferences.Get("ParametrosUsuario", "");
            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] parametrosJson vacío: {string.IsNullOrEmpty(parametrosJson)}");
            if (!string.IsNullOrEmpty(parametrosJson))
            {
                try
                {
                    var parametros = Newtonsoft.Json.JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosJson);
                    if (parametros != null)
                    {
                        _saldoInicial = parametros.SaldoPoderes;
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Costos desde API -> Base:{parametros.CostoBasePromocion} Logo:{parametros.CostoLogo} Contacto:{parametros.CostoContacto} Domicilio:{parametros.CostoDomicilio} 500m:{parametros.CostoPor500mExtra} Dia:{parametros.CostoPorDiaExtra} Push:{parametros.CostoPor50UsuariosPush}");
                        CostoBasePromocion = parametros.CostoBasePromocion ?? 10;
                        CostoPor500mExtra = parametros.CostoPor500mExtra ?? 5;
                        CostoPorDiaExtra = parametros.CostoPorDiaExtra ?? 2;
                        CostoLogo = parametros.CostoLogo ?? 2;
                        CostoContacto = parametros.CostoContacto ?? 1;
                        CostoDomicilio = parametros.CostoDomicilio ?? 1;
                        CostoPor50UsuariosPush = parametros.CostoPor50UsuariosPush ?? 5;
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Costos aplicados -> Base:{CostoBasePromocion} Logo:{CostoLogo} Contacto:{CostoContacto} Domicilio:{CostoDomicilio} 500m:{CostoPor500mExtra} Dia:{CostoPorDiaExtra} Push:{CostoPor50UsuariosPush}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] parametros deserializado es NULL");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error parseando ParametrosUsuario: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] No hay ParametrosUsuario en Preferences, usando defaults");
            }

            VerificarNITCommand = new AsyncRelayCommand(VerificarNIT);
            LanzarPromocionCommand = new AsyncRelayCommand(LanzarPromocionAsync);
            CancelarCommand = new AsyncRelayCommand(CancelarAsync);

            // Cargar conteos de usuarios
            _ = CargarConteosUsuariosAsync();
        }

        #endregion

        #region Methods

        private async Task CargarConteosUsuariosAsync()
        {
            try
            {
                IsRunning = true;

                var request = new ObtenerConteosPorIntervalosRequest
                {
                    latitud = _latitud,
                    longitud = _longitud,
                    radio_maximo_metros = 5000,
                    intervalo_metros = 500
                };

                var response = await ApiService.ObtenerConteosPorIntervalos(request);

                if (response.IsSuccess && response.Conteos != null)
                {
                    _conteosUsuarios = response.Conteos;

                    // Calcular usuarios disponibles para el radio actual
                    _usuariosDisponibles = ObtenerUsuariosPorRadioInterpolado(_radioMetros);
                    OnPropertyChanged(nameof(UsuariosDisponibles));
                    OnPropertyChanged(nameof(LabelUsuariosDisponibles));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error cargando conteos: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PromocionLocalViewModel", "CargarConteosUsuariosAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private int ObtenerUsuariosPorRadioInterpolado(int radioMetros)
        {
            if (_conteosUsuarios == null || !_conteosUsuarios.Any())
                return 0;

            // Buscar el intervalo exacto
            var exacto = _conteosUsuarios.FirstOrDefault(c => c.RadioMetros == radioMetros);
            if (exacto != null)
                return exacto.CantidadUsuarios;

            // Buscar los dos puntos más cercanos para interpolar
            var menorIgual = _conteosUsuarios.Where(c => c.RadioMetros <= radioMetros)
                                            .OrderByDescending(c => c.RadioMetros)
                                            .FirstOrDefault();

            var mayorIgual = _conteosUsuarios.Where(c => c.RadioMetros >= radioMetros)
                                            .OrderBy(c => c.RadioMetros)
                                            .FirstOrDefault();

            // Si solo hay un lado, devolver ese valor
            if (menorIgual == null && mayorIgual != null)
                return mayorIgual.CantidadUsuarios;

            if (mayorIgual == null && menorIgual != null)
                return menorIgual.CantidadUsuarios;

            if (menorIgual == null && mayorIgual == null)
                return 0;

            // Interpolación lineal: y = y1 + (x - x1) * (y2 - y1) / (x2 - x1)
            int x = radioMetros;
            int x1 = menorIgual.RadioMetros;
            int x2 = mayorIgual.RadioMetros;
            int y1 = menorIgual.CantidadUsuarios;
            int y2 = mayorIgual.CantidadUsuarios;

            if (x2 == x1)
                return y1;

            double interpolado = y1 + ((double)(x - x1) * (y2 - y1) / (x2 - x1));
            return (int)Math.Round(interpolado);
        }

        private void CalcularCosto()
        {
            // Costo base
            int costo = CostoBasePromocion;

            // Costo por radio extra (cada 500m sobre los primeros 100m)
            if (_radioMetros > 100)
            {
                int metrosExtras = _radioMetros - 100;
                int bloques500m = (int)Math.Ceiling(metrosExtras / 500.0);
                costo += bloques500m * CostoPor500mExtra;
            }

            // Costo por días extra (días 2 en adelante)
            if (_duracionDias > 1)
            {
                costo += (_duracionDias - 1) * CostoPorDiaExtra;
            }

            // Costo logo
            if (_logoHabilitado)
            {
                costo += CostoLogo;
            }

            // Costo contacto
            if (_contactoHabilitado)
            {
                costo += CostoContacto;
            }

            // Costo domicilio
            if (_domicilioHabilitado)
            {
                costo += CostoDomicilio;
            }

            // Costo push
            if (_usuariosPush > 0)
            {
                int bloques50Usuarios = (int)Math.Ceiling(_usuariosPush / 50.0);
                costo += bloques50Usuarios * CostoPor50UsuariosPush;
            }

            CostoActual = costo;
        }

        public void SetMultimedia(List<string> rutasMultimedia, string rutaLogo, bool usarLogoPersonalizado)
        {
            _rutasMultimedia = rutasMultimedia ?? new List<string>();
            _rutaLogo = rutaLogo;
            _usarLogoPersonalizado = usarLogoPersonalizado;

            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] === SetMultimedia LLAMADO ===");
            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Multimedia recibido: {_rutasMultimedia.Count} archivos");
            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Logo ruta: {_rutaLogo ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Usar logo personalizado: {_usarLogoPersonalizado}");

            // Verificar si el archivo de logo existe
            if (!string.IsNullOrEmpty(_rutaLogo))
            {
                if (File.Exists(_rutaLogo))
                {
                    var fileInfo = new FileInfo(_rutaLogo);
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Archivo de logo EXISTE: {fileInfo.Length} bytes");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] ADVERTENCIA: Archivo de logo NO EXISTE en: {_rutaLogo}");
                }
            }
        }

        private async Task VerificarNIT()
        {
            // 1. Validar que NIT no esté vacío
            if (string.IsNullOrWhiteSpace(NitCedulaPropietario))
            {
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var nitRequerido = await TranslateExtension.TranslateAsync("NITRequerido");
                await ModernAlerts.ShowError(labelError, nitRequerido);
                return;
            }

            try
            {
                IsRunning = true;

                // 2. Llamar al API: POST /Emprendimientos/ConsultarPorNIT
                var request = new ConsultarEmprendimientoRequest
                {
                    nit_cedula_propietario = NitCedulaPropietario,
                    p_user_id_thirdparty = App.persona?.user_id_thirdparty ?? ""
                };

                var response = await ApiService.ConsultarEmprendimientoPorNIT(request);

                if (!response.IsSuccess)
                {
                    var labelError = await TranslateExtension.TranslateAsync("LabelError");
                    await ModernAlerts.ShowError(labelError, response.Message);
                    return;
                }

                // 3. Procesar respuesta según 3 casos
                if (!response.existe)
                {
                    // CASO 1: Emprendimiento NO existe - Usuario será propietario
                    _esNuevoEmprendimiento = true;
                    _esPropietario = true;
                    NitVerificado = true;

                    MensajeVerificacionNIT = await TranslateExtension.TranslateAsync("NITNoExisteSerPropietario");
                    ColorMensajeNIT = Colors.Orange;

                    // Habilitar campos para que usuario escriba
                    CamposEmprendimientoHabilitados = true;
                    LogoHabilitadoEdicion = true;
                    MostrarCheckboxPropietario = true;

                    // Limpiar campos
                    NombreEmprendimiento = "";
                    NombrePropietario = "";
                    DeclaroPropietario = false;
                    _urlLogoExistente = null;
                }
                else if (response.es_propietario == true)
                {
                    // CASO 2: Emprendimiento existe y usuario ES propietario
                    _esNuevoEmprendimiento = false;
                    _esPropietario = true;
                    // IMPORTANTE: asignar _idEmprendimiento y NombreEmprendimiento ANTES de NitVerificado=true
                    // para que cuando PropertyChanged dispare, IdEmprendimiento y NombreEmprendimiento ya tengan valor
                    _idEmprendimiento = response.emprendimiento.id_emprendimiento;
                    NombreEmprendimiento = response.emprendimiento.nombre_emprendimiento;
                    NombrePropietario = response.emprendimiento.nombre_propietario;
                    _urlLogoExistente = response.emprendimiento.url_logo;
                    NitVerificado = true;

                    MensajeVerificacionNIT = await TranslateExtension.TranslateAsync("EresPropietario");
                    ColorMensajeNIT = Colors.Green;

                    CamposEmprendimientoHabilitados = true;
                    LogoHabilitadoEdicion = true;
                    MostrarCheckboxPropietario = false;
                }
                else
                {
                    // CASO 3: Emprendimiento existe y usuario NO es propietario (subalterno)
                    _esNuevoEmprendimiento = false;
                    _esPropietario = false;
                    // IMPORTANTE: asignar _idEmprendimiento y NombreEmprendimiento ANTES de NitVerificado=true
                    // para que cuando PropertyChanged dispare, IdEmprendimiento y NombreEmprendimiento ya tengan valor
                    _idEmprendimiento = response.emprendimiento.id_emprendimiento;
                    NombreEmprendimiento = response.emprendimiento.nombre_emprendimiento;
                    NombrePropietario = response.emprendimiento.nombre_propietario;
                    _urlLogoExistente = response.emprendimiento.url_logo;
                    NitVerificado = true;

                    MensajeVerificacionNIT = await TranslateExtension.TranslateAsync("EresSubalternoSoloPromocion");
                    ColorMensajeNIT = Colors.Red;

                    CamposEmprendimientoHabilitados = false;
                    LogoHabilitadoEdicion = false;
                    MostrarCheckboxPropietario = false;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "PromocionLocalViewModel", "VerificarNIT");

                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var errorMessage = $"Error al verificar NIT: {ex.Message}";
                await ModernAlerts.ShowError(labelError, errorMessage);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task LanzarPromocionAsync()
        {
            try
            {
                IsRunning = true;

                // VALIDACIÓN 1: Verificar que se haya consultado el NIT primero
                if (!_nitVerificado || string.IsNullOrWhiteSpace(NitCedulaPropietario))
                {
                    var labelError = await TranslateExtension.TranslateAsync("LabelError");
                    var debeVerificarNIT = await TranslateExtension.TranslateAsync("DebeVerificarNITPrimero");
                    await ModernAlerts.ShowError(labelError, debeVerificarNIT);
                    return;
                }

                // VALIDACIÓN 2: Si es nuevo emprendimiento, validar checkbox
                if (_esNuevoEmprendimiento && !DeclaroPropietario)
                {
                    var labelAdvertencia = await TranslateExtension.TranslateAsync("LabelAdvertencia");
                    var debeAceptar = await TranslateExtension.TranslateAsync("DebeAceptarDeclaracionPropietario");
                    await ModernAlerts.ShowWarning(labelAdvertencia, debeAceptar);
                    return;
                }

                // VALIDACIÓN 3: Si es nuevo, validar que llenó nombre emprendimiento y propietario
                if (_esNuevoEmprendimiento)
                {
                    if (string.IsNullOrWhiteSpace(NombreEmprendimiento) || string.IsNullOrWhiteSpace(NombrePropietario))
                    {
                        var labelError = await TranslateExtension.TranslateAsync("LabelError");
                        var nombresRequeridos = await TranslateExtension.TranslateAsync("NombreEmprendimientoYPropietarioRequeridos");
                        await ModernAlerts.ShowError(labelError, nombresRequeridos);
                        return;
                    }
                }

                // Validación: descripción requerida
                if (string.IsNullOrWhiteSpace(_descripcion))
                {
                    var labelError = await TranslateExtension.TranslateAsync("LabelError");
                    var promoDescripcionVacia = await TranslateExtension.TranslateAsync("PromoDescripcionVacia");
                    await ModernAlerts.ShowWarning(labelError, promoDescripcionVacia);
                    return;
                }

                // Validación: saldo suficiente
                if (SaldoResultante < 0)
                {
                    var promoPoderesInsuficientes = await TranslateExtension.TranslateAsync("PromoPoderesInsuficientes");
                    var promoInsufficientBalance = await TranslateExtension.TranslateAsync("PromoInsufficientBalance");
                    await ModernAlerts.ShowWarning(promoPoderesInsuficientes, promoInsufficientBalance);
                    return;
                }

                // Confirmación
                var labelConfirmar = await TranslateExtension.TranslateAsync("LabelConfirmar");
                var promoConfirmarLanzar = await TranslateExtension.TranslateAsync("PromoConfirmarLanzar");
                var promoWarningNoCancellation = await TranslateExtension.TranslateAsync("PromoWarningNoCancellation");
                var promoSiLanzar = await TranslateExtension.TranslateAsync("PromoSiLanzar");
                var labelCancelar = await TranslateExtension.TranslateAsync("LabelCancelar");

                var mensajeConfirmacion = string.Format(
                    promoConfirmarLanzar,
                    _costoActual,
                    _duracionDias,
                    _radioMetros,
                    promoWarningNoCancellation
                );

                var confirmar = await ModernAlerts.ShowConfirmation(
                    labelConfirmar,
                    mensajeConfirmacion,
                    promoSiLanzar,
                    labelCancelar,
                    useTranslations: false
                );

                if (!confirmar) return;

                // Procesar multimedia (convertir a Base64)
                List<FotoAlarmaDto> fotosDto = new List<FotoAlarmaDto>();
                if (_rutasMultimedia != null && _rutasMultimedia.Any())
                {
                    int orden = 1;
                    foreach (var ruta in _rutasMultimedia)
                    {
                        try
                        {
                            if (File.Exists(ruta))
                            {
                                byte[] bytes = File.ReadAllBytes(ruta);
                                string base64 = Convert.ToBase64String(bytes);

                                bool esVideo = ruta.ToLower().EndsWith(".mp4") || ruta.ToLower().EndsWith(".mov") || ruta.ToLower().EndsWith(".avi");

                                var fotoDto = new FotoAlarmaDto
                                {
                                    Base64Data = base64,
                                    NombreArchivoOriginal = Path.GetFileName(ruta),
                                    TipoMime = esVideo ? "video/mp4" : "image/jpeg",
                                    TamanoBytes = bytes.Length,
                                    EsVideo = esVideo,
                                    Orden = orden++
                                };

                                fotosDto.Add(fotoDto);
                                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Foto convertida: {fotoDto.NombreArchivoOriginal}, Tamaño: {bytes.Length} bytes");
                            }
                        }
                        catch (Exception exMedia)
                        {
                            CrashlyticsHelper.LogError(exMedia, "PromocionLocalViewModel", "LanzarPromocionAsync_ConvertirFotos");
                            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error convirtiendo archivo: {exMedia.Message}");
                        }
                    }
                }

                // Procesar logo personalizado: convertir a Base64 y enviar como "LogoBase64" en el request
                string logoBase64 = null;
                string urlLogo = null;

                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] === PROCESANDO LOGO ===");
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] _logoHabilitado: {_logoHabilitado}");
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] _usarLogoPersonalizado: {_usarLogoPersonalizado}");
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] _rutaLogo: {_rutaLogo ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] _urlLogoExistente: {_urlLogoExistente ?? "NULL"}");

                if (_logoHabilitado)
                {
                    if (_usarLogoPersonalizado && !string.IsNullOrEmpty(_rutaLogo))
                    {
                        // Subir logo personalizado: convertir a Base64 para que el API lo suba a S3
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Procesando logo PERSONALIZADO desde: {_rutaLogo}");
                        try
                        {
                            if (File.Exists(_rutaLogo))
                            {
                                byte[] logoBytes = File.ReadAllBytes(_rutaLogo);
                                logoBase64 = Convert.ToBase64String(logoBytes);
                                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Logo personalizado convertido a Base64: {logoBytes.Length} bytes");
                                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] LogoBase64 longitud: {logoBase64.Length} caracteres");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] ERROR: Archivo de logo NO EXISTE en: {_rutaLogo}");
                            }
                        }
                        catch (Exception exLogo)
                        {
                            CrashlyticsHelper.LogError(exLogo, "PromocionLocalViewModel", "LanzarPromocionAsync_ConvertirLogo");
                            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error convirtiendo logo: {exLogo.Message}");
                            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] StackTrace: {exLogo.StackTrace}");
                        }
                    }
                    else if (!string.IsNullOrEmpty(_urlLogoExistente))
                    {
                        // Usar logo de emprendimiento ya guardado en S3 (desde verificación NIT)
                        urlLogo = _urlLogoExistente;
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Usando logo EXISTENTE de S3: {urlLogo}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] ADVERTENCIA: Logo habilitado pero no hay ruta ni URL existente");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Logo NO habilitado, no se enviará logo");
                }

                // Crear request
                var request = new CrearAlarmaPromocionalRequest
                {
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_latitud = _latitud,
                    p_longitud = _longitud,
                    ip_usuario = "",
                    idioma_dispositivo = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName,

                    // Datos del emprendimiento
                    nit_cedula_propietario = NitCedulaPropietario,
                    nombre_emprendimiento = NombreEmprendimiento,
                    nombre_propietario = NombrePropietario,

                    descripcion_promocional = _descripcion,
                    radio_metros = _radioMetros,
                    duracion_dias = _duracionDias,
                    logo_habilitado = _logoHabilitado,
                    url_logo = urlLogo,
                    LogoBase64 = logoBase64,  // Nuevo campo para logo en Base64
                    contacto_habilitado = _contactoHabilitado,
                    domicilio_habilitado = _domicilioHabilitado,
                    usuarios_push = _usuariosPush,
                    texto_push = _textoPush,  // CORREGIDO 2026-01-29: Usar el texto personalizado de notificación push
                    proveedor_acepto_terminos_chat = _aceptoTerminosChat,
                    Fotos = fotosDto.Cast<object>().ToList()
                };

                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Request creado con {fotosDto.Count} fotos, url_logo: {urlLogo}, logoBase64: {(logoBase64 != null ? "SI" : "NO")}");

                var response = await ApiService.CrearAlarmaPromocional(request);

                if (response.IsSuccess)
                {
                    // Actualizar saldo en ParametrosUsuario localmente (rápido, para que el menú
                    // muestre el valor correcto inmediatamente antes de que termine la recarga del API)
                    var parametrosJson = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosJson))
                    {
                        try
                        {
                            var parametros = Newtonsoft.Json.JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosJson);
                            if (parametros != null)
                            {
                                parametros.SaldoPoderes = response.SaldoResultante;
                                Preferences.Set("ParametrosUsuario", Newtonsoft.Json.JsonConvert.SerializeObject(parametros));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error actualizando saldo: {ex.Message}");
                        }
                    }
                    // Forzar recarga completa desde el API en background
                    _ = HomeViewModel.RefrescarParametrosAsync();

                    var labelExito = await TranslateExtension.TranslateAsync("LabelExito");
                    var promoCreacionExitosa = await TranslateExtension.TranslateAsync("PromoCreacionExitosa");
                    await ModernAlerts.ShowSuccess(labelExito, promoCreacionExitosa);

                    // Cerrar la página usando el NavigationPage correcto
                    INavigation navigation = null;
                    if (Application.Current.MainPage is TabbedPage tabbedPage && tabbedPage.CurrentPage is NavigationPage navPage)
                    {
                        navigation = navPage.Navigation;
                    }
                    else if (Application.Current.MainPage is NavigationPage np)
                    {
                        navigation = np.Navigation;
                    }

                    if (navigation != null)
                    {
                        await navigation.PopAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: No se pudo obtener Navigation para cerrar PromocionLocalPage");
                    }
                }
                else
                {
                    var promoErrorCreacion = await TranslateExtension.TranslateAsync("PromoErrorCreacion");
                    await ModernAlerts.ShowError(promoErrorCreacion, response.Message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PromocionLocalViewModel", "LanzarPromocionAsync");

                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var promoErrorCreacion = await TranslateExtension.TranslateAsync("PromoErrorCreacion");
                await ModernAlerts.ShowError(labelError, $"{promoErrorCreacion}: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task CancelarAsync()
        {
            // Cerrar la página usando el NavigationPage correcto
            INavigation navigation = null;
            if (Application.Current.MainPage is TabbedPage tabbedPage && tabbedPage.CurrentPage is NavigationPage navPage)
            {
                navigation = navPage.Navigation;
            }
            else if (Application.Current.MainPage is NavigationPage np)
            {
                navigation = np.Navigation;
            }

            if (navigation != null)
            {
                await navigation.PopAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: No se pudo obtener Navigation para cerrar PromocionLocalPage");
            }
        }

        public void ActualizarUsuariosDisponibles()
        {
            _usuariosDisponibles = ObtenerUsuariosPorRadioInterpolado(_radioMetros);
            OnPropertyChanged(nameof(UsuariosDisponibles));
            OnPropertyChanged(nameof(LabelUsuariosDisponibles));
        }

        /// <summary>
        /// Actualiza las coordenadas de lanzamiento cuando el usuario mueve el pin en el minimapa.
        /// </summary>
        public void ActualizarUbicacion(double latitud, double longitud)
        {
            _latitud = latitud;
            _longitud = longitud;
            System.Diagnostics.Debug.WriteLine($"[PromocionLocalViewModel] Ubicación actualizada: {_latitud}, {_longitud}");
        }

        #endregion
    }
}
