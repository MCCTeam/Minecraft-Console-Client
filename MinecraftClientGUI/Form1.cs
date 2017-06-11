using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MinecraftClientGUI
{
    /// <summary>
    /// The main graphical user interface
    /// </summary>

    public partial class Form1 : Form
    {
        private LinkedList<string> previous = new LinkedList<string>();
        private MinecraftClient Client;
        private Thread t_clientread;

        #region Aero Glass Low-level Windows API

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);

        #endregion

        public Form1(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0) { initClient(new MinecraftClient(args)); }
        }

        /// <summary>
        /// Define some element properties and init Aero Glass if using Vista or newer
        /// </summary>

        private void Form1_Load(object sender, EventArgs e)
        {
            box_output.ScrollBars = RichTextBoxScrollBars.None;
            box_output.Font = new Font("Consolas", 8);
            box_output.BackColor = Color.White;

            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor == 1)
            {
                this.BackColor = Color.DarkMagenta; this.TransparencyKey = Color.DarkMagenta;
                MARGINS marg = new MARGINS() { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(this.Handle, ref marg);
            }
        }

        /// <summary>
        /// Launch the Minecraft Client by clicking the "Go!" button.
        /// If a client is already running, it will be closed.
        /// </summary>

        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (Client != null)
            {
                Client.Close();
                t_clientread.Abort();
                box_output.Text = "";
            }
            string username = box_Login.Text;
            string password = box_password.Text;
            string serverip = box_ip.Text;
            if (password == "") { password = "-"; }
            if (username != "" && serverip != "")
            {
                initClient(new MinecraftClient(username, password, serverip));
            }
        }

        /// <summary>
        /// Handle a new Minecraft Client
        /// </summary>
        /// <param name="client">Client to handle</param>

        private void initClient(MinecraftClient client)
        {
            Client = client;
            t_clientread = new Thread(new ThreadStart(t_clientread_loop));
            t_clientread.Start();
            box_input.Select();
        }

        /// <summary>
        /// Thread reading output from the Minecraft Client
        /// </summary>

        private void t_clientread_loop()
        {
            while (true && !Client.Disconnected)
            {
                printstring(Client.ReadLine());
            }
        }

        /// <summary>
        /// Print a Minecraft-Formatted string to the console area
        /// </summary>
        /// <param name="str">String to print</param>

        private void printstring(string str)
        {
            if (!String.IsNullOrEmpty(str))
            {
                Color color = Color.Black;
                FontStyle style = FontStyle.Regular;
                string[] subs = str.Split('§');
                if (subs[0].Length > 0) { AppendTextBox(box_output, subs[0], Color.Black, FontStyle.Regular); }
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 0)
                    {
                        if (subs[i].Length > 1)
                        {
                            switch (subs[i][0])
                            {
                                //Font colors
                                case '0': color = Color.Black; break;
                                case '1': color = Color.DarkBlue; break;
                                case '2': color = Color.DarkGreen; break;
                                case '3': color = Color.DarkCyan; break;
                                case '4': color = Color.DarkRed; break;
                                case '5': color = Color.DarkMagenta; break;
                                case '6': color = Color.DarkGoldenrod; break;
                                case '7': color = Color.DimGray; break;
                                case '8': color = Color.Gray; break;
                                case '9': color = Color.Blue; break;
                                case 'a': color = Color.Green; break;
                                case 'b': color = Color.CornflowerBlue; break;
                                case 'c': color = Color.Red; break;
                                case 'd': color = Color.Magenta; break;
                                case 'e': color = Color.Goldenrod; break;

                                //White on white = invisible so use gray instead
                                case 'f': color = Color.DimGray; break;

                                //Font styles. Can use several styles eg Bold + Underline
                                case 'l': style = style | FontStyle.Bold; break;
                                case 'm': style = style | FontStyle.Strikeout; break;
                                case 'n': style = style | FontStyle.Underline; break;
                                case 'o': style = style | FontStyle.Italic; break;

                                //Reset font color & style
                                case 'r': color = Color.Black; style = FontStyle.Regular; break;
                            }

                            AppendTextBox(box_output, subs[i].Substring(1, subs[i].Length - 1), color, style);
                        }
                    }
                }
                AppendTextBox(box_output, "\n", Color.Black, FontStyle.Regular);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Append text to a RichTextBox with font customization
        /// </summary>
        /// <param name="box">Target RichTextBox</param>
        /// <param name="text">Text to add</param>
        /// <param name="color">Color of the text</param>
        /// <param name="style">Font style of the text</param>

        private void AppendTextBox(RichTextBox box, string text, Color color, FontStyle style)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<RichTextBox, string, Color, FontStyle>(AppendTextBox), new object[] { box, text, color, style });
            }
            else
            {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;
                box.SelectionColor = color;
                box.SelectionFont = new Font(box.Font, style);
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
                box.SelectionStart = box.Text.Length;
                box.ScrollToCaret();
            }
        }

        /// <summary>
        /// Properly disconnect the client when clicking the [X] close button
        /// </summary>

        protected void onClose(object sender, EventArgs e)
        {
            if (t_clientread != null) { t_clientread.Abort(); }
            if (Client != null) { new Thread(new ThreadStart(Client.Close)).Start(); }
        }

        /// <summary>
        /// Allows an Enter keypress in "Login", "Password" or "Server IP" box to be considered as a click on the "Go!" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void loginBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_connect_Click(sender, e);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle special functions in the input box : send with Enter key, command history and tab-complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_send_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (previous.Count > 0)
                {
                    box_input.Text = previous.First.Value;
                    previous.AddLast(box_input.Text);
                    previous.RemoveFirst();
                    box_input.Select(box_input.Text.Length, 0);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (previous.Count > 0)
                {
                    box_input.Text = previous.Last.Value;
                    previous.AddFirst(box_input.Text);
                    previous.RemoveLast();
                    box_input.Select(box_input.Text.Length, 0);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                if (box_input.SelectionStart > 0)
                {
                    string behind_cursor = box_input.Text.Substring(0, box_input.SelectionStart);
                    string after_cursor = box_input.Text.Substring(box_input.SelectionStart);
                    string[] behind_temp = behind_cursor.Split(' ');
                    string autocomplete = Client.tabAutoComplete(behind_temp[behind_temp.Length - 1]);
                    if (!String.IsNullOrEmpty(autocomplete))
                    {
                        behind_temp[behind_temp.Length - 1] = autocomplete;
                        behind_cursor = String.Join(" ", behind_temp);
                        box_input.Text = behind_cursor + after_cursor;
                        box_input.SelectionStart = behind_cursor.Length;
                    }
                }
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Send the input in the input box, if any, by pressing the "Send" button.
        /// Handle "/quit" command to properly disconnect and close the GUI.
        /// </summary>

        private void btn_send_Click(object sender, EventArgs e)
        {
            if (Client != null)
            {
                if (box_input.Text.Trim().ToLower() == "/quit")
                {
                    Close();
                }
                else
                {
                    Client.SendText(box_input.Text);
                    previous.AddLast(box_input.Text);
                    box_input.Text = "";
                }
            }
        }

        /// <summary>
        /// Draw text on glass pane without ClearType, only black pixels
        /// </summary>

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            e.Graphics.DrawString("Login Details", this.Font, Brushes.Black, 20, 11);
            e.Graphics.DrawString("Username:", this.Font, Brushes.Black, 20, 31);
            e.Graphics.DrawString("Password:", this.Font, Brushes.Black, 191, 31);
            e.Graphics.DrawString("Server IP:", this.Font, Brushes.Black, 355, 31);
        }

        /// <summary>
        /// Show the "About" message box, open the official topic in an internet browser if the user press OK.
        /// </summary>

        private void btn_about_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("MCC GUI version 1.0 - (c) 2013 ORelio\nAllows to send commands to any Minecraft server\nand receive text messages in a fast and easy way.\n\nPress OK to visit the official topic on Minecraft Forums.",
                "About Minecraft Console Client", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                System.Diagnostics.Process.Start("http://www.minecraftforum.net/topic/1314800-/");
            }
        }

        /// <summary>
        /// Open a link located in the console window
        /// </summary>

        private void LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(e.LinkText); }
            catch (Exception ex) { MessageBox.Show("An error occured while opening the link :\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
