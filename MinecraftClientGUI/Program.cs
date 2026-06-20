using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MinecraftClientGUI
{
    static class Program
    {
        private const string ReleasesUrl = "https://github.com/MCCTeam/Minecraft-Console-Client/releases";

        /// <summary>
        /// Minecraft Console Client GUI by ORelio (c) 2013.
        /// Allows to use Minecraft Console Client in a more user friendly interface
        /// This source code is released under the CDDL 1.0 License.
        /// </summary>
        
        [STAThread]
        static void Main(string[] args)
        {
            if (!System.IO.File.Exists(MinecraftClient.ExePath))
            {
                DialogResult result = MessageBox.Show(
                    "File not found: " + MinecraftClient.ExePath + Environment.NewLine + Environment.NewLine
                    + "Place MinecraftClient.exe in the same folder as MinecraftClientGUI.exe." + Environment.NewLine + Environment.NewLine
                    + "Download MinecraftClient.exe from:" + Environment.NewLine
                    + ReleasesUrl + Environment.NewLine + Environment.NewLine
                    + "Open the releases page now?",
                    "Minecraft client not found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1(args));
            }
        }
    }
}
