using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace ZapretControl
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EnableModernTls();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (HasArg(args, "--selftest"))
            {
                Environment.Exit(Core.SelfTest());
                return;
            }

            string shot = ArgValue(args, "--shot");
            bool dev = HasArg(args, "--dev") || shot != null;

            if (!dev && !IsAdministrator())
            {
                try
                {
                    var start = new ProcessStartInfo(Application.ExecutablePath, "--elevated");
                    start.UseShellExecute = true;
                    start.Verb = "runas";
                    Process.Start(start);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Для управления службой zapret нужны права администратора.\n\n" + ex.Message,
                        "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if (shot != null)
            {
                MakeScreenshot(shot);
                return;
            }

            bool created;
            using (var mutex = new Mutex(true, "ZapretControlSingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("Zapret Control уже запущен.", "Zapret Control",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.Run(new MainForm(dev));
                GC.KeepAlive(mutex);
            }
        }

        private static void MakeScreenshot(string path)
        {
            var form = new MainForm(true);
            form.Show();
            for (int i = 0; i < 140; i++)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }

            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(path, ImageFormat.Png);
            }

            form.SetChecksExpanded(false);
            for (int i = 0; i < 20; i++)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }

            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(path + ".collapsed.png", ImageFormat.Png);
            }

            form.Dispose();
        }

        private static void EnableModernTls()
        {
            try
            {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
            }
            catch
            {
                try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; }
                catch { }
            }
        }

        private static bool HasArg(string[] args, string value)
        {
            return args != null && args.Any(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase));
        }

        private static string ArgValue(string[] args, string name)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static bool IsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
