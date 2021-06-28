using log4net;
using log4net.Config;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace OpenCommandBgService
{
    class Program
    {
        private static void InitLog4Net()
        {
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
            logger = LogManager.GetLogger(typeof(Program));
        }

        // the AppServiceConnection to our UWP app
        private static AppServiceConnection connection = new AppServiceConnection();

        // HRESULT 80004005 is E_FAIL
        const int E_FAIL = unchecked((int)0x80004005);

        static ILog logger = null;

        static void Main(string[] args)
        {
            InitLog4Net();

            // The AppServiceName must match the name declared in the RegistryPackaging project's Package.appxmanifest file.
            // You'll have to view it as code to see the XML.  It will look like this:
            //
            //       <Extensions>
            //           <uap:Extension Category="windows.appService">
            //              <uap:AppService Name="CommunicationService" />
            //          </uap:Extension>
            //          <desktop:Extension Category="windows.fullTrustProcess" Executable="RegistryReadAppService\RegistryReadAppService.exe" />
            //       </Extensions>
            //
            connection.AppServiceName = "CommunicationService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            // hook up the connection event handlers
            connection.ServiceClosed += Connection_ServiceClosed;
            connection.RequestReceived += Connection_RequestReceived;

            AppServiceConnectionStatus result = AppServiceConnectionStatus.Unknown;

            // static void Main cannot be async until C# 7.1, so put this on the thread pool
            Task.Run(async () =>
            {
                // open a connection to the UWP AppService
                result = await connection.OpenAsync();

            }).GetAwaiter().GetResult();

            //logger.Info($"OpenCommandBgService AppServiceConnectionStatus {result}");

            if (result == AppServiceConnectionStatus.Success)
            {
                // To debug this app, you'll need to have it started in console mode.  Uncomment 
                // the lines below and then right-click on the project file to get to project settings.
                // Select the Application tab and change the Output Type from Windows Application to 
                // Console Application.  A "Windows Application" is simply a headless console app.

                //Console.WriteLine("Detatch your debugger from the UWP app and attach it to RegistryReadAppService.");
                //Console.WriteLine("Set your breakpoint in RegistryReadAppService and then press Enter to continue.");
                //Console.ReadLine();

                // Let the app service connection handlers respond to events.  If this Win32 app had a Window,
                // this would be a message loop.  The app ends when the app service connection to 
                // the UWP app is closed and our Connection_ServiceClosed event handler is fired.
                while (true)
                {
                    // the below is necessary if this were calling COM and this was STAThread
                    // pump the underlying STA thread
                    // https://blogs.msdn.microsoft.com/cbrumme/2004/02/02/apartments-and-pumping-in-the-clr/
                    // Thread.CurrentThread.Join(0);
                }
            }

        }

        /// <summary>
        /// The UWP host has sent a request for something. Responses to the UWP app are
        /// sent by the respective case handlers, to the UWP Connection_RequestReceived handler
        /// via the AppServiceConnection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async static void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = new ValueSet();

            // get the verb or "command" for this request
            string verb = message["verb"] as String;

            switch (verb)
            {
                // we received a request to get the Startup program names
                case "getStartupProgramNames":
                    {
                        try
                        {

                            // we switch on the value of the verb in the UWP app that receives this valueSet
                            returnData.Add("verb", "RegistryReadResult");

                            // open HKLM with a 64bit view. If you use Registry32, your view will be virtualized to the current user
                            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);

                            // Open the key where the Startup programs are listed for read-only access.  Cannot write
                            // to the registry from an unelevated Win32 process.
                            RegistryKey key = baseKey.OpenSubKey(@"Directory\Background\shell\cmd", false);

                            string[] names = key.GetValueNames();

                            // add the names to our response
                            returnData.Add("StartupProgramNames", names);
                            

                        }
                        catch (Exception ex)
                        {
                            returnData.Add("verb", "RegistryReadError");
                            returnData.Add("exceptionMessage", ex.Message.ToString());
                        }

                        break;
                    }

                // we received a request to write the registry
                case "elevatedRegistryWrite":
                    {
                        // the exitCode is the only response we receive from LaunchElevatedRegistryWrite
                        int exitCode = LaunchElevatedRegistryWrite();
                        returnData.Add("exitcode", exitCode);
                        //logger.Info($"OpenCommandBgService elevatedRegistryWrite exitcode {exitCode}");
                        break;
                    }
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
        /// Launch the elevated process.  The only way it can communicate back to this
        /// process is through its exit code.
        /// </summary>
        /// <returns></returns>
        private static int LaunchElevatedRegistryWrite()
        {
            int exitCode = 0;

            try
            {
                logger.Info("OpenCommandBgService LaunchElevatedRegistryWrite");
                // call the elevated process here to trigger assessment
                ProcessStartInfo info = new ProcessStartInfo();
                info.Verb = "runas";
                info.UseShellExecute = true;

                // this path is a proxy for the Package
                string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var servicePath = localAppDataPath + @"\microsoft\windowsapps\OpenCommandWriteService.exe";

                //{
                //    string result = Assembly.GetExecutingAssembly().Location;
                //    int index = result.LastIndexOf("\\");
                //    string rootPath = $"{result.Substring(0, index)}\\..\\";

                //    servicePath = rootPath + @"OpenCommandWriteService\OpenCommandWriteService.exe";
                //}

                info.FileName = servicePath;

                logger.Info($"{info.FileName}");

                Process elevatedProcess = null;

                try
                {
                    //Process.Start(info);
                    elevatedProcess = Process.Start(info);
                    //this should take only a very short time, so wait 10 seconds max
                    elevatedProcess?.WaitForExit(10000);

                    // if everything went normally, the exit code will be zero
                    exitCode = elevatedProcess.ExitCode;
                }
                catch (Exception ex)
                {
                    logger.Error($"{ex.Message}");
                    // default exception exitCode
                    exitCode = 3;

                    if (ex.HResult == E_FAIL)
                    {
                        // the user cancelled the elevated process
                        // by clicking "No" on the Windows elevation dialog
                        exitCode = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            return exitCode;
        }

        /// <summary>
        /// Our UWP app service is closing, so shut ourselves down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            logger.Info("OpenCommandBgService Connection_ServiceClosed");
            //System.Environment.Exit(0);
        }
    }
}
