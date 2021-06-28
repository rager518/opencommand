using OpenCommand.LocalServiceReference;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace OpenCommand
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Setting : Page, INotifyPropertyChanged
    {
        public static Setting Current;

        private LocalServiceClient client = new LocalServiceClient();
        private bool isSMode = Windows.System.Profile.WindowsIntegrityPolicy.IsEnabled;
        private ObservableCollection<string> startupProgramNames = new ObservableCollection<string>();

        public Setting()
        {
            Current = this;
            this.InitializeComponent();
        }

        private bool isCmdOpen = false;

        public bool IsCmdOpen
        {
            get { return isCmdOpen; }
            set
            {
                if (isCmdOpen != value)
                {
                    isCmdOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSMode
        {
            get { return isSMode; }
            set { isSMode = value; }
        }

        public bool NotSMode
        {
            get { return !isSMode; }
        }

        public string OSBitness
        {
            get
            {
                if (System.Environment.Is64BitProcess)
                {
                    return "x64";
                }
                else
                {
                    return "x86  Registry changes virtualized and do not persist outside process";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Called by App.xaml.cs OnBackgroundActivated through static Current ref
        /// </summary>
        public void RegisterConnection()
        {
            if (App.Connection != null)
            {
                App.Connection.RequestReceived += Connection_RequestReceived;

                GetStartupProgramNames();
            }
        }

        /// <summary>
        /// Get the Startup program names from the RegistryReadAppService and put them in ObservableCollection
        /// bound to the ListBox in the UI.
        /// </summary>
        private async void GetStartupProgramNames()
        {
            ValueSet valueSet = new ValueSet();

            valueSet.Clear();
            valueSet.Add("verb", "getStartupProgramNames");

            try
            {
                AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);

                if (response.Status == AppServiceResponseStatus.Success)
                {
                    if (response.Message["verb"] as string == "RegistryReadResult")
                    {
                        // Update UI-bound collections and controls on the UI thread
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            string[] newNames = (string[])response.Message["StartupProgramNames"];

                            if (!(newNames.Contains("Extended") || newNames.Contains("HideBasedOnVelocityId")))
                            {
                                IsCmdOpen = true;
                            }
                        });
                    }
                }

                OpenCmdSwitch.Toggled += ToggleSwitch_Toggled;
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// This isn't called in this demo, but is the pattern if needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = new ValueSet();
            returnData.Add("response", "success");

            // get the verb or "command" for this request
            string verb = message["verb"] as String;

            switch (verb)
            {

            }

            try
            {
                // Return the data to the caller.
                await args.Request.SendResponseAsync(returnData);
            }
            catch (Exception e)
            {
                // Your exception handling code here.
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                deferral.Complete();
            }
        }

        /// <summary>
        /// When app is loaded, kick off the desktop process
        /// and listen to app service connection events
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LoadBgService();
        }

        public async void LoadBgService()
        {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
        }

        /// <summary>
        /// When the desktop process is connected, get ready to send/receive requests
        /// </summary>
        private async void MainPage_AppServiceConnected(object sender, AppServiceTriggerDetails e)
        {
            App.Connection.RequestReceived += AppServiceConnection_RequestReceived;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // enable UI to access  the connection
                // btnRegKey.IsEnabled = true;
            });
        }

        /// <summary>
        /// When the desktop process is disconnected, reconnect if needed
        /// </summary>
        private async void MainPage_AppServiceDisconnected(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // disable UI to access the connection
                // btnRegKey.IsEnabled = false;

                // ask user if they want to reconnect
                Reconnect();
            });
        }

        /// <summary>
        /// Ask user if they want to reconnect to the desktop process
        /// </summary>
        private async void Reconnect()
        {
            //if (App.IsForeground)
            //{
            //    MessageDialog dlg = new MessageDialog("Connection to desktop process lost. Reconnect?");
            //    UICommand yesCommand = new UICommand("Yes", async (r) =>
            //    {
            //        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            //    });
            //    dlg.Commands.Add(yesCommand);
            //    UICommand noCommand = new UICommand("No", (r) => { });
            //    dlg.Commands.Add(noCommand);
            //    await dlg.ShowAsync();
            //}
        }

        /// <summary>
        /// Handle calculation request from desktop process
        /// (dummy scenario to show that connection is bi-directional)
        /// </summary>
        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            double d1 = (double)args.Request.Message["D1"];
            double d2 = (double)args.Request.Message["D2"];
            double result = d1 + d2;

            ValueSet response = new ValueSet();
            response.Add("RESULT", result);
            await args.Request.SendResponseAsync(response);

            // log the request in the UI for demo purposes
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //tbRequests.Text += string.Format("Request: {0} + {1} --> Response = {2}\r\n", d1, d2, result);
            });
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                //if (toggleSwitch.IsOn)
                {
                    if (App.Connection != null)
                    {
                        ValueSet valueSet = new ValueSet();
                        valueSet.Add("verb", "elevatedRegistryWrite");
                        AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);
                        //var resp = response.Message["response"] as string;
                    }
                }
                //else
                //{
                //    if (App.Connection != null)
                //    {
                //        ValueSet valueSet = new ValueSet();
                //        valueSet.Add("verb", "elevatedRegistryWrite");
                //        //AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);
                //        //var resp = response.Message["response"] as string;
                //    }
                //}
            }
        }

    }
}
