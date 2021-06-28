using Microsoft.Win32;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using System.Threading.Tasks;

namespace OpenCommandBgService
{
    class Example1
    {
        static AutoResetEvent done = new AutoResetEvent(false);

        static void Test()
        {
            Thread appServiceThread = new Thread(new ThreadStart(InitConnection));
            appServiceThread.Start();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("*****************************");
            Console.WriteLine("**** Classic desktop app ****");
            Console.WriteLine("*****************************");
            Console.ReadLine();
        }

        //static void Main(string[] args)
        //{
        //    Thread bgThread = new Thread(ThreadProc);
        //    bgThread.Start(done);
        //    done.WaitOne();

        //    InitConnection();
        //}

        static void ThreadProc(object unused)
        {
            string key = "HKCR\\Directory\\Background\\shell\\cmd";
            //var regs = GetRegedit(key);
            InitConnection();
            // keep this up ofr 10sec, just for demo purposes
            Thread.Sleep(10000);
            done.Set();
        }

        ///// <summary>
        ///// Open connection to UWP app service
        ///// </summary>
        //static Dictionary<string, string> GetRegedit(string key)
        //{
        //    // HKCR\Directory\Background\shell\cmd
        //    Dictionary<string, string> response = new Dictionary<string, string>();

        //    int index = key.IndexOf('\\');
        //    if (index > 0)
        //    {
        //        // read the key values from the respective hive in the registry
        //        string hiveName = key.Substring(0, key.IndexOf('\\'));
        //        string keyName = key.Substring(key.IndexOf('\\') + 1);
        //        RegistryHive hive = RegistryHive.ClassesRoot;

        //        switch (hiveName)
        //        {
        //            case "HKLM":
        //                hive = RegistryHive.LocalMachine;
        //                break;
        //            case "HKCU":
        //                hive = RegistryHive.CurrentUser;
        //                break;
        //            case "HKCR":
        //                hive = RegistryHive.ClassesRoot;
        //                break;
        //            case "HKU":
        //                hive = RegistryHive.Users;
        //                break;
        //            case "HKCC":
        //                hive = RegistryHive.CurrentConfig;
        //                break;
        //        }

        //        using (RegistryKey regKey = RegistryKey.OpenRemoteBaseKey(hive, "").OpenSubKey(keyName))
        //        {
        //            // compose the response as ValueSet

        //            if (regKey != null)
        //            {
        //                foreach (string valueName in regKey.GetValueNames())
        //                {
        //                    response.Add(valueName, regKey.GetValue(valueName).ToString());
        //                }
        //            }
        //            else
        //            {
        //                response.Add("ERROR", "KEY NOT FOUND");
        //            }
        //            // send the response back to the UWP
        //        }
        //    }

        //    return response;
        //}

        ///// <summary>
        ///// Open connection to UWP app service
        ///// </summary>
        //static Dictionary<string, string> SetRegedit(string key, string value)
        //{
        //    // HKCR\Directory\Background\shell\cmd
        //    Dictionary<string, string> response = new Dictionary<string, string>();

        //    int index = key.IndexOf('\\');
        //    if (index > 0)
        //    {
        //        // read the key values from the respective hive in the registry
        //        string hiveName = key.Substring(0, key.IndexOf('\\'));
        //        string keyName = key.Substring(key.IndexOf('\\') + 1);
        //        RegistryHive hive = RegistryHive.ClassesRoot;

        //        switch (hiveName)
        //        {
        //            case "HKLM":
        //                hive = RegistryHive.LocalMachine;
        //                break;
        //            case "HKCU":
        //                hive = RegistryHive.CurrentUser;
        //                break;
        //            case "HKCR":
        //                hive = RegistryHive.ClassesRoot;
        //                break;
        //            case "HKU":
        //                hive = RegistryHive.Users;
        //                break;
        //            case "HKCC":
        //                hive = RegistryHive.CurrentConfig;
        //                break;
        //        }

        //        using (RegistryKey regKey = RegistryKey.OpenRemoteBaseKey(hive, "").OpenSubKey(keyName))
        //        {
        //            // compose the response as ValueSet

        //            if (regKey != null)
        //            {
        //                foreach (string valueName in regKey.GetValueNames())
        //                {
        //                    response.Add(valueName, regKey.GetValue(valueName).ToString());
        //                }
        //            }
        //            else
        //            {
        //                response.Add("ERROR", "KEY NOT FOUND");
        //            }
        //            // send the response back to the UWP
        //        }
        //    }

        //    return response;
        //}

        static async void InitConnection()
        {
            var connection = new AppServiceConnection();
            connection.AppServiceName = "CommunicationService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;

            //connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await connection.OpenAsync();
            switch (status)
            {
                case AppServiceConnectionStatus.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Connection established - waiting for requests");
                    Console.WriteLine();
                    break;
                case AppServiceConnectionStatus.AppNotInstalled:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The app AppServicesProvider is not installed.");
                    return;
                case AppServiceConnectionStatus.AppUnavailable:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The app AppServicesProvider is not available.");
                    return;
                case AppServiceConnectionStatus.AppServiceUnavailable:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("The app AppServicesProvider is installed but it does not provide the app service {0}.", connection.AppServiceName));
                    return;
                case AppServiceConnectionStatus.Unknown:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("An unkown error occurred while we were trying to open an AppServiceConnection."));
                    return;
            }
        }

        /// <summary>
        /// Handles the event when the desktop process receives a request from the UWP app
        /// </summary>
        static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            string key = args.Request.Message.First().Key;
            string value = args.Request.Message.First().Value.ToString();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("Received message '{0}' with value '{1}'", key, value));
            if (key == "request")
            {
                ValueSet valueSet = new ValueSet();
                valueSet.Add("response", "RspSuccess");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(string.Format("Sending response: '{0}'", value.ToUpper()));
                Console.WriteLine();
                args.Request.SendResponseAsync(valueSet).Completed += delegate { };
            }

        }

        ///// <summary>
        ///// Handles the event when the app service connection is closed
        ///// </summary>
        //static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        //{
        //    // connection to the UWP lost, so we shut down the desktop process
        //    //Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        //    //{
        //    //    Application.Current.Shutdown();
        //    //}));
        //}
    }
}
