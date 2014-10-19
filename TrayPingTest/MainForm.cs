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
using System.Net;
using System.Net.NetworkInformation;
using NLog;

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

        /// <summary>
        /// The Logentries log handler
        /// </summary>
        private static Logger log = LogManager.GetCurrentClassLogger();

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

        void RunPingTest(object sender, EventArgs e)
        {
            // TODO Check if the IP Address is set
            targetHost = targetIpAddress.Text;

            try
            {
                BackgroundTimer.Stop(); 
                
                Ping pingSender = new Ping();
                IPAddress address = IPAddress.Parse(targetHost);
                PingReply reply = pingSender.Send(address);

                if (reply.Status == IPStatus.Success)
                {
                    // Successful connection
                    string Message = string.Format("host={2} result=OK status={0} latency={1}ms", reply.Status.ToString(), reply.RoundtripTime, targetHost);
                    CloudLogging.AddLogEntry(Message, 2);
                    if (FailureCount > 0) FailureCount--;
                }
                else
                {
                    string Message = string.Format("host={1} result=FAIL status={0}", reply.Status.ToString(), targetHost);
                    CloudLogging.AddLogEntry(Message, 4);
                    if (FailureCount < 9) FailureCount++;
                }

            }
            catch(Exception ex)
            {
                if (FailureCount < 9) FailureCount++;

                string strToWrite = string.Format("host={1} result=EXCEPTION status={0}", ex.Message, targetHost);

                if (enableLogFile.Checked)
                {
                    StreamWriter logFileHandle = File.AppendText(saveDialogLocalLog.FileName);
                    logFileHandle.AutoFlush = true;
                    logFileHandle.Write(strToWrite);
                    logFileHandle.Close();
                }

                if (Properties.Settings.Default.EnableCloudLogging)
                {
                    CloudLogging.AddLogEntry(strToWrite, 5);
                }

            }
            finally
            {
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

        /// <summary>
        /// Manage the background cloud log uploads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogUploadTimer_Tick(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.EnableCloudLogging) CloudLogging.UploadLogEntries();
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.LogEntriesAPIKey = textBox1.Text;
            Properties.Settings.Default.EnableCloudLogging = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void OpenGithubProjectPage(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/woganmay/tray-ping-test");
        }

    }
}
