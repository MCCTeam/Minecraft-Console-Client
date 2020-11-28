using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MinecraftClient.WinForms.Menus
{
    public partial class Store_Scripts : Form
    {
        public Store_Scripts()
        {
            InitializeComponent();
        }
        private List<DownloadedLists> scripts = new List<DownloadedLists>();
        private void Store_Scripts_Load(object sender, EventArgs e)
        {
            Update();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Update();
        }
        public void Update()
        {
            try
            {
                listBox1.Items.Clear();
                scripts.Clear();
                using (WebClient wc = new WebClient())
                {
                    string list = wc.DownloadString("https://raw.githubusercontent.com/Nekiplay/Minecraft-Console-Client-Premium-ServerSide/master/scripts/" + Login.client.GetServerHost() + "/list.txt");
                    string[] stringSeparators = new string[] { "\n" };
                    string[] result = list.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string file in result)
                    {
                        if (file != "")
                        {
                            string name = Regex.Match(file, "(.*)<").Groups[1].Value;
                            string server = Regex.Match(file, "(.*)<(.*)>").Groups[2].Value;
                            Console.WriteLine(server);
                            Console.WriteLine(Login.client.GetServerHost());
                            if (server == Login.client.GetServerHost())
                            {
                                scripts.Add(new DownloadedLists { name = name, download = "https://raw.githubusercontent.com/Nekiplay/Minecraft-Console-Client-Premium-ServerSide/master/scripts/" + Login.client.GetServerHost() + "/" + name });
                                listBox1.Items.Add(name);
                            }
                        }
                    }
                }
            } catch { }
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            TimeZoneInfo moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime localtime = DateTime.Now;
            DateTime localmoscowTime = TimeZoneInfo.ConvertTime(localtime, moscowZone);
            foreach (DownloadedLists model in scripts)
            {
                if (model.name != "")
                {
                    if (listBox1.SelectedItem.ToString() == model.name)
                    {
                        using (WebClient wc = new WebClient())
                        {
                            string down = wc.DownloadString(model.download);
                            if (!File.Exists(model.name))
                            {
                                File.Create(model.name).Close();
                            }
                            using (StreamWriter sw = new StreamWriter(model.name, false, System.Text.Encoding.Default))
                            {
                                sw.WriteLine(down);

                            }
                        }
                        break;
                    }
                }
            }
        }
    }
    public class DownloadedLists
    {
        public string download;
        public string name;
    }
}
