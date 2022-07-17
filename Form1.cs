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

        private Settings LoadData() {
            var data = new Settings();

            // load in from registry

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
        }

        private static void SaveData(Settings data) {
            // save data back into registry

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\VAVE\\SK8\\Launcher", true)) {
                if (key != null) {
                    key.SetValue("Username", data.SetUsername);
                    key.SetValue("DisableWatermark", data.DisableWatermark.ToString().ToLower());
                    key.SetValue("Multiplayer", data.Multiplayer.ToString().ToLower());
                    key.SetValue("AdditionalSwitches", data.AdditionalSwitches);
                    key.SetValue("UseAdditionalSwitches", data.UseAdditionalSwitches.ToString().ToLower());
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            string executablePath = assemblyPath + "\\skate.crack.exe";

            /*
            var proc = new Process();
            proc.StartInfo.FileName = executablePath;
            proc.StartInfo.Arguments = $"-Online.ClientIsPresenceEnabled false -   -Client.ServerIp {textBox1.Text}:25200";
            */

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

            sb.Append("-thinclient 0 -Render.Rc2BridgeEnable 1 -Rc2Bridge. DeviceBackend Rc2BridgeBackend_Vulkan -RenderDevice. RenderCore2Enable 1");

            if (launchSettings.DisableWatermark) { sb.Append(" -DelMarUI.EnableWatermark false"); }
            if (!launchSettings.PresenceEnabled) { sb.Append(" -Online.ClientIsPresenceEnabled false"); }

            if (launchSettings.MultiplayerEnabled) {
                sb.Append($" -   -Client.ServerIp {launchSettings.ServerIP}");
            }

            sb.Append($" -DelMar.LocalPlayerDebugName {launchSettings.Username}");

            if(checkBox5.Checked) {
                sb.Append($" {textBox3.Text}");
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
            var newData = new Settings();

            newData.SetUsername = frm.textBox4.Text;
            newData.DisableWatermark = frm.checkBox2.Checked;
            newData.Multiplayer = frm.cbMultiplayer.Checked;
            newData.AdditionalSwitches = frm.textBox3.Text;
            newData.UseAdditionalSwitches = frm.checkBox5.Checked;

            SaveData(newData);

            Process.Start("taskkill.exe", "/f /im skate.crack.exe /t");
        }
    }
}
