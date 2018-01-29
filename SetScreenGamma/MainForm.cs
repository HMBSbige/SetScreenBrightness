using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SetScreenGamma.Properties;

namespace SetScreenGamma
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Icon = Resources.p90;//load form icon.
            ChangeGamma(128);//Set to default gamma, because i don't know how to get current system gamma.
            //Get Current Brightness
            bLevels = GetBrightnessLevels(); //get the level array for this system
            if (bLevels.Length == 0) //"WmiMonitorBrightness" is not supported by the system
            {
                groupBox2.Enabled = false;
            }
            else
            {
                trackBar2.TickFrequency = bLevels.Length; //adjust the trackbar ticks according the number of possible brightness levels
                trackBar2.Maximum = bLevels.Length - 1;
                numericUpDown2.Maximum = bLevels.Length - 1;
                trackBar2.Update();
                trackBar2.Refresh();
                CheckBrightness(out defaultbrightness);
            }
        }
        
        #region gamma

        [DllImport(@"user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(@"gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        private static void SetGamma(int gamma)
        {
            if (gamma <= 255 && gamma >= 0)
            {
                var ramp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };
                for (var i = 0; i < 256; ++i)
                {
                    var iArrayValue = i * (gamma + 128);

                    if (iArrayValue > 65535)
                        iArrayValue = 65535;
                    ramp.Red[i] = ramp.Blue[i] = ramp.Green[i] = (ushort)iArrayValue;
                }
                SetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp);
            }
        }

        private void ChangeGamma(int gamma)
        {
            try
            {
                SetGamma(gamma);
                trackBar1.Value = gamma;
                numericUpDown1.Value = gamma;
            }
            catch
            {
                //
            }
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            ChangeGamma(trackBar1.Value);
        }
        
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ChangeGamma(Convert.ToInt32(numericUpDown1.Value));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeGamma(128);
        }

        #endregion

        #region Brightness

        private byte[] bLevels; //array of valid level values

        private int defaultbrightness;

        private void CheckBrightness(out int brightness)
        {
            int iBrightness = GetBrightness(); //get the actual value of brightness
            int i = Array.IndexOf(bLevels, (byte)iBrightness);
            if (i < 0)
                i = 1;
            brightness = i;
            ChangeBrightness(brightness);
        }

        private void ChangeBrightness(int i)
        {
            try
            {
                trackBar2.Value = i;
                numericUpDown2.Value = i;
                SetBrightness(bLevels[i]);
            }
            catch
            {
                //
            }
        }

        //get the actual percentage of brightness
        private static int GetBrightness()
        {
            //define scope (namespace)
            var s = new ManagementScope(@"root\WMI");

            //define query
            var q = new SelectQuery(@"WmiMonitorBrightness");

            //output current brightness
            var mos = new ManagementObjectSearcher(s, q);

            var moc = mos.Get();

            //store result
            byte curBrightness = 0;
            foreach (var managementBaseObject in moc)
            {
                var o = (ManagementObject) managementBaseObject;
                curBrightness = (byte)o.GetPropertyValue(@"CurrentBrightness");
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();

            return curBrightness;
        }

        //array of valid brightness values in percent
        private static byte[] GetBrightnessLevels()
        {
            //define scope (namespace)
            var s = new ManagementScope(@"root\WMI");

            //define query
            var q = new SelectQuery(@"WmiMonitorBrightness");

            //output current brightness
            var mos = new ManagementObjectSearcher(s, q);
            var BrightnessLevels = new byte[0];

            try
            {
                var moc = mos.Get();

                //store result


                foreach (var managementBaseObject in moc)
                {
                    var o = (ManagementObject) managementBaseObject;
                    BrightnessLevels = (byte[])o.GetPropertyValue(@"Level");
                    break; //only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

            }
            catch (Exception)
            {
                MessageBox.Show(@"你的系统不支持亮度控制23333",@"提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }

            return BrightnessLevels;
        }

        private static void SetBrightness(byte targetBrightness)
        {
            //define scope (namespace)
            var s = new ManagementScope(@"root\WMI");

            //define query
            var q = new SelectQuery(@"WmiMonitorBrightnessMethods");

            //output current brightness
            var mos = new ManagementObjectSearcher(s, q);

            var moc = mos.Get();

            foreach (var managementBaseObject in moc)
            {
                var o = (ManagementObject) managementBaseObject;
                o.InvokeMethod(@"WmiSetBrightness", new object[]
                {
                    uint.MaxValue, targetBrightness
                }); //note the reversed order - won't work otherwise!
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();
        }

        #endregion

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            ChangeBrightness(trackBar2.Value);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            ChangeBrightness(Convert.ToInt32(numericUpDown2.Value));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ChangeBrightness(defaultbrightness);
        }
    }
}