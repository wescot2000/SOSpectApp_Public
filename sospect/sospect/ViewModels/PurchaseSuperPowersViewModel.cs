using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using Plugin.InAppBilling;
using System.Linq;
using sospect.Helpers;
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class PurchaseSuperPowersViewModel : BaseViewModel
    {
        public ObservableCollection<SuperPower> SuperPowers { get; set; }
        public ICommand PurchaseCommand { get; set; }
        public bool IsPurchaseProcessing { get; set; }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        public PurchaseSuperPowersViewModel()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            PurchaseCommand = new Command<SuperPower>(async (superPower) =>
            {
                if (IsPurchaseProcessing)
                {
                    return;
                }

                IsPurchaseProcessing = true;
                try
                {
                    await PurchaseSuperPowerAsync(superPower);
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "PurchaseCommand");
                }
                finally
                {
                    IsPurchaseProcessing = false;
                }
            }, (superPower) => !IsPurchaseProcessing);

            SuperPowers = new ObservableCollection<SuperPower>();
            _ = Task.Run(async () => await GetSuperPowersAndRequestDetailsAsync());
        }

        public async Task GetSuperPowersAndRequestDetailsAsync()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

            try
            {
                System.Diagnostics.Debug.WriteLine("=== INICIANDO CARGA DE PRODUCTOS ===");

                var superPowersResponse = await ApiService.ObtenerValoresPoderes();

                if (superPowersResponse != null && superPowersResponse.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Productos obtenidos de la BD: {superPowersResponse.Count}");

                    foreach (var power in superPowersResponse)
                    {
                        System.Diagnostics.Debug.WriteLine($"  BD ProductId: '{power.ProductId}', Cantidad: {power.cantidad_poderes}");
                    }

                    List<string> identifiers = superPowersResponse.Select(sp => sp.ProductId).ToList();
                    var billing = CrossInAppBilling.Current;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Conectando al servicio de facturacion...");
                        var connected = await billing.ConnectAsync();
                        System.Diagnostics.Debug.WriteLine($"Estado de conexion: {connected}");

                        if (!connected)
                        {
                            System.Diagnostics.Debug.WriteLine("No conectado - Cargando productos mock");
                            await LoadMockProducts(superPowersResponse);
                            return;
                        }

                        await CheckAndConsumePendingPurchases();

                        System.Diagnostics.Debug.WriteLine($"Solicitando informacion de {identifiers.Count} productos a la tienda...");
                        System.Diagnostics.Debug.WriteLine("IDs solicitados: " + string.Join(", ", identifiers.Select(id => $"'{id}'")));

                        // USAR ItemType.InAppPurchase (sin Consumable) como en Xamarin
                        var products = await billing.GetProductInfoAsync(ItemType.InAppPurchase, identifiers.ToArray());

                        if (products != null && products.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"Se obtuvieron {products.Count()} productos de la tienda");

                            foreach (var product in products)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Tienda ProductId: '{product.ProductId}', Precio: {product.LocalizedPrice}");
                            }

                            var sortedProducts = products.OrderBy(p =>
                            {
                                if (int.TryParse(p.ProductId, out int productIdNum))
                                {
                                    return productIdNum;
                                }
                                return int.MaxValue;
                            }).ToList();

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                SuperPowers.Clear();
                                foreach (var product in sortedProducts)
                                {
                                    var correspondingPower = superPowersResponse.FirstOrDefault(p => p.ProductId == product.ProductId);
                                    if (correspondingPower != null)
                                    {
                                        SuperPowers.Add(new SuperPower
                                        {
                                            ProductId = correspondingPower.ProductId,
                                            cantidad_poderes = correspondingPower.cantidad_poderes,
                                            valor_usd = correspondingPower.valor_usd,
                                            LocalizedPrice = product.LocalizedPrice
                                        });
                                        System.Diagnostics.Debug.WriteLine($"  Agregado a UI: {correspondingPower.cantidad_poderes} poderes - {product.LocalizedPrice}");
                                    }
                                }
                                System.Diagnostics.Debug.WriteLine($"Total productos en UI: {SuperPowers.Count}");
                            });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No se obtuvieron productos - Cargando mock");
                            await LoadMockProducts(superPowersResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error: {ex.GetType().Name} - {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "GetSuperPowersAndRequestDetailsAsync");
                        await LoadMockProducts(superPowersResponse);
                    }
                    finally
                    {
                        await billing.DisconnectAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error general: {ex.Message}");
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "GetSuperPowersAndRequestDetailsAsync");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
            }
        }

        private async Task CheckAndConsumePendingPurchases()
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

                if (purchases != null && purchases.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Compras pendientes: {purchases.Count()}");
                    foreach (var purchase in purchases)
                    {
                        if (purchase.State == PurchaseState.Purchased)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Consumiendo: {purchase.ProductId}");
                            await billing.ConsumePurchaseAsync(purchase.ProductId, purchase.PurchaseToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando compras: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "CheckAndConsumePendingPurchases");
            }
        }

        private async Task LoadMockProducts(List<SuperPower> superPowersResponse)
        {
            var sortedPowers = superPowersResponse.OrderBy(p => p.cantidad_poderes).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SuperPowers.Clear();
                foreach (var power in sortedPowers)
                {
                    SuperPowers.Add(new SuperPower
                    {
                        ProductId = power.ProductId,
                        cantidad_poderes = power.cantidad_poderes,
                        valor_usd = power.valor_usd,
                        LocalizedPrice = $"${power.valor_usd:F2} USD"
                    });
                }
            });

            await Task.CompletedTask;
        }

        private async Task PurchaseSuperPowerAsync(SuperPower superPower)
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LblCompraExitosa = await TranslateExtension.TranslateAsync("LblCompraExitosa");
            var LblCompraFallida = await TranslateExtension.TranslateAsync("LblCompraFallida");

            MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

            try
            {
                string userId = App.persona.user_id_thirdparty;
                string ipTransaccion = InternetUtil.GetPublicIpAddress();

                System.Diagnostics.Debug.WriteLine("=== INICIANDO COMPRA ===");
                System.Diagnostics.Debug.WriteLine($"ProductId: '{superPower.ProductId}'");
                System.Diagnostics.Debug.WriteLine($"Usuario: {userId}");

                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();

                if (!connected)
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo conectar");
                    await ModernAlerts.ShowError(LabelError, "No se pudo conectar al servicio de facturacion");
                    return;
                }

                try
                {
                    var existingPurchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                    var pendingPurchase = existingPurchases?.FirstOrDefault(p => p.ProductId == superPower.ProductId && p.State == PurchaseState.Purchased);

                    if (pendingPurchase != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Consumiendo compra pendiente...");
                        await billing.ConsumePurchaseAsync(pendingPurchase.ProductId, pendingPurchase.PurchaseToken);
                    }

                    System.Diagnostics.Debug.WriteLine("Iniciando PurchaseAsync...");

                    // CRÍTICO: Usar ItemType.InAppPurchase con payload (como Xamarin)
                    var purchase = await billing.PurchaseAsync(superPower.ProductId, ItemType.InAppPurchase, "payload");

                    if (purchase == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Compra retorno null");
                        await ModernAlerts.ShowWarning(LabelError, "Compra cancelada");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"Estado: {purchase.State}");

                    if (purchase.State == PurchaseState.Purchased)
                    {
                        System.Diagnostics.Debug.WriteLine("Registrando en backend...");

                        CompraSuperPoderRequest compraRequest = new CompraSuperPoderRequest
                        {
                            p_user_id_thirdparty = userId,
                            cantidad = superPower.cantidad_poderes,
                            valor = superPower.valor_usd,
                            ip_transaccion = ipTransaccion,
                            p_tipo_transaccion = "Compra",
                            p_purchase_token = purchase.PurchaseToken
                        };

                        bool success = await ApiService.ComprarSuperPoder(compraRequest);

                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine("Consumiendo compra...");
                            var consumeResult = await billing.ConsumePurchaseAsync(purchase.ProductId, purchase.PurchaseToken);

                            if (consumeResult != null)
                            {
                                System.Diagnostics.Debug.WriteLine("Compra exitosa");
                                await ModernAlerts.ShowSuccess(LabelExito, LblCompraExitosa);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Error consumiendo");
                                await ModernAlerts.ShowWarning(LabelError, "Compra registrada pero no consumida. Contacte soporte.");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Error en backend");
                            await ModernAlerts.ShowError(LabelError, LblCompraFallida);
                        }
                    }
                    else if (purchase.State == PurchaseState.Canceled)
                    {
                        await ModernAlerts.ShowWarning(LabelError, "Compra cancelada");
                    }
                    else if (purchase.State == PurchaseState.PaymentPending)
                    {
                        await ModernAlerts.ShowWarning(LabelError, "Pago pendiente");
                    }
                }
                catch (InAppBillingPurchaseException purchaseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"InAppBillingPurchaseException: {purchaseEx.PurchaseError}");
                    System.Diagnostics.Debug.WriteLine($"   Mensaje: {purchaseEx.Message}");

                    CrashlyticsHelper.LogError(purchaseEx, "PurchaseSuperPowersViewModel", "PurchaseSuperPowerAsync");

                    string message = purchaseEx.PurchaseError switch
                    {
                        PurchaseError.BillingUnavailable => "Servicio no disponible",
                        PurchaseError.DeveloperError => "Error de configuracion. Verifica Google Play Console",
                        PurchaseError.ItemUnavailable => "Producto no disponible",
                        PurchaseError.UserCancelled => "Compra cancelada",
                        PurchaseError.AlreadyOwned => "Compra pendiente detectada",
                        PurchaseError.InvalidProduct => "Producto invalido",
                        _ => $"Error: {purchaseEx.Message}"
                    };

                    await ModernAlerts.ShowError(LabelError, message);

                    if (purchaseEx.PurchaseError == PurchaseError.AlreadyOwned)
                    {
                        await TryConsumePendingPurchases(superPower.ProductId);
                    }
                }
                finally
                {
                    await billing.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name} - {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "PurchaseSuperPowerAsync");
                await ModernAlerts.ShowError(LabelError, "Error inesperado");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
            }
        }

        private async Task TryConsumePendingPurchases(string productId)
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();

                if (connected)
                {
                    try
                    {
                        var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                        var pendingPurchase = purchases?.FirstOrDefault(p => p.ProductId == productId);

                        if (pendingPurchase != null)
                        {
                            await billing.ConsumePurchaseAsync(pendingPurchase.ProductId, pendingPurchase.PurchaseToken);
                            await ModernAlerts.ShowSuccess("Exito", "Compra pendiente procesada");
                        }
                    }
                    finally
                    {
                        await billing.DisconnectAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PurchaseSuperPowersViewModel", "TryConsumePendingPurchases");
            }
        }
    }
}