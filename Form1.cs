using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net;

namespace SkateLauncher {
    public partial class Form1 : Form {
        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public Form1() {
            InitializeComponent();
        }

        static Form1 frm;

        struct LaunchData {
            public bool DisableWatermark;
            public bool MultiplayerEnabled;
            public bool PickedFromServerList;
            public bool IsServerHost;
            public bool PresenceEnabled;
            public string Username;
            public string ServerIP;
            public string AdditionalArgs;
        }

        struct Settings {
            public string SetUsername;
            public bool DisableWatermark;
            public bool Multiplayer;
            public string AdditionalSwitches;
            public bool UseAdditionalSwitches;
        }

        private string GetFile() {
            StackTrace st = new StackTrace(new StackFrame(true));
            StackFrame sf = st.GetFrame(0);
            return sf.GetFileName();
        }

        private Settings LoadData() {
            var data = new Settings();

            // load in from registry

            string iniPath = "";

            if(File.Exists($"C:\\Users\\{Environment.UserName}\\Documents\\VAVE\\Outsourcing\\SK8\\settings.ini")) {
                iniPath = $"C:\\Users\\{Environment.UserName}\\Documents\\VAVE\\Outsourcing\\SK8\\settings.ini";
            }
            else {
                MessageBox.Show($"Cannot find settings file {"settings.ini"} at any of the storage locations\n {GetFile()}");

                return data;
            }

            var ini = new IniFile(iniPath);

            try {
                data.DisableWatermark = bool.Parse(ini.Read("DisableWatermark"));
                data.Multiplayer = bool.Parse(ini.Read("MultiplayerEnabled"));
                data.SetUsername = ini.Read("Username");
                data.AdditionalSwitches = ini.Read("AdditionalArgs");
                data.UseAdditionalSwitches = bool.Parse(ini.Read("UsingAdditionalArgs"));
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                Application.Exit();
            }

            return data;

            /*
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\VAVE\\SK8\\Launcher")) {
                    if (key != null) {
                        data.SetUsername = (string)key.GetValue("Username");
                        data.DisableWatermark = bool.Parse((string)key.GetValue("DisableWatermark"));
                        data.Multiplayer = bool.Parse((string)key.GetValue("Multiplayer"));
                        data.AdditionalSwitches = (string)key.GetValue("AdditionalSwitches");
                        data.UseAdditionalSwitches = bool.Parse((string)key.GetValue("UseAdditionalSwitches"));
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                Application.Exit();
            }

            return data;
            */
        }

        private static void SaveData(Settings data) {
            // save data back into registry

            if(!Debugger.IsAttached) {
                string iniPath = "";

                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (File.Exists($"C:\\Users\\{Environment.UserName}\\Documents\\VAVE\\Outsourcing\\SK8\\settings.ini")) {
                    iniPath = $"C:\\Users\\{Environment.UserName}\\Documents\\VAVE\\Outsourcing\\SK8\\settings.ini";
                }

                var ini = new IniFile(iniPath);
                ini.Write("DisableWatermark", data.DisableWatermark.ToString().ToLower());
                ini.Write("MultiplayerEnabled", data.Multiplayer.ToString().ToLower());
                ini.Write("Username", data.SetUsername);
                ini.Write("AdditionalArgs", data.AdditionalSwitches);
                ini.Write("UsingAdditionalArgs", data.UseAdditionalSwitches.ToString().ToLower());

                MessageBox.Show("Saved.");
            }
            else {
                MessageBox.Show("Debugger attached, will not save.");
            }

            /*
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\VAVE\\SK8\\Launcher", true)) {
                if (key != null) {
                    key.SetValue("Username", data.SetUsername);
                    key.SetValue("DisableWatermark", data.DisableWatermark.ToString().ToLower());
                    key.SetValue("Multiplayer", data.Multiplayer.ToString().ToLower());
                    key.SetValue("AdditionalSwitches", data.AdditionalSwitches);
                    key.SetValue("UseAdditionalSwitches", data.UseAdditionalSwitches.ToString().ToLower());
                }
            }
            */
        }

        private void button1_Click(object sender, EventArgs e) {
            string executablePath = "";

            if(File.Exists(assemblyPath + "\\skate.crack.exe")) {
                assemblyPath = assemblyPath + "\\skate.crack.exe";
            }
            else {
                MessageBox.Show("Unable to find game executable in " + assemblyPath);
                return;
            }

            var sb = new StringBuilder();

            var launchSettings = new LaunchData();

            if(cbMultiplayer.Checked) {
                if(UserHasAccessToMultiplayer()) {
                    launchSettings.MultiplayerEnabled = true;

                    launchSettings.PickedFromServerList = !checkBox3.Checked;

                    if (checkBox3.Checked) {
                        launchSettings.ServerIP = $"{textBox1.Text}:{textBox2.Text}";
                    }
                    else {
                        launchSettings.ServerIP = "sk8dyngs.ddns.net:254";
                    }

                    launchSettings.Username = textBox4.Text;

                    launchSettings.DisableWatermark = checkBox2.Checked;
                    launchSettings.PresenceEnabled = checkBox1.Checked;
                }
                else {
                    // doesnt have access

                    MessageBox.Show("You do not have access to multiplayer. Please apply by DM'ing a member of support staff.");
                    return;
                    //Application.Exit();
                }
            }
            else {
                launchSettings.MultiplayerEnabled = false;
            }

            //sb.Append("-thinclient 0 -Render.Rc2BridgeEnable 1 -Rc2Bridge. DeviceBackend Rc2BridgeBackend_Vulkan -RenderDevice. RenderCore2Enable 1");

            if (launchSettings.DisableWatermark) { sb.Append(" -DelMarUI.EnableWatermark false"); }
            if (!launchSettings.PresenceEnabled) { sb.Append(" -Online.ClientIsPresenceEnabled false"); }

            if (launchSettings.MultiplayerEnabled) {
                sb.Append($" -   -Client.ServerIp {launchSettings.ServerIP}");
            }

            sb.Append($" -DelMar.LocalPlayerDebugName {launchSettings.Username}");

            if(checkBox5.Checked) {
                sb.Append($" {textBox3.Text}");
            }

            if(checkBox6.Checked) {
                sb.Append("-WorldRender.ShadowmapsEnable false");
            }

            if(checkBox7.Checked) {
                sb.Append("-DebugRender true");
            }

            if(checkBox8.Checked) {
                sb.Append("-UI.DrawEnable false");
            }

            string args = sb.ToString();

            /*
            if(!Debugger.IsAttached) {
                
            }
            else {
                var fbox = new Form2(args);
                fbox.Show();
            }*/

            var proc = new Process();
            proc.StartInfo.FileName = executablePath;
            proc.StartInfo.Arguments = args;

            proc.Start();
        }

        private bool UserHasAccessToMultiplayer() {
            bool allowed = false;
            using (WebClient client = new WebClient()) {
                string s = client.DownloadString("https://cdn.vavestudios.com/usergenerated/fupload/30757/allowlist.txt");

                foreach (var line in s.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (line == $"{Environment.UserName}:{Environment.MachineName}") {
                        allowed = true;
                    }
                }
            }

            return allowed;
        }

        private void Form1_Load(object sender, EventArgs e) {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            frm = this;

            var data = LoadData();

            if (data.SetUsername != "") {
                textBox4.Text = data.SetUsername;
            }
            else {
                textBox4.Text = Environment.UserName;
            }

            checkBox2.Checked = data.DisableWatermark;
            cbMultiplayer.Checked = data.Multiplayer;

            textBox3.Text = data.AdditionalSwitches;
            checkBox5.Checked = data.UseAdditionalSwitches;

            if(UserHasAccessToMultiplayer()) {
                listBox1.Visible = true;
            }

            MessageBox.Show("RULES:\n- No racism whatsoever\n- NO cheats whatsoever\n- No disrespecting others\n\nIf any of those rules get broken by you, you will INSTANTLY get de-whitelisted from the Launcher meaning you wont be able to play."); ;
        }

        private void cbMultiplayer_CheckedChanged(object sender, EventArgs e) {
            //groupBox1.Enabled = cbMultiplayer.Checked;
        }

        static void OnProcessExit(object sender, EventArgs e) {
            //SaveData(frm.GetSaveable());

            Process.Start("taskkill.exe", "/f /im skate.crack.exe /t");
        }

        private void button5_Click(object sender, EventArgs e) {
            SaveData(GetSaveable());
        }

        private Settings GetSaveable() {
            var data = new Settings();

            data.SetUsername = textBox4.Text;
            data.DisableWatermark = checkBox2.Checked;
            data.Multiplayer = cbMultiplayer.Checked;
            data.AdditionalSwitches = textBox3.Text;
            data.UseAdditionalSwitches = checkBox5.Checked;

            return data;
        }
    }
}
