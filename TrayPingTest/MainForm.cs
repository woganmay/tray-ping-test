using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;

namespace TrayPingTest
{
    public partial class MainForm : Form
    {

        /// <summary>
        /// Keep track of sequential failures
        /// </summary>
        private int FailureCount;

        /// <summary>
        /// The host:port being targeted
        /// </summary>
        string targetHost;

        public MainForm()
        {
            InitializeComponent();
            SetStatusText(BackgroundTimer.Enabled);
            FailureCount = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Start the configured ping test
        }

        async void RunPingTest(object sender, EventArgs e)
        {
            // TODO Check if the IP Address is set
            targetHost = targetIpAddress.Text;
            string[] ipAddress = targetIpAddress.Text.Split(':');
            string ip = ipAddress[0];
            int port = int.Parse(ipAddress[1]);

            if (port == 0) port = 23;

            Console.WriteLine("Running ping test on " + ip);

            TcpClient client = new TcpClient();

            try
            {
                BackgroundTimer.Stop();

                await client.ConnectAsync(ip, port);
                if (client.Connected)
                {
                    if (FailureCount > 0) FailureCount--;
                }
                else
                {
                    if (FailureCount < 9) FailureCount++;
                }

            }
            catch(Exception ex)
            {
                if (FailureCount < 9) FailureCount++;

                StreamWriter logFileHandle = File.AppendText(saveDialogLocalLog.FileName);
                logFileHandle.AutoFlush = true;

                if (enableLogFile.Checked)
                {
                    string strToWrite = string.Format("{0}\t{1}\t{2}\n", DateTime.Now.ToString("HH:mm:ss"), targetHost, ex.Message);
                    logFileHandle.Write(strToWrite);
                    logFileHandle.Close();
                }

            }
            finally
            {
                client.Close();
                UpdateTrayIcon(FailureCount);
                BackgroundTimer.Start();
            }

        }

        /// <summary>
        /// http://stackoverflow.com/a/10380166
        /// </summary>
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void UpdateTrayIcon(int failureCount)
        {
            string icon = string.Format("Icons/icon_{0}.ico", failureCount);
            TrayIcon.Icon = new Icon(icon);
        }

        /// <summary>
        /// Changes the status text displayed in the label
        /// </summary>
        /// <param name="status"></param>
        private void SetStatusText(bool status)
        {
            statusLabel.Text = (status) ? " ONLINE" : "OFFLINE";
            statusLabel.ForeColor = (status) ? Color.Green : Color.Red;
            testSwitch.Text = (status) ? "Switch OFF" : "Switch ON";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch(BackgroundTimer.Enabled)
            {
                case true: BackgroundTimer.Stop(); break;
                case false: BackgroundTimer.Start(); break;
                default: break;
            }

            SetStatusText(BackgroundTimer.Enabled);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            saveDialogLocalLog.ShowDialog();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.TestTarget = targetIpAddress.Text;
            Properties.Settings.Default.Save();
        }

        private void saveDialogLocalLog_FileOk(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.LocalFileLogLocation = saveDialogLocalLog.FileName;
            Properties.Settings.Default.Save();
        }

        private void enableLogFile_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableLocalFileSave = enableLogFile.Checked;
            Properties.Settings.Default.Save();
        }

        private void ShowMainForm(object sender, EventArgs e)
        {
            this.Show();
        }

        private void InterceptFormClose(object sender, FormClosingEventArgs e)
        {
            this.Hide();

            if (Properties.Settings.Default.ShowCloseNotice)
            {
                TrayIcon.ShowBalloonTip(3, "Minimized to tray", "Click this notice to permanently hide it.", ToolTipIcon.Info);
            }

            e.Cancel = true;
        }

        private void TerminateApplication(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void DisableNoticeBalloon(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowCloseNotice = false;
            Properties.Settings.Default.Save();
        }

    }
}
