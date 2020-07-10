using SumUp.Sdk;
using SumUp.Sdk.Api.Emv;
using SumUp.Sdk.Api.TransactionGateway.Dto;
using SumUp.Sdk.Device.Reader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace playgroundv6
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.Unicode;

                var sumUpService = new SumUpService();
                Task.Run(() => sumUpService.Authentication.AuthenticateAsync(
                            new SumUp.Sdk.Api.Authentication.OAuth.OAuthCredentials("ZBMsY3JDxMdK3GFIp3NBSw87LsSu",
                                "cf768b340a13846d51bbd98fb54bcc163dca7d030a77d0fe383337e096209689", "dev_statix@sumup.com",
                                "extdev"))).Wait();

                var usbReaders = Task.Run(() => sumUpService.CardReader.FindAllUsbAsync());
                usbReaders.Wait();
                var usbDevicesList = usbReaders.Result;

                //var findBlueToothReaders = Task.Run(() =>
                //       sumUpService.CardReader.FindAllBluetoothGattAsync(CardReaderType.Air, 5, 10));
                //findBlueToothReaders.Wait();
                //var readers = findBlueToothReaders.Result;
                //if (readers.Count == 0)
                //{
                //    Console.WriteLine("Няма терминал", "Не е намерен POS терминал свързан с този компютър");
                //    //Dispatcher?.Invoke(async () =>
                //    //{
                //    //    await c.CloseAsync();
                //    //    await MainWindow.Instance.ShowMessageAsync("Няма терминал",
                //    //        "Не е намерен POS терминал свързан с този компютър");
                //    //});
                //    //return false;
                //}


                sumUpService.CardPayment.AssignCallbackFunctions(
                    (s, paymentFinishing) =>
                    {
                        if (paymentFinishing.Result.Message == "successful_receipt")
                        {
                        //Set OrderPayments values
                        //payment.Complete = true;
                        //payment.AccountingConfirmed = true;
                        //payment.Save();
                        //Dispatcher?.Invoke(() => SavePayment(sender, e));
                    }
                        else
                        {
                            Console.WriteLine();
                        }
                    },
                            (s, readerNotification) => { },
                            (s, statusChanged) =>
                            {
                            //Dispatcher?.Invoke(() =>
                            //{
                            string message = String.Empty;
                                Console.WriteLine(statusChanged.StatusMessage);
                                switch (statusChanged.StatusMessage)
                                {
                                    case "InProgress":
                                        message = "Изпращане към терминала";
                                        break;
                                    case "air_present_card":
                                        message = "Моля доближете или поставете карта!";
                                        break;
                                    case "payment_air_payment_processing_nfc":
                                        message = "Обработване на безконтактно плащане";
                                        break;
                                    case "successful_receipt":
                                        message = "Успешно плащане";
                                        break;
                                    case "generic_error":
                                        message = "Грешка при изпълнение";
                                        break;
                                    default:
                                        message = "Непозната команда от терминала";
                                        break;
                                }

                            //c.SetMessage(message);
                            Console.WriteLine(message);
                            /*}*/
                            });

                //if (usbReaders.Result.Count > 0)
                //{
                var paymentsTask = Task.Run(() => sumUpService.CardPayment.MakePaymentUsingDefaultUsbReaderAsync(
                    new PaymentSignatureCallback(), CancellationToken.None, new CheckoutRequest()
                    {
                        Amount = 1,
                        Currency = "BGN",
                        StartTime = DateTimeOffset.Now,
                        TransactionId = Guid.NewGuid().ToString(),
                        VatAmount = 0.20,
                        CustomItems = new List<CustomItem>()
                        {
                                        new CustomItem()
                                        {
                                            Amount = 2,
                                            Quantity = 1,
                                            Title = "TestPayment"
                                        }
                        }
                    }));

                var z = paymentsTask.Result;
                //}

            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine(DateTime.Now.ToString(CultureInfo.CurrentCulture));
                sb.AppendLine();
                sb.AppendLine(ex.Message);
                sb.AppendLine();
                sb.AppendLine(ex.StackTrace);
                File.WriteAllText("sumup.log", sb.ToString());
                Console.WriteLine("Грешка", "Грешка при комуникация с POS устройството !");
                //Dispatcher?.Invoke(async () =>
                //{
                //    await c.CloseAsync();
                //    await MainWindow.Instance.ShowMessageAsync("Грешка",
                //        "Грешка при комуникация с POS устройството !");
                //});
                //return false;
                Console.Read();
            }
        }
    }

    internal class PaymentSignatureCallback : IEmvPaymentSignatureCallback
    {
        public Task<TransactionSignature> GetSignature()
        {
            return Task.FromResult(new TransactionSignature
            {
                Gesture = new List<TransactionSignaturePoint>()
                {
                    new TransactionSignaturePoint()
                    {
                        TimeDelta = 0,
                        X = 0,
                        Y = 0
                    },
                    new TransactionSignaturePoint()
                    {
                        TimeDelta = (uint) TimeSpan.FromSeconds(1).TotalMilliseconds,
                        X = 100,
                        Y = 0
                    },
                    new TransactionSignaturePoint()
                    {
                        TimeDelta = (uint) TimeSpan.FromSeconds(2).TotalMilliseconds,
                        X = 0,
                        Y = 100
                    },
                    new TransactionSignaturePoint()
                    {
                        TimeDelta = (uint) TimeSpan.FromSeconds(3).TotalMilliseconds,
                        X = 100,
                        Y = 100
                    },
                    new TransactionSignaturePoint()
                    {
                        TimeDelta = (uint) TimeSpan.FromSeconds(4).TotalMilliseconds,
                        X = 0,
                        Y = 0
                    },
                },
                StrokeWidth = 1,
                Time = (uint)TimeSpan.FromSeconds(4).TotalMilliseconds,
                Tries = 1,
                Version = 1
            });
        }
    }
}

