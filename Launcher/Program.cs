using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Launcher
{
    static class Program
    {
        static void Main(string[] args)
        {
            // determine the package root, based on own location
            string result = Assembly.GetExecutingAssembly().Location;
            int index = result.LastIndexOf("\\");
            string rootPath = $"{result.Substring(0, index)}\\..\\";

            // process object to keep track of your child process
            Process newProcess = null;

            if (args.Length > 2)
            {
                // launch process based on parameter
                switch (args[2])
                {
                    case "/background":
                        newProcess = Process.Start(rootPath + @"OpenCommandBgService\OpenCommandBgService.exe");
                        break;
                    case "/write":
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.Verb = "runas";
                        info.UseShellExecute = true;
                        info.FileName = rootPath + @"OpenCommandWriteService\OpenCommandWriteService.exe";
                        Process.Start(info);
                        break;
                    case "/mstsc":
                        Process.Start(@"mstsc.exe");
                        break;
                    case "/parameters":
                        //string parameters = ApplicationData.Current.LocalSettings.Values["parameters"] as string;
                        //newProcess = Process.Start(rootPath + @"FullTrust_WPF\FullTrust_WPF.exe", parameters);
                        break;
                }
            }
        }
    }
}
