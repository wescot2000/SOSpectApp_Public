using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.Forms;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using Plugin.InAppBilling;
using System.Linq;
using sospect.Helpers;


namespace sospect.ViewModels
{
    public class PurchaseSuperPowersViewModel : BaseViewModel
    {
        public ObservableCollection<SuperPower> SuperPowers { get; set; }
        public ICommand PurchaseCommand { get; set; }

        public ICommand ConsumirCommand { get; set; }


        public bool IsPurchaseProcessing { get; set; }

        public PurchaseSuperPowersViewModel()
        {
            PurchaseCommand = new Command<SuperPower>(async (superPower) =>
            {
                if (IsPurchaseProcessing)
                {
                    return; // Retorna si una compra ya está en curso
                }

                IsPurchaseProcessing = true;
                await PurchaseSuperPowerAsync(superPower);
                IsPurchaseProcessing = false;
            });

            ConsumirCommand = new Command(async () =>
            {
                if (IsPurchaseProcessing)
                {
                    return; // Retorna si una compra ya está en curso
                }

                IsPurchaseProcessing = true;
                await ConsumirPowerAsync();
                IsPurchaseProcessing = false;
            });

            SuperPowers = new ObservableCollection<SuperPower>();
            _ = Task.Run(async () => await GetSuperPowersAndRequestDetailsAsync());
        }

        public async Task ConsumirPowerAsync()
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            await CrossInAppBilling.Current.ConsumePurchaseAsync("11", "trewtrewtrewt");
            await CrossInAppBilling.Current.ConsumePurchaseAsync("11", "trewtrewtrewtrewtrewt");
            await CrossInAppBilling.Current.DisconnectAsync();
        }
            public async Task GetSuperPowersAndRequestDetailsAsync()
        {
            IsRunning = true;
            var superPowersResponse = await ApiService.ObtenerValoresPoderes();
            IsRunning = false;

            if (superPowersResponse != null)
            {
                List<string> identifiers = new List<string>();
                foreach (var superPower in superPowersResponse)
                {
                    identifiers.Add(superPower.ProductId);
                }

                var connected = await CrossInAppBilling.Current.ConnectAsync();
                if (!connected)
                {
                    //No se pudo conectar al servicio de facturación
                    //Puede ser que no esté disponible, o que el usuario esté bloqueado temporalmente, etc.
                    return;
                }
                //string[] productids = new string[] { "11","12","13","14","15","16","17"};
                var products = await CrossInAppBilling.Current.GetProductInfoAsync(ItemType.InAppPurchaseConsumable, identifiers.ToArray());

                foreach (var product in products)
                {
                    // Aquí, buscarías el poder correspondiente en la respuesta de la base de datos usando product.ProductId como identificador,
                    // luego crearías un nuevo objeto SuperPower usando los datos de la base de datos y el precio localizado de la tienda.
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
                    }
                }

                // Recuerda siempre desconectarte del servicio de facturación después de utilizarlo
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }


        public async Task<InAppBillingPurchase> BuyProductByProductId(string productId)
        {
            var billingResult = await CrossInAppBilling.Current.ConnectAsync();
            if (!billingResult)
            {
                // handle connection error
                return null;
            }
          
            var product = SuperPowers.FirstOrDefault(p => p.ProductId == productId);

            if (product == null)
            {
                // product not found
                return null;
            }

            var purchase = await CrossInAppBilling.Current.PurchaseAsync(product.ProductId, ItemType.InAppPurchase, "payload");
            if (purchase == null)
            {
                // handle purchase error
                return null;
            }

            // purchase was successful, now return purchase token
            await CrossInAppBilling.Current.DisconnectAsync();
            return purchase;
        }


        private async Task PurchaseSuperPowerAsync(SuperPower superPower)
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelExito = TranslateExtension.Translate("LabelExito");
            var LblCompraExitosa = TranslateExtension.Translate("LblCompraExitosa");
            var LblCompraFallida = TranslateExtension.Translate("LblCompraFallida");
            try
            {
                string userId = App.persona.user_id_thirdparty; // Reemplazar con el ID de usuario real
                string ipTransaccion = InternetUtil.GetPublicIpAddress(); // Reemplazar con la IP de la transacción real

                // Primero intentamos comprar el producto
                InAppBillingPurchase purchase = await BuyProductByProductId(superPower.ProductId);
                // Usamos el identificador del producto para hacer la compra

                // Asumiendo que si la compra fue exitosa, se habrá guardado el token de compra en un campo
                if (purchase != null)
                {
                    // Solo si la compra fue exitosa, hacemos una llamada a la API para registrar la transacción
                    CompraSuperPoderRequest compraRequest = new CompraSuperPoderRequest
                    {
                        p_user_id_thirdparty = userId,
                        cantidad = superPower.cantidad_poderes,
                        valor = superPower.valor_usd,
                        ip_transaccion = ipTransaccion,
                        p_tipo_transaccion = "Compra",
                        p_purchase_token = purchase.PurchaseToken
                    };

                    IsRunning = true;
                    bool success = await ApiService.ComprarSuperPoder(compraRequest);
                    IsRunning = false;

                    if (success)
                    {
                        var billingResult = await CrossInAppBilling.Current.ConnectAsync();
                        if (billingResult)
                        {
                            await CrossInAppBilling.Current.ConsumePurchaseAsync(purchase.ProductId,purchase.PurchaseToken);
                            await CrossInAppBilling.Current.DisconnectAsync();
                        }

                        await App.Current.MainPage.DisplayAlert(LabelExito, LblCompraExitosa, LabelOK);

                    }
                    else
                    {

                        await App.Current.MainPage.DisplayAlert(LabelError, LblCompraFallida, LabelOK);
                        //CompraSuperPoderRequest compraRequestFallida = new CompraSuperPoderRequest
                        //{
                        //    p_user_id_thirdparty = userId,
                        //    cantidad = superPower.cantidad_poderes,
                        //    valor = superPower.valor_usd,
                        //    ip_transaccion = ipTransaccion,
                        //    p_tipo_transaccion = "FalloCompra",
                        //    p_purchase_token = null
                        //};

                        //IsRunning = true;
                        //    await ApiService.RegistrarFalloCompra(compraRequest);
                        //IsRunning = false;
                    }
                }

            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelError, ex.Message, LabelOK);
            }
            
        }
    }
}
