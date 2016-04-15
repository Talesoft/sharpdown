using Microsoft.Win32;
using Sharpdown.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sharpdown
{
    static class Program
    {

        public static string ExecutablePath;
        public static string ProcessName;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args = null)
        {

            Program.ExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            Program.ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            
            if (!Settings.Default.Ie9StandardsModeSet) {

                if (RunAsAdministrator())
                    return;

                if (!Settings.Default.Ie9StandardsModeSet)
                    SetIe9KeyForWebBrowserControl();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new EditorForm(args));
        }



        public static bool RunAsAdministrator()
        {

            if (Program.IsAdministrator())
                return false;

            DialogResult result = MessageBox.Show(
                    "I need to set the Internet Explorer Mode to IE9 Standards mode in your registry or " +
                    "you'll have script errors with the Syntax Highlighter.\n" +
                    "Can I restart as Administrator once?",
                    "Listen!",
                    MessageBoxButtons.YesNo
                );

            if (result != DialogResult.Yes) {

                MessageBox.Show(
                    "Fine, but don't flame me for script errors!\n" +
                    "You can always set the key again in the settings.\n",
                    "Alright!"
                );
                Settings.Default.Ie9StandardsModeSet = true;
                Settings.Default.Save();
                return false;
            }

            // Restart program and run as admin
            ProcessStartInfo startInfo = new ProcessStartInfo(Program.ExecutablePath);
            startInfo.Verb = "runas";
            Process.Start(startInfo);
            Application.Exit();
            return true;
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void ReportUnknownError()
        {

            MessageBox.Show(
                "Failed to set your IE Mode to IE9 Standards mode.\nI don't even know what caused it, sorry. \n" +
                "I'm useless.",
                "Damn :("
            );
            Application.Exit();
            return;
        }

        //http://stackoverflow.com/questions/17922308/use-latest-version-of-ie-in-webbrowser-control
        public static void SetIe9KeyForWebBrowserControl()
        {


            RegistryKey key = null;
            try {

                //For 64 bit Machine 
                if (Environment.Is64BitOperatingSystem)
                    key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION",
                        true
                        );
                else  //For 32 bit Machine 
                    key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION",
                        true
                    );

                //If the path is not correct or 
                //If user dont have priviledges to access registry 
                if (key == null) {

                    if (Program.IsAdministrator())
                        Program.ReportUnknownError();
                    else {

                        Program.RunAsAdministrator();
                    }
                    return;
                }

                string appKey = Convert.ToString(key.GetValue(Program.ProcessName));

                //Check if key is already present 
                if (appKey == "9000") {

                    MessageBox.Show("I see it was set already, alright!", "Oh yeah!");
                    Settings.Default.Ie9StandardsModeSet = true;
                    Settings.Default.Save();

                    key.Close();
                    return;
                }

                //If key is not present add the key , Kev value 8000-Decimal 
                if (string.IsNullOrEmpty(appKey))
                    key.SetValue(Program.ProcessName, unchecked((int)0x2328), RegistryValueKind.DWord);

                //check for the key after adding 
                appKey = Convert.ToString(key.GetValue(Program.ProcessName));

                if (appKey == "9000") {

                    MessageBox.Show("Registry key successfully set. Have fun!", "Oh yeah!");
                    Settings.Default.Ie9StandardsModeSet = true;
                    Settings.Default.Save();
                } else {

                    Program.ReportUnknownError();
                    return;
                }


            } catch (Exception ex) {
                
                MessageBox.Show(ex.Message, "Oh shit!");
            } finally {

                //Close the Registry 
                if (key != null)
                    key.Close();
            }
        }
    }
}