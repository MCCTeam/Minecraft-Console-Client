using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MinecraftClient.WinForms
{
    public partial class Scripts : Form
    {
        public Scripts()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            scripts.Clear();
            listBox1.Items.Clear();
            string[] sDirectoryInfo = Directory.GetFiles(Application.StartupPath, "*.cs");
            foreach (string fullpath in sDirectoryInfo)
            {
                FileInfo fileInfo = new FileInfo(fullpath);
                scripts.Add(new ScriptsModel(fileInfo.FullName, fileInfo.Name));
                listBox1.Items.Add(fileInfo.Name);
            }
        }

        private void listBox1_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        List<ScriptsModel> scripts = new List<ScriptsModel>();
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            TimeZoneInfo moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime localtime = DateTime.Now;
            DateTime localmoscowTime = TimeZoneInfo.ConvertTime(localtime, moscowZone);

            foreach (ScriptsModel model in scripts)
            {
                if (listBox1.SelectedItem.ToString() == model.name)
                {
                    Login.client.BotLoad(new ChatBots.Script(model.fullpath));
                    break;
                }
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            scripts.Clear();
            listBox1.Items.Clear();
            string[] sDirectoryInfo = Directory.GetFiles(Application.StartupPath, "*.cs");
            foreach (string fullpath in sDirectoryInfo)
            {
                FileInfo fileInfo = new FileInfo(fullpath);
                scripts.Add(new ScriptsModel(fileInfo.FullName, fileInfo.Name));
                listBox1.Items.Add(fileInfo.Name);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            MinecraftClient.WinForms.Menus.Store_Scripts store = new MinecraftClient.WinForms.Menus.Store_Scripts();
            store.Show();
        }
    }
    public class ScriptsModel
    {
        public ScriptsModel(string path, string name)
        {
            this.fullpath = path;
            this.name = name;
        }
        public string fullpath;
        public string name;
    }

}
