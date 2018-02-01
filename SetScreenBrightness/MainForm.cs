using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SetScreenBrightness.Properties;

namespace SetScreenBrightness
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
            //Get Current Gamma
            defaultgamma = GetGamma();
            ChangeGamma(defaultgamma);
            //Brightness
            bLevels = GetBrightnessLevels(); //get the level array for this system
            if (bLevels.Length == 0) //"WmiMonitorBrightness" is not supported by the system
            {
                groupBox2.Enabled = false;
            }
            else
            {
                trackBar2.Minimum = 0;
                trackBar2.Maximum = bLevels.Length - 1;
                numericUpDown2.Minimum = 0;
                numericUpDown2.Maximum = bLevels.Length - 1;
                trackBar2.Update();
                trackBar2.Refresh();
                //Get Current Brightness
                defaultbrightness = GetCurrentBrightness();
                ChangeBrightness(defaultbrightness, true);
                brightnessThreadTimer = new System.Threading.Timer(SyncWithSystemBrightness, null, 0, 10);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            brightnessThreadTimer?.Dispose();
        }

        #region gamma

        [DllImport(@"user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(@"gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport(@"gdi32.dll")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

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

        private static int GetGamma()
        {
            var ramp = new RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };
            GetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp);
            for (var i = 1; i < 256; ++i)
            {
                if (ramp.Red[i] != 65535)
                {
                    var gamma = ramp.Red[i] / i - 128;
                    if (gamma <= 255 && gamma >= 0)
                    {
                        return gamma;
                    }
                }
            }
            return 128;
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

        private int defaultgamma;

        private void ChangeGamma(int gamma,bool sync = false)
        {
            try
            {
                trackBar1.Value = gamma;
                numericUpDown1.Value = gamma;
                if (!sync)
                {
                    SetGamma(gamma);
                }
            }
            catch
            {
                //
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (trackBar1.Focused)
            {
                ChangeGamma(trackBar1.Value);
            }
        }
        
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Focused)
            {
                ChangeGamma(Convert.ToInt32(numericUpDown1.Value));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeGamma(128);
        }
        
        private void button5_Click(object sender, EventArgs e)
        {
            ChangeGamma(trackBar1.Minimum);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ChangeGamma(trackBar1.Maximum);
        }

        #endregion

        #region Brightness

        private byte[] bLevels; //array of valid level values

        private int defaultbrightness;

        private System.Threading.Timer brightnessThreadTimer;

        private int GetCurrentBrightness()
        {
            int iBrightness = GetBrightness(); //get the actual value of brightness
            int i = Array.IndexOf(bLevels, (byte)iBrightness);
            if (i < 0)
                i = 0;
            return i;
        }

        private void ChangeBrightness(int i,bool sync = false)
        {
            try
            {
                trackBar2.Value = i;
                numericUpDown2.Value = i;
                if (!sync)
                {
                    SetBrightness(bLevels[i]);
                }
            }
            catch
            {
                //
            }
        }
        
        private void SyncWithSystemBrightness(object state)
        {
            if (!trackBar2.Focused && !numericUpDown2.Focused)
            {
                ChangeBrightness(GetCurrentBrightness(),true);
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
                MessageBox.Show(@"你的系统不支持亮度控制或者没用管理员权限运行23333",@"提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
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
        
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (trackBar2.Focused)
            {
                ChangeBrightness(trackBar2.Value);
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown2.Focused)
            {
                ChangeBrightness(Convert.ToInt32(numericUpDown2.Value));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ChangeBrightness(defaultbrightness);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ChangeBrightness(trackBar2.Minimum);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChangeBrightness(trackBar2.Maximum);
        }

        #endregion
    }
}