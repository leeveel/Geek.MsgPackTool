using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;


public class Setting
{

    public string BaseMessageName { set; get; }

    /// <summary>
    /// Proto 工程路径
    /// </summary>
    public string ProjectPath { set; get; }

    /// <summary>
    /// 服务器代码导出路径
    /// </summary>
    public string ServerOutPath { set; get; }

    /// <summary>
    /// 客户端代码导出路径
    /// </summary>
    public string ClientOutPath { set; get; }

    /// <summary>
    /// TS代码导出路径
    /// </summary>
    public string TSOutPath { set; get; }

    /// <summary>
    /// 服务器是否使用代码生成的Resovler
    /// </summary>
    public bool GeneratedFirst { set; get; }

    public List<string> NoExportList { set; get; } = new List<string>();

    public static Setting Ins { get; private set; }

    public static bool Init()
    {
        var cfgPath = "Configs/config.json";
        if (File.Exists(cfgPath))
        {
            Ins = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(cfgPath));
            return true;
        }
        else
        {
            Console.WriteLine("服务器配置文件错误或不存在,启动失败!");
            return false;
        }
    }
}
