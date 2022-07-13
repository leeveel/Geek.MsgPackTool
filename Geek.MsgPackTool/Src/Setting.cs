using System.Xml;

namespace Geek.MsgPackTool
{
    public static class Setting
    {
        /// <summary>
        /// Proto 工程路径
        /// </summary>
        public static string ProjectPath { set; get; }

        /// <summary>
        /// 服务器代码导出路径
        /// </summary>
        public static string ServerOutPath { private set; get; }

        /// <summary>
        /// 客户端代码导出路径
        /// </summary>
        public static string ClientOutPath { private set; get; }

        /// <summary>
        /// 服务器是否使用代码生成的Resovler
        /// </summary>
        public static bool GeneratedFirst { private set; get; }

        public static bool Init()
        {
            if (File.Exists("Configs/config.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("Configs/config.xml");
                XmlElement root = doc.DocumentElement;
                XmlNode listNodes = root.SelectNodes("/config").Item(0);
                foreach (XmlNode node in listNodes)
                {
                    switch (node.Name)
                    {
                        case "project-path":
                            ProjectPath = node.InnerText;
                            break;
                        case "server-out-path":
                            ServerOutPath = node.InnerText;
                            break;
                        case "client-out-path":
                            ClientOutPath = node.InnerText;
                            break;
                        case "gen-first":
                            GeneratedFirst = bool.Parse(node.InnerText);
                            break;
                    }
                }
                return true;
            }
            else
            {
                Console.WriteLine("服务器配置文件错误或不存在,启动失败!");
                return false;
            }
        }

    }
}
