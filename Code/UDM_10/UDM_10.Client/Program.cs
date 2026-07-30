using UDM_10.Client.Shared.Config;
using UDM_10.Client.Server;

namespace UDM_10.Client
{
  
        internal static class Program
        {
            [STAThread]
        static void Main()
        {
            ServerConfig.Load();

            ClientConfig.Load();

            Logger.Init();

            ApplicationConfiguration.Initialize();

            Application.Run(new MainForm());
        }
    }
    }