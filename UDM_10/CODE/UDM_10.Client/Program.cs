using UDM_10.Shared.Config;

namespace UDM_10.Client
{
    // NOTE (fix): trước đây Client Program.cs gọi cả ServerConfig.Load() và Logger.Init()
    // (là 2 thứ thuộc về Server) -> gây nhầm lẫn kiến trúc, và cũng là nguyên nhân gián tiếp
    // khiến Server/Client bị gộp chung 1 project (2 Main cùng lúc). Client giờ chỉ cần
    // load ClientConfig (IP/port của Server để kết nối tới) - đúng vai trò 1 tiến trình độc lập.
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ClientConfig.Load();

            ApplicationConfiguration.Initialize();

            Application.Run(new MainForm());
        }
    }
}
