using log4net;
using log4net.Config;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenCommandWriteService
{
    class Program
    {
        private static void InitLog4Net()
        {
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
            logger = LogManager.GetLogger(typeof(Program));
        }

        static ILog logger = null;

        static int Main(string[] args)
        {

            try
            {
                InitLog4Net();
                logger.Info($"Init");
                // Open the base key for what we need, HKEY_LOCAL_MACHINE, with the 64 bit view for the process,
                // Only x64 will persist to the actual HKEY_LOCAL_MACHINE, x86 processes will manipulate
                // a virtualized user copy that does not persist to the actual machine hive.
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);

                // open the SubKey for the Startup Programs list, with write access. Requires Admin privilege
                // because it is in HKEY_LOCAL_MACHINE and we want to write to it.
                RegistryKey key = baseKey.OpenSubKey(@"Directory\Background\shell\cmd", true);

                string[] names = key.GetValueNames();

                bool isExtended = false;
                bool isShowBasedOnVelocityId = false;
                bool isHideBasedOnVelocityId = false;

                foreach (string name in names)
                {
                    if (name == "Extended")
                    {
                        isExtended = true;
                    }
                    if (name == "ShowBasedOnVelocityId")
                    {
                        isShowBasedOnVelocityId = true;
                    }
                    if (name == "HideBasedOnVelocityId")
                    {
                        isHideBasedOnVelocityId = true;
                    }
                }

                //logger.Info($"bNotepadPresent {bNotepadPresent}");

                if (true)
                {
                    // remove notepad from the list. If x64, notepad will no longer start after reboot.
                    if (isExtended)
                    {
                        key.DeleteValue("Extended");
                    }
                    else
                    {
                        key.SetValue("Extended", "");
                    }

                    if (isHideBasedOnVelocityId)
                    {
                        key.DeleteValue("HideBasedOnVelocityId");
                        key.SetValue("ShowBasedOnVelocityId", 0x00639bc8, RegistryValueKind.DWord);
                    }

                    if (isShowBasedOnVelocityId)
                    {
                        key.DeleteValue("ShowBasedOnVelocityId");
                        key.SetValue("HideBasedOnVelocityId", 0x00639bc8, RegistryValueKind.DWord);
                    }

                }
                //else
                //{
                //    // add notepad to the list. If x64, notepad will start after reboot.
                //    key.SetValue("notepad", @"C:\Windows\System32\notepad.exe");
                //}
            }
            catch (Exception ex)
            {
                //logger.Error(ex.Message);

                Debug.WriteLine(String.Format("In ElevatedRegistryWrite key.SetValue, exception={0}", ex.Message));

                // return codes: 0 = OK, 1 = ElevationDialogCancelled, 2 = Exception
                // return code 1 is not raised here, but by the dialog itself if user says "No"
                return 2;
            }

            return 0;
        }
    }
}