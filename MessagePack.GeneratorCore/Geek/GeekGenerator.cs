using Microsoft.CodeAnalysis;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;

namespace MessagePackCompiler
{
    public class GeekGenerator
    {

        public const string SerializeAttribute = "Geek.Server.Proto.SerializeAttribute";
        public const string BaseMessage = "Geek.Server.BaseMessage";

        public static GeekGenerator Singleton = new GeekGenerator();
        //sub - parent
        public Dictionary<string, string> polymorphicDic = new Dictionary<string, string>();

        public void GenCode(INamedTypeSymbol[] targetTypes, string output, string clientOutput)
        {
            List<ClassTemplate> list = new List<ClassTemplate>();
            PolymorphicInfoFactory polymorphicInfos = new PolymorphicInfoFactory();

            foreach (var type in targetTypes)
            {
                ClassTemplate clsTemp = new ClassTemplate();
                clsTemp.name = type.Name;

                var atts = type.GetAttributes();
                foreach (var att in atts)
                {
                    if (att.ToString().Contains(SerializeAttribute))
                    {
                        clsTemp.sid = (int)att.ConstructorArguments[0].Value;
                        clsTemp.ismsg = (bool)att.ConstructorArguments[1].Value;
                    }
                }

                //处理多态关系
                //TODO：检查父类也必须标记了[MessagePackObject]
                if (type.BaseType.ToString().Equals("object"))
                {
                    if (clsTemp.ismsg)
                    {
                        PolymorphicInfo info = new PolymorphicInfo();
                        info.basename = BaseMessage;
                        info.subname = type.ToString();
                        info.subsid = clsTemp.sid;
                        clsTemp.super = $": {BaseMessage}";
                        polymorphicInfos.infos.Add(info);
                    }
                }
                else
                {
                    clsTemp.super = type.BaseType.ToString();
                    PolymorphicInfo info = new PolymorphicInfo();
                    info.basename = clsTemp.super; 
                    info.subname = type.ToString();
                    info.subsid = clsTemp.sid;
                    polymorphicInfos.infos.Add(info);
                }
               
                //命名空间
                clsTemp.space = type.ContainingNamespace.ToString();

                //属性
                var members = type.GetMembers().OfType<IPropertySymbol>();
                foreach (var m in members)
                {
                    FieldTemplate ftemp = new FieldTemplate();
                    ftemp.name = m.Name;
                    ftemp.clsname = m.Type.ToString();
                    //ftemp.clsname = m.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)

                    var matts = m.GetAttributes();
                    foreach (var att in matts)
                    {
                        ftemp.attributes.Add(att.ToString().Replace("MessagePack.KeyAttribute", "Key"));
                    }
                    clsTemp.fields.Add(ftemp);
                }

                list.Add(clsTemp);
            }

            //清除并创建目录
            output.CreateDirectory();

            //生成多态注册器
            Template registerTemp = Template.Parse(File.ReadAllText("Geek/Register.liquid"));
            var rstr = registerTemp.Render(polymorphicInfos);

            if (!output.Equals("no"))
                File.WriteAllText($"{output}/PolymorphicRegisterGen.cs", rstr);
            if (!clientOutput.Equals("no"))
                File.WriteAllText($"{clientOutput}/PolymorphicRegisterGen.cs", rstr);

            Template template = Template.Parse(File.ReadAllText("Geek/Proto.liquid"));
            foreach (var cls in list)
            {
                var str = template.Render(cls);
                if (!output.Equals("no"))
                    File.WriteAllText($"{output}/{cls.name}.cs", str);
                if (!clientOutput.Equals("no"))
                    File.WriteAllText($"{clientOutput}/{cls.name}.cs", str);
            }
        }

        /// <summary>
        /// 1.检查key id 是否连贯，重复
        /// 2.检查继承关系的key id是否连贯，重复
        /// 3.检查SID是否重复
        /// </summary>
        /// <returns></returns>
        public bool Check()
        {
            return true;
        }

        public void GenPolymorphicRegister()
        {
            
        }

    }
}
