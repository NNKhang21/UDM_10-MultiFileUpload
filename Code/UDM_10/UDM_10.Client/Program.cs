using System;
using System.Windows.Forms;

namespace UDM_10.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(new MainForm());
        }
    }
}