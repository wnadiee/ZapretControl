using System;
using System.IO;
using System.Text;
namespace ZapretControl
{
    internal static class ArgDump
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                string output = Environment.GetEnvironmentVariable("ZC_ARGDUMP_OUT");
                if (!string.IsNullOrEmpty(output))
                    File.WriteAllText(output, Environment.CommandLine, new UTF8Encoding(false));
            }
            catch { }
        }
    }
}
