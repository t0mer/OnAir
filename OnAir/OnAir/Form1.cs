using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using NAudio.CoreAudioApi;

namespace OnAir
{
    public partial class Form1 : Form
    {
        // Helpers
        private CameraHelper cameraHelper;
        private AudioHelper audioHelper;

        // Configuration values.
        private readonly string cameraHardwareId;
        private readonly string postUrl;
        private readonly string getUrl;
        private readonly int activeMeetingWithCameraPreset;
        private readonly int activeMeetingWithoutCameraPreset;
        private readonly int noMeetingWithCameraPreset;
        private readonly int noMeetingWithoutCameraPreset;

        // Monitoring timer and state.
        private System.Windows.Forms.Timer monitorTimer;
        private bool monitoringActive = true;
        private DeviceStatus lastStatus = null;

        // System tray icon and context menu.
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public Form1()
        {
            InitializeComponent();

            // Read configuration values.
            cameraHardwareId = ConfigurationManager.AppSettings["CameraHardwareId"] ?? "VID_5986&PID_2113";
            postUrl = ConfigurationManager.AppSettings["PostUrl"] ?? "http://192.168.0.96/json";
            getUrl = ConfigurationManager.AppSettings["GetUrl"] ?? "http://192.168.0.96/win";
            activeMeetingWithCameraPreset = int.Parse(ConfigurationManager.AppSettings["ActiveMeetingWithCameraPreset"] ?? "1");
            activeMeetingWithoutCameraPreset = int.Parse(ConfigurationManager.AppSettings["ActiveMeetingWithoutCameraPreset"] ?? "2");
            noMeetingWithCameraPreset = int.Parse(ConfigurationManager.AppSettings["NoMeetingWithCameraPreset"] ?? "5");
            noMeetingWithoutCameraPreset = int.Parse(ConfigurationManager.AppSettings["NoMeetingWithoutCameraPreset"] ?? "3");

            cameraHelper = new CameraHelper();
            audioHelper = new AudioHelper();

            // Initialize system tray icon and context menu.
            InitializeTrayIcon();

            // Setup timer to check every 5 seconds.
            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 5000; // 5 seconds
            monitorTimer.Tick += async (s, e) => await CheckAndUpdateStatusAsync();
            monitorTimer.Start();

            // Hide the form and wire up load/closing events.
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // Hide the form and remove it from the taskbar.
            this.Hide();
            this.ShowInTaskbar = false;

            // Send startup GET request.
            await SendGetRequestAsync($"{getUrl}&T=1");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // On exit, send the exit GET request synchronously.
            SendGetRequestAsync($"{getUrl}&T=0").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes the system tray icon and its context menu.
        /// </summary>
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            // "Start Monitoring" menu item.
            var startItem = new ToolStripMenuItem("Start Monitoring", null, (s, e) =>
            {
                if (!monitoringActive)
                {
                    monitorTimer.Start();
                    monitoringActive = true;
                    trayIcon.ShowBalloonTip(3000, "Monitoring Started", "Monitoring has resumed.", ToolTipIcon.Info);
                }
            });

            // "Stop Monitoring" menu item.
            var stopItem = new ToolStripMenuItem("Stop Monitoring", null, (s, e) =>
            {
                if (monitoringActive)
                {
                    monitorTimer.Stop();
                    monitoringActive = false;
                    trayIcon.ShowBalloonTip(3000, "Monitoring Stopped", "Monitoring has been paused.", ToolTipIcon.Info);
                }
            });

            // "Exit" menu item.
            var exitItem = new ToolStripMenuItem("Exit", null, async (s, e) =>
            {
                trayIcon.Visible = false;
                await SendGetRequestAsync($"{getUrl}&T=0");
                Application.Exit();
            });

            trayMenu.Items.Add(startItem);
            trayMenu.Items.Add(stopItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                ContextMenuStrip = trayMenu,
                // Set your icon from resources. For example, if your resource is named "MyIcon":
                // Icon = Properties.Resources.MyIcon,
                // If not using resources, you can load from a file:
                // Icon = new Icon("MyIcon.ico"),
                Icon = Properties.Resources.tray,
                Visible = true,
                Text = "OnAir Monitor"
            };

            // Optional: on double-click, force an immediate status update.
            trayIcon.DoubleClick += async (s, e) =>
            {
                await CheckAndUpdateStatusAsync();
            };
        }

        /// <summary>
        /// Checks the meeting, camera, and microphone status.
        /// If the state has changed, sends an HTTP POST and shows a balloon tip.
        /// </summary>
        private async Task CheckAndUpdateStatusAsync()
        {
            // Check for active meeting by retrieving active meeting app names.
            string meetingApps = MeetingHelper.GetActiveMeetingApps(); // Comma-separated names or empty.
            bool meetingActive = !string.IsNullOrWhiteSpace(meetingApps);

            // Check camera status.
            VideoCaptureDevice myCamera = cameraHelper.GetCameraByHardwareId(cameraHardwareId);
            bool cameraInUse = cameraHelper.IsCameraInUse(myCamera);

            // Check microphone status.
            bool micInUse = audioHelper.IsMicrophoneInUse();

            // Compute the preset (ps) value based on the conditions.
            int ps = meetingActive
                ? (cameraInUse ? activeMeetingWithCameraPreset : activeMeetingWithoutCameraPreset)
                : (cameraInUse ? noMeetingWithCameraPreset : noMeetingWithoutCameraPreset);

            // Build the current status.
            DeviceStatus currentStatus = new DeviceStatus
            {
                MeetingActive = meetingActive,
                MeetingApps = meetingApps,
                CameraInUse = cameraInUse,
                MicInUse = micInUse,
                Ps = ps
            };

            // If the status changed, send the update.
            if (lastStatus == null || !lastStatus.Equals(currentStatus))
            {
                lastStatus = currentStatus;
                await SendStatusAsync(ps);

                // Build balloon tip message.
                List<string> statusParts = new List<string>();
                if (meetingActive)
                    statusParts.Add($"On meeting: {meetingApps}");
                if (cameraInUse)
                    statusParts.Add("Camera in use");
                if (micInUse)
                    statusParts.Add("Microphone in use");
                if (statusParts.Count == 0)
                    statusParts.Add("No active meeting, camera, or mic");

                string balloonMessage = string.Join(", ", statusParts);
                trayIcon.ShowBalloonTip(3000, "Status Change", balloonMessage, ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// Sends an HTTP POST request with a JSON payload containing the preset (ps) value.
        /// </summary>
        private async Task SendStatusAsync(int ps)
        {
            string payload = $"{{ \"ps\": {ps} }}";

            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                try
                {
                    await client.PostAsync(postUrl, content);
                }
                catch (Exception ex)
                {
                    trayIcon.ShowBalloonTip(3000, "HTTP Error", $"Error sending POST: {ex.Message}", ToolTipIcon.Error);
                }
            }
        }

        /// <summary>
        /// Sends an HTTP GET request to the specified URL.
        /// </summary>
        private async Task SendGetRequestAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    await client.GetAsync(url);
                }
                catch (Exception ex)
                {
                    trayIcon.ShowBalloonTip(3000, "HTTP GET Error", $"Error sending GET: {ex.Message}", ToolTipIcon.Error);
                }
            }
        }
    }

    /// <summary>
    /// Represents the current device status.
    /// </summary>
    public class DeviceStatus
    {
        public bool MeetingActive { get; set; }
        public string MeetingApps { get; set; }
        public bool CameraInUse { get; set; }
        public bool MicInUse { get; set; }
        public int Ps { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is DeviceStatus other)
            {
                return MeetingActive == other.MeetingActive &&
                       string.Equals(MeetingApps, other.MeetingApps, StringComparison.OrdinalIgnoreCase) &&
                       CameraInUse == other.CameraInUse &&
                       MicInUse == other.MicInUse &&
                       Ps == other.Ps;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (MeetingActive, MeetingApps, CameraInUse, MicInUse, Ps).GetHashCode();
        }
    }

    /// <summary>
    /// Helper class for camera operations.
    /// </summary>
    public class CameraHelper
    {
        public VideoCaptureDevice GetCameraByHardwareId(string hwIdFragment)
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
            {
                if (device.MonikerString.IndexOf(hwIdFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return new VideoCaptureDevice(device.MonikerString);
            }
            return null;
        }

        public bool IsCameraInUse(VideoCaptureDevice videoDevice)
        {
            if (videoDevice == null)
                return true;

            bool frameReceived = false;
            NewFrameEventHandler frameHandler = (sender, eventArgs) =>
            {
                frameReceived = true;
            };

            videoDevice.NewFrame += frameHandler;
            try
            {
                videoDevice.Start();
                int totalWaitTime = 2000; // Wait up to 2 seconds.
                int interval = 100;
                int waited = 0;
                while (waited < totalWaitTime && !frameReceived)
                {
                    Thread.Sleep(interval);
                    waited += interval;
                }
                videoDevice.SignalToStop();
                videoDevice.WaitForStop();
            }
            catch (Exception)
            {
                return true;
            }
            finally
            {
                videoDevice.NewFrame -= frameHandler;
            }
            return !frameReceived;
        }
    }

    /// <summary>
    /// Helper class for microphone operations using audio meter polling.
    /// </summary>
    public class AudioHelper
    {
        public bool IsMicrophoneInUse(float threshold = 0.01f, int pollTimeMs = 2000)
        {
            try
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice mic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < pollTimeMs)
                {
                    float peak = mic.AudioMeterInformation.MasterPeakValue;
                    if (peak > threshold)
                        return true;
                    Thread.Sleep(100);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Helper class to check for active meeting applications (e.g. Teams, Zoom) by inspecting window titles.
    /// </summary>
    public static class MeetingHelper
    {
        public static string GetActiveMeetingApps()
        {
            List<string> activeApps = new List<string>();
            string[] targetProcesses = { "Teams", "Zoom" };
            string[] meetingKeywords = { "Meeting", "Call" };

            foreach (string procName in targetProcesses)
            {
                Process[] processes = Process.GetProcessesByName(procName);
                foreach (Process proc in processes)
                {
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        string title = proc.MainWindowTitle;
                        if (!string.IsNullOrEmpty(title))
                        {
                            foreach (string keyword in meetingKeywords)
                            {
                                if (title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (!activeApps.Contains(procName))
                                        activeApps.Add(procName);
                                }
                            }
                        }
                    }
                }
            }
            return string.Join(", ", activeApps);
        }

        public static bool IsInActiveMeeting()
        {
            return !string.IsNullOrWhiteSpace(GetActiveMeetingApps());
        }
    }
}
