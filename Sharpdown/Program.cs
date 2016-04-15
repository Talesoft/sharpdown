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
        public static bool WillShutdown;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args = null)
        {

            ExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            WillShutdown = false;

            //Reset the settings while debugging.
            if (Debugger.IsAttached)
                Settings.Default.Reset();

            CheckIe9StandardsModeForWebBrowserControl();

            if (!WillShutdown) {

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new EditorForm(args));
            }
        }

        public static void Shutdown()
        {

            if (Application.OpenForms.Count > 0)
                foreach (Form form in Application.OpenForms)
                    form.Close();

            Application.Exit();
            WillShutdown = true;
        }

        public static void DisplayError(string message, bool fatal = false)
        {

            MessageBox.Show(message, "Oh noes!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (fatal)
                Shutdown();
        }

        public static void DisplayInfo(string message)
        {

            MessageBox.Show(message, "Attention!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult DisplayYesNoQuestion(string question, string title = null)
        {

            return MessageBox.Show(question, title != null ? title : "Attention!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public static void CheckIe9StandardsModeForWebBrowserControl()
        {

            if (Settings.Default.IsIe9StandardsModeSet || !Settings.Default.CheckIfIe9StandardsModeIsSet)
                return;

            if (DisplayYesNoQuestion(
                "I need to set the Internet Explorer Mode to IE9 Standards mode in your registry or " +
                "you'll have script errors with the Syntax Highlighter.\n" +
                "If you're not an administrator, I probably have to restart. Can I?") != DialogResult.Yes) {

                DisplayInfo(
                    "Fine, but don't flame me for script errors!\n" +
                    "You can always set the key again in the settings.\n"
                );
                Settings.Default.CheckIfIe9StandardsModeIsSet = false;
                Settings.Default.Save();
                return;
            }

            if (!IsAdministrator()) {

                RunAsAdministrator();
                return;
            }

            if (!Settings.Default.IsIe9StandardsModeSet)
                SetIe9KeyForWebBrowserControl();
        }

        public static void RunAsAdministrator()
        {

            // Restart program and run as admin
            ProcessStartInfo startInfo = new ProcessStartInfo(ExecutablePath);
            startInfo.Verb = "runas";
            Process.Start(startInfo);
            WillShutdown = true;
            Shutdown();
        }

        public static bool IsAdministrator()
        {

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void ReportUnknownError()
        {

            DisplayError(
                "Failed to set your IE Mode to IE9 Standards mode.\nI don't even know what caused it, sorry. \n" +
                "I'm useless.",
                true
            );
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
                //Since we checked access before already, it's an unknown error of some kind.
                if (key == null)
                    ReportUnknownError();

                string appKey = Convert.ToString(key.GetValue(ProcessName));

                //Check if key is already present 
                if (appKey == "9000") {
                    
                    //Just fucken silently continue and let the men (and women!) do their work.
                    Settings.Default.IsIe9StandardsModeSet = true;
                    Settings.Default.CheckIfIe9StandardsModeIsSet = false;
                    Settings.Default.Save();

                    key.Close();
                    return;
                }

                //If key is not present add the key , Kev value 8000-Decimal 
                if (string.IsNullOrEmpty(appKey))
                    key.SetValue(ProcessName, unchecked((int)0x2328), RegistryValueKind.DWord);

                //check for the key after adding 
                appKey = Convert.ToString(key.GetValue(ProcessName));

                if (appKey == "9000") {

                    DisplayInfo("Registry key successfully set. Have fun!");
                    Settings.Default.IsIe9StandardsModeSet = true;
                    Settings.Default.CheckIfIe9StandardsModeIsSet = false;
                    Settings.Default.Save();
                } else {

                    ReportUnknownError();
                    return;
                }


            } catch (Exception ex) {
                
                DisplayError(ex.Message, true);
            } finally {

                //Close the Registry 
                if (key != null)
                    key.Close();
            }
        }
    }
}