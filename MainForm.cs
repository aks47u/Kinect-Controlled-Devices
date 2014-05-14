using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using GestureFramework;
using WindowsInput;
using NKH.MindSqualls;
using System.IO.Ports;
using System.Diagnostics;
using System.ComponentModel;

namespace WindowsFormsKinectNXT
{
    public partial class MainForm : Form
    {
        private KinectSensorChooser _chooser;
        private Bitmap _bitmap;

        private GestureMap _gestureMap;
        private Dictionary<int, GestureMapState> _gestureMaps;
        private string GestureFileName;// = "gestures.xml";

        public int PlayerId;

        public NXTBlock nxtBlock;

        public KeyboardController car;
        private byte comport;

        public MainForm()
        {
            nxtBlock = new NXTBlock(0);
            // car = new KeyboardController();
            GestureFileName = nxtBlock.GestureFileName();

            InitializeComponent();
            GetDevices(false);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            // First Load the XML File that contains the application configuration
            _gestureMap = new GestureMap();
            _gestureMap.LoadGesturesFromXml(GestureFileName);

            _chooser = new KinectSensorChooser();
            _chooser.KinectChanged += ChooserSensorChanged;
            _chooser.Start();

            // Instantiate the in memory representation of the gesture state for each player
            _gestureMaps = new Dictionary<int, GestureMapState>();
        }

        void ChooserSensorChanged(object sender, KinectChangedEventArgs e)
        {
            var old = e.OldSensor;
            StopKinect(old);

            var newsensor = e.NewSensor;
            if (newsensor == null)
            {
                return;
            }

            newsensor.SkeletonStream.Enable();
            newsensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            newsensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            newsensor.AllFramesReady += SensorAllFramesReady;

            try
            {
                newsensor.Start();
                rtbMessages.Text = "Kinect Started" + "\r";
                toolStripStatusKinect.Text = "Online";
                toolStripStatusKinect.ForeColor = Color.Green;
            }
            catch (System.IO.IOException)
            {
                rtbMessages.Text = "Kinect Not Started" + "\r";
                // Maybe another app is using Kinect
                _chooser.TryResolveConflict();
            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();
                    sensor.AudioSource.Stop();
                }
            }
        }

        void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (_gestureMap.MessagesWaiting)
            {
                foreach (var msg in _gestureMap.Messages)
                {
                    rtbMessages.AppendText(msg + "\r");
                }
                rtbMessages.ScrollToCaret();
                _gestureMap.MessagesWaiting = false;
            }

            SensorDepthFrameReady(e);
            SensorSkeletonFrameReady(e);
            video.Image = _bitmap;
        }

        void SensorSkeletonFrameReady(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return;
                }

                var allSkeletons = new Skeleton[skeletonFrameData.SkeletonArrayLength];
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                foreach (Skeleton sd in allSkeletons)
                {
                    // If this skeleton is no longer being tracked, skip it
                    if (sd.TrackingState != SkeletonTrackingState.Tracked)
                    {
                        continue;
                    }

                    // If there is not already a gesture state map for this skeleton, then create one
                    if (!_gestureMaps.ContainsKey(sd.TrackingId))
                    {
                        var mapstate = new GestureMapState(_gestureMap);
                        _gestureMaps.Add(sd.TrackingId, mapstate);
                    }

                    var keycode = VirtualKeyCode.NONAME;
                    try
                    {
                        keycode = _gestureMaps[sd.TrackingId].Evaluate(sd, false, _bitmap.Width, _bitmap.Height);
                        GetWaitingMessages(_gestureMaps);
                    }
                    catch(Exception err)
                    {
                        ;
                    }

                    if (keycode != VirtualKeyCode.NONAME)
                    {
                        rtbMessages.AppendText("Gesture accepted from player " + sd.TrackingId + "\r");
                        rtbMessages.ScrollToCaret();
                        if (this.nxtBlock.IsConnected())
                        {
                            rtbMessages.AppendText("Command passed to System: " + keycode + "\r");
                            rtbMessages.ScrollToCaret();

                            if (this.nxtBlock.IsConnected())
                            {
                                this.nxtBlock.PerformKeyCodeAction(keycode);
                            }
                        }

                        _gestureMaps[sd.TrackingId].ResetAll(sd);
                    }

                    // This break prevents multiple player data from being confused during evaluation.
                    // If one were going to dis-allow multiple players, this trackingId would facilitate
                    // that feat.
                    PlayerId = sd.TrackingId;

                    if (_bitmap != null)
                    {
                        _bitmap = AddSkeletonToDepthBitmap(sd, _bitmap, true);
                    }
                }
            }
        }

        /// <summary>
        /// This method draws the joint dots and skeleton on the depth image of the depth display
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="bitmap"></param>
        /// <param name="isActive"> </param>
        /// <returns></returns>
        private Bitmap AddSkeletonToDepthBitmap(Skeleton skeleton, Bitmap bitmap, bool isActive)
        {
            Pen pen;

            var gobject = Graphics.FromImage(bitmap);

            if (isActive)
            {
                pen = new Pen(Color.Green, 5);
            }
            else
            {
                pen = new Pen(Color.DeepSkyBlue, 5);
            }

            var head = CalculateJointPosition(bitmap, skeleton.Joints[JointType.Head]);
            var neck = CalculateJointPosition(bitmap, skeleton.Joints[JointType.ShoulderCenter]);
            var rightshoulder = CalculateJointPosition(bitmap, skeleton.Joints[JointType.ShoulderRight]);
            var leftshoulder = CalculateJointPosition(bitmap, skeleton.Joints[JointType.ShoulderLeft]);
            var rightelbow = CalculateJointPosition(bitmap, skeleton.Joints[JointType.ElbowRight]);
            var leftelbow = CalculateJointPosition(bitmap, skeleton.Joints[JointType.ElbowLeft]);
            var rightwrist = CalculateJointPosition(bitmap, skeleton.Joints[JointType.WristRight]);
            var leftwrist = CalculateJointPosition(bitmap, skeleton.Joints[JointType.WristLeft]);

            //var spine = CalculateJointPosition(bitmap, skeleton.Joints[JointType.Spine]);
            var hipcenter = CalculateJointPosition(bitmap, skeleton.Joints[JointType.HipCenter]);
            var hipleft = CalculateJointPosition(bitmap, skeleton.Joints[JointType.HipLeft]);
            var hipright = CalculateJointPosition(bitmap, skeleton.Joints[JointType.HipRight]);
            var kneeleft = CalculateJointPosition(bitmap, skeleton.Joints[JointType.KneeLeft]);
            var kneeright = CalculateJointPosition(bitmap, skeleton.Joints[JointType.KneeRight]);
            var ankleleft = CalculateJointPosition(bitmap, skeleton.Joints[JointType.AnkleLeft]);
            var ankleright = CalculateJointPosition(bitmap, skeleton.Joints[JointType.AnkleRight]);

            gobject.DrawEllipse(pen, new Rectangle((int)head.X - 25, (int)head.Y - 25, 50, 50));
            gobject.DrawEllipse(pen, new Rectangle((int)neck.X - 5, (int)neck.Y, 10, 10));
            gobject.DrawLine(pen, head.X, head.Y + 25, neck.X, neck.Y);

            gobject.DrawLine(pen, neck.X, neck.Y, rightshoulder.X, rightshoulder.Y);
            gobject.DrawLine(pen, neck.X, neck.Y, leftshoulder.X, leftshoulder.Y);
            gobject.DrawLine(pen, rightshoulder.X, rightshoulder.Y, rightelbow.X, rightelbow.Y);
            gobject.DrawLine(pen, leftshoulder.X, leftshoulder.Y, leftelbow.X, leftelbow.Y);

            gobject.DrawLine(pen, rightshoulder.X, rightshoulder.Y, hipcenter.X, hipcenter.Y);
            gobject.DrawLine(pen, leftshoulder.X, leftshoulder.Y, hipcenter.X, hipcenter.Y);

            gobject.DrawEllipse(pen, new Rectangle((int)rightwrist.X - 10, (int)rightwrist.Y - 10, 20, 20));
            gobject.DrawLine(pen, rightelbow.X, rightelbow.Y, rightwrist.X, rightwrist.Y);
            gobject.DrawEllipse(pen, new Rectangle((int)leftwrist.X - 10, (int)leftwrist.Y - 10, 20, 20));
            gobject.DrawLine(pen, leftelbow.X, leftelbow.Y, leftwrist.X, leftwrist.Y);

            gobject.DrawLine(pen, hipcenter.X, hipcenter.Y, hipleft.X, hipleft.Y);
            gobject.DrawLine(pen, hipcenter.X, hipcenter.Y, hipright.X, hipright.Y);
            gobject.DrawLine(pen, hipleft.X, hipleft.Y, kneeleft.X, kneeleft.Y);
            gobject.DrawLine(pen, hipright.X, hipright.Y, kneeright.X, kneeright.Y);
            gobject.DrawLine(pen, kneeright.X, kneeright.Y, ankleright.X, ankleright.Y);
            gobject.DrawLine(pen, kneeleft.X, kneeleft.Y, ankleleft.X, ankleleft.Y);

            return bitmap;
        }

        /// <summary>
        /// This method turns a skeleton joint position vector into one that is scaled to the depth image
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="joint"></param>
        /// <returns></returns>
        protected SkeletonPoint CalculateJointPosition(Bitmap bitmap, Joint joint)
        {
            var jointx = joint.Position.X;
            var jointy = joint.Position.Y;
            var jointz = joint.Position.Z;

            if (jointz < 1)
            {
                jointz = 1;
            }

            var jointnormx = jointx / (jointz * 1.1f);
            var jointnormy = -(jointy / jointz * 1.1f);
            var point = new SkeletonPoint();
            point.X = (jointnormx + 0.5f) * bitmap.Width;
            point.Y = (jointnormy + 0.5f) * bitmap.Height;
            return point;
        }

        protected void GetWaitingMessages(Dictionary<int, GestureMapState> gestureMapDict)
        {
            foreach (var map in _gestureMaps)
            {
                if (map.Value.MessagesWaiting)
                {
                    foreach (var msg in map.Value.Messages)
                    {
                        rtbMessages.AppendText(msg + "\r");
                        rtbMessages.ScrollToCaret();
                    }
                    map.Value.Messages.Clear();
                    map.Value.MessagesWaiting = false;
                }
            }
        }

        void SensorDepthFrameReady(AllFramesReadyEventArgs e)
        {
            // If the window is displayed, show the depth buffer image
            if (WindowState != FormWindowState.Minimized)
            {
                using (var frame = e.OpenDepthImageFrame())
                {
                    _bitmap = CreateBitMapFromDepthFrame(frame);
                }
            }
        }

        private Bitmap CreateBitMapFromDepthFrame(DepthImageFrame frame)
        {
            if (frame != null)
            {
                var bitmapImage = new Bitmap(frame.Width, frame.Height, PixelFormat.Format16bppRgb565);
                var g = Graphics.FromImage(bitmapImage);
                g.Clear(Color.FromArgb(0, 34, 68));
                return bitmapImage;
            }
            return null;
        }

        private void connectToDevice_Click(object sender, EventArgs e)
        {
            Button btn=(Button)sender;

            if (!this.nxtBlock.IsConnected())
            {
                try
                {
                    comport = GetPortFromComboBox();

                    if (comport != 0)
                    {
                        this.backgroundWorker1.RunWorkerAsync();
                    }
                }
                catch(Exception err)
                {
                    rtbMessages.AppendText("Unable to connect, Is the brick switched on." + "\r");
                    rtbMessages.ScrollToCaret();
                }
            }
            else
            {
                batteryGauge.Value = 0;
                this.batteryCheckTimer.Stop();

                this.nxtBlock.Disconnect();

                btn.Text = "Connect";
                rtbMessages.AppendText("Disconnected from Brick" + "\r");
                rtbMessages.ScrollToCaret();
                toolStripStatusNXTBrick.Text = "Offline";
                toolStripStatusNXTBrick.ForeColor = Color.Red;
            }
        }

        public void GetDevices(bool scan)
        {
            try
            {  //add device n os
                if (scan)
                {
                    Process p = Process.Start("C:\\Windows\\System32\\DevicePairingWizard.exe");//here write ur own windows drive
                    while (true)
                    {
                        if (p.HasExited) //determine if process end
                        {
                            break;
                        }
                    }
                }
                //generate busy com ort list

                List<String> tList = new List<String>();
                deviceComboBox.Items.Clear();
                foreach (string s in SerialPort.GetPortNames())
                {
                    tList.Add(s);
                }
                tList.Sort();
                deviceComboBox.Items.Add("NO PORT");
                deviceComboBox.Items.AddRange(tList.ToArray());
                deviceComboBox.SelectedIndex = 0;
                //richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "COMPORT GENERATED";
            }
            catch (Exception err)
            {
                if (DialogResult.Retry == MessageBox.Show("CANT FIND UR ADDED DEVICE..", "Problem occured", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error))
                {
                    GetDevices(false);
                }
            }
        }

        byte GetPortFromComboBox()
        {
            try
            {
                if (deviceComboBox.SelectedIndex != 0)
                {
                    string port = deviceComboBox.SelectedItem.ToString();

                    return byte.Parse(port.Replace("COM", ""));
                }
                else
                {
                    MessageBox.Show("Please select com port", "Missing port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return 0;
            }
            catch (Exception a)
            {
                if (DialogResult.Retry == MessageBox.Show(a.Message, "problem occured", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning))
                {
                    GetPortFromComboBox();
                }

                return 0;
            }
        }

        private void scanForDevices_Click(object sender, EventArgs e)
        {
            GetDevices(false);
        }

        private void addDevice_Click(object sender, EventArgs e)
        {
            GetDevices(true);
        }


        private void establishConnectionToDevice(object sender, DoWorkEventArgs e)
        {
            bool res = this.nxtBlock.ConnectToDevice(comport);

            if (res)
            {
                this.nxtBlock.setSpeed(50);
            }
        }

        private void onConnectionEstablished(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                rtbMessages.AppendText("Connecting to Brick was cancelled!" + "\r");
                rtbMessages.ScrollToCaret();
            }

            else if (!(e.Error == null))
            {
                rtbMessages.AppendText("Error: " + e.Error.Message + "\r");
                rtbMessages.ScrollToCaret();
            }

            else
            {
                if (this.nxtBlock.IsConnected())
                {
                    this.connectToDeviceButton.Text = "Disconnect";
                    rtbMessages.AppendText("Connected to Brick" + "\r");
                    rtbMessages.ScrollToCaret();
                    toolStripStatusNXTBrick.Text = "Online";
                    toolStripStatusNXTBrick.ForeColor = Color.Green;

                    UpdateBatteryLevel(null,null);
                    this.batteryCheckTimer.Start();
                }
                else
                {
                    MessageBox.Show("Is the device switched on?\rAre you using the correct port?", "Unable to connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateBatteryLevel(object sender, EventArgs e)
        {
            if (this.nxtBlock.IsConnected())
            {
                batteryGauge.Value = this.nxtBlock.CheckBatteryLevel();
                rtbMessages.AppendText("Battery level = " + batteryGauge.Value + "\r");
                rtbMessages.ScrollToCaret();
            }
            else
            {
                batteryGauge.Value = 0;
                this.batteryCheckTimer.Stop();
            }
        }
    }
}
