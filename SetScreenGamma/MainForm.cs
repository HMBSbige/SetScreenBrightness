using System;
using System.Reflection;
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
            if (gamma <= 256 && gamma >= 1)
            {
                var ramp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };
                for (var i = 1; i < 256; i++)
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            Icon = Resources.p90;
            ChangeGamma(128);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ChangeGamma(Convert.ToInt32(numericUpDown1.Value));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeGamma(128);
        }
    }
}