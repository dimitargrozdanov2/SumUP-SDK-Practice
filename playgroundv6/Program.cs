using SumUp.Sdk;
using SumUp.Sdk.Api.Emv;
using SumUp.Sdk.Api.TransactionGateway.Dto;
using SumUp.Sdk.Device.Facade.Dto;
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
        static string message = String.Empty;

        static void Main(string[] args)
        {
            PayPos();
        }

        public static CardPaymentResult PayPos()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.Unicode;

                var sumUpService = new SumUpService();
                Task.Run(() => sumUpService.Authentication.AuthenticateAsync(
                            new SumUp.Sdk.Api.Authentication.OAuth.OAuthCredentials("ZBMsY3JDxMdK3GFIp3NBSw87LsSu",
                                "cf768b340a13846d51bbd98fb54bcc163dca7d030a77d0fe383337e096209689", "dev_statix@sumup.com",
                                "extdev"))).Wait();

                //First bluetooth finding because of exception

                var findBlueToothReaders = Task.Run(() =>
                       sumUpService.CardReader.FindAllBluetoothGattAsync(CardReaderType.AirLite, 5, 10));
                findBlueToothReaders.Wait();
                var blueToothReaders = findBlueToothReaders.Result;

                //SKIP step below because if no bluetooth is found , find USB alternative. If no USB is found , an exception will be thrown.
                //if (blueToothReaders.Count == 0)
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
                            //this is executed on the web app
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


                if (blueToothReaders.Count > 0)
                {
                    var bluetoothPayment = Task.Run(() => sumUpService.CardPayment.MakePaymentUsingBluetoothReaderAsync(
                             new CardReaderInformation()
                             {
                                 CardReaderType = CardReaderType.Air,
                                 Id = blueToothReaders[0].Id,
                                 Name = blueToothReaders[0].Name
                             }, new PaymentSignatureCallback(), CancellationToken.None, new CheckoutRequest()
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
                                        Amount = 1,
                                        Quantity = 1,
                                        Title = "Bluetooth Payment"
                                    }
                                 }
                             }));

                    bluetoothPayment.Wait();

                    var bluetoothPaymentInfo = bluetoothPayment.Result;

                    var result = new CardPaymentResult { TransacationCompleted = bluetoothPaymentInfo.IsStop, PaymentStatus = bluetoothPaymentInfo.ProcessingMessage, TransactionCode = bluetoothPaymentInfo.TransactionCode};

                    return result;
                }
                else
                {
                    //NEED to use USB readers first as it the most common case.
                    var usbReaders = Task.Run(() => sumUpService.CardReader.FindAllUsbAsync());
                    usbReaders.Wait();
                    var usbDevicesList = usbReaders.Result;

                    if (usbReaders.Result.Count > 0)
                    {
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

                        paymentsTask.Wait();

                        var usbPaymentInfo = paymentsTask.Result;

                        var result = new CardPaymentResult { TransacationCompleted = usbPaymentInfo.IsStop, PaymentStatus = usbPaymentInfo.ProcessingMessage, TransactionCode = usbPaymentInfo.TransactionCode };

                        return result;
                    }
                    else
                    {
                        Console.WriteLine("Няма терминал", "Не е намерен POS терминал свързан с този компютър");
                        return new CardPaymentResult { TransacationCompleted = false, PaymentStatus = "Не е намерен POS терминал свързан с този компютър", TransactionCode = null }; 
                    }
                }

            }
            catch (Exception ex)
            {
                var result = new CardPaymentResult { TransacationCompleted = false, PaymentStatus = "Грешка при комуникация с POS устройството !", TransactionCode = null };

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

                return result;
            }

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


