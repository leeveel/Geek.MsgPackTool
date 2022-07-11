using NLog;
using NLog.Config;
using System.Text.RegularExpressions;

namespace Geek.MsgPackTool
{
    internal class Program
    {
        static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("Configs/NLog.config");
            LOGGER.Info("Geek.MsgPackTool start....");

            //初始化配置信息
            if (!Setting.Init())
            {
                Console.WriteLine("----配置错误，启动失败----");
                return;
            }

            Console.WriteLine("请按需输入指令:");
            Console.WriteLine("1.导出服务器");
            Console.WriteLine("2.导出客户端");
            Console.WriteLine("3.导出服务器+客户端");
            while (true)
            {
                var key = Console.ReadKey().KeyChar;
                Console.WriteLine("你输入了:" + key.ToString());
                switch (key)
                {
                    case '1':
                        _ = Gen(1);
                        break;
                    case '2':
                        _ = Gen(2);
                        break;
                    case '3':
                        _ = Gen(3);
                        break;
                    default:
                        Console.WriteLine("输入指令错误");
                        break;
                }
            }
        }

        public static bool invokingMpc = false;
        private static async Task Gen(int model)
        {
            MpcArgument mpcArgument = new MpcArgument();
            //mpcArgument.Input = @"D:\workspace\common\Tools\MsgPackTool\Geek.Proto";
            //mpcArgument.Input = @"F:\github\leeveel\GeekServerMPC\GeekServer.Proto";
            //mpcArgument.Output = @"F:\github\leeveel\GeekServerMPC\GeekServer.Generate\Proto";
            mpcArgument.Input = Setting.ProjectPath;
            mpcArgument.AutoNew = Setting.AutoNew; 
            if (model == 1)
            {
                mpcArgument.ServerOutput = Setting.ServerOutPath;
                mpcArgument.Output = "no";
            }
            else if (model == 2)
            {
                mpcArgument.ServerOutput = "no";
                mpcArgument.Output = Setting.ClientOutPath;
            }
            else if (model == 3)
            {
                mpcArgument.ServerOutput = Setting.ServerOutPath;
                mpcArgument.Output = Setting.ClientOutPath;
            }
            else
            {
                Console.WriteLine("输入指令错误");
                return;
            }

            var commnadLineArguments = mpcArgument.ToString();
            Console.WriteLine("Generate MessagePack Files, command:" + commnadLineArguments);
            invokingMpc = true;
            try
            {
                var log = await ProcessHelper.InvokeProcessStartAsync("mpc", commnadLineArguments);
                if (log.Contains("System.Exception"))
                    log.WriteErrorLine();
                else
                    log.WriteSuccessLine();
            }
            finally
            {
                invokingMpc = false;
            }
        }

    }
}