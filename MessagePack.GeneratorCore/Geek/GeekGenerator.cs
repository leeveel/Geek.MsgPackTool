using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MessagePackCompiler
{
    public class GeekGenerator
    {

        public const string KeyAttribute = "MessagePack.KeyAttribute";
        public const string SerializeAttribute = "Geek.Server.Proto.SerializeAttribute";
        public const string BaseMessage = "Geek.Server.BaseMessage";

        public static GeekGenerator Singleton = new GeekGenerator();
        //sub - parent
        private readonly Dictionary<string, ClassTemplate> clsDid = new Dictionary<string, ClassTemplate>();
        private readonly Dictionary<int, string> sidDic = new Dictionary<int, string>();
        private readonly Dictionary<string, ClassDeclarationSyntax> clsSyntaxDic = new Dictionary<string, ClassDeclarationSyntax>();
        private readonly PolymorphicInfoFactory polymorphicInfos = new PolymorphicInfoFactory();
        private readonly MsgFactory msgFactory = new MsgFactory();
        private readonly List<ClassTemplate> clsTemps = new List<ClassTemplate>();

        public void GenCode(Compilation compilation, INamedTypeSymbol[] targetTypes, string output, string clientOutput, bool autoNew)
        {
            GetAllClassSyntax(compilation);

            ClassTemplate baseMsg = new ClassTemplate();
            baseMsg.fullname = BaseMessage;
            clsDid.Add(BaseMessage, baseMsg);

            foreach (var type in targetTypes)
            {
                var member = type.GetMembers("BarType").FirstOrDefault();

                ClassTemplate clsTemp = new ClassTemplate();
                clsTemp.name = type.Name;
                clsTemp.fullname = type.ToString();
                clsTemp.autonew = autoNew;

                //class syntax
                ClassDeclarationSyntax clsSyntas = clsSyntaxDic[clsTemp.fullname];
                CompilationUnitSyntax root = clsSyntas.SyntaxTree.GetCompilationUnitRoot();
                foreach (UsingDirectiveSyntax element in root.Usings)
                {
                    clsTemp.usings.Add(element.Name.ToString());
                }

                var atts = type.GetAttributes();
                foreach (var att in atts)
                {
                    if (att.ToString().Contains(SerializeAttribute))
                    {
                        clsTemp.sid = (int)att.ConstructorArguments[0].Value;
                        clsTemp.ismsg = (bool)att.ConstructorArguments[1].Value;
                    }
                }

                //检查sid是否重复
                if (!sidDic.ContainsKey(clsTemp.sid))
                {
                    sidDic.Add(clsTemp.sid, clsTemp.fullname);
                }
                else
                {
                    throw new Exception($"sid exists duplicate key: {clsTemp.fullname}---{sidDic[clsTemp.sid]}");
                }


                //处理多态关系
                //TODO：检查父类也必须标记了[MessagePackObject]
                if (type.BaseType.ToString().Equals("object"))
                {
                    if (clsTemp.ismsg)
                    {
                        PolymorphicInfo info = new PolymorphicInfo();
                        info.basename = BaseMessage;
                        info.subname = clsTemp.fullname;
                        info.subsid = clsTemp.sid;
                        clsTemp.super = BaseMessage;
                        polymorphicInfos.infos.Add(info);
                    }
                }
                else
                {
                    clsTemp.super = type.BaseType.ToString();
                    PolymorphicInfo info = new PolymorphicInfo();
                    info.basename = clsTemp.super;
                    info.subname = clsTemp.fullname;
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
                        if (att.ToString().Contains(KeyAttribute))
                        {
                            ftemp.order = (int)att.ConstructorArguments[0].Value;
                            clsTemp.sfieldcount++;
                            if (ftemp.orderdic.ContainsKey(ftemp.order))
                            {
                                throw new Exception($"sid exists duplicate key {ftemp.order} : {clsTemp.fullname}");
                            }
                            else
                            {
                                ftemp.orderdic.Add(ftemp.order, GetPropertyCode(ftemp.name, clsSyntaxDic[clsTemp.fullname]));
                            }
                        }
                        else
                        {
                            ftemp.ignore = true;
                            ftemp.ignorepropcode = GetPropertyCode(ftemp.name, clsSyntaxDic[clsTemp.fullname]);
                        }
                    }
                    clsTemp.fields.Add(ftemp);
                }

                //以order为标准对字段进行重排序
                clsTemp.fields.OrderBy(f => f.order);

                clsTemps.Add(clsTemp);
                clsDid.Add(clsTemp.fullname, clsTemp);

                MsgInfo msg = new MsgInfo();
                msg.sid = clsTemp.sid;
                msg.typename = clsTemp.fullname;
                msgFactory.msgs.Add(msg);
            }

            //检查key是否合法
            CheckOrder(clsTemps);

            //重新分配order id
            ReAllocateOrder(clsTemps);

            //清除并创建目录
            output.CreateDirectory();
            if (!output.Equals("no"))
                output.CreateDirectory();
            if (!clientOutput.Equals("no"))
                clientOutput.CreateDirectory();

            //MsgFactory
            Template msgTemp = Template.Parse(File.ReadAllText("Geek/MsgFactory.liquid"));
            var msgstr = msgTemp.Render(msgFactory);

            if (!output.Equals("no"))
                File.WriteAllText($"{output}/MsgFactory.cs", msgstr);
            if (!clientOutput.Equals("no"))
                File.WriteAllText($"{clientOutput}/MsgFactory.cs", msgstr);


            //生成多态注册器
            Template registerTemp = Template.Parse(File.ReadAllText("Geek/Register.liquid"));
            var rstr = registerTemp.Render(polymorphicInfos);

            if (!output.Equals("no"))
                File.WriteAllText($"{output}/PolymorphicRegisterGen.cs", rstr);
            if (!clientOutput.Equals("no"))
                File.WriteAllText($"{clientOutput}/PolymorphicRegisterGen.cs", rstr);

            Template template = Template.Parse(File.ReadAllText("Geek/Proto.liquid"));
            foreach (var cls in clsTemps)
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
        /// </summary>
        /// <returns></returns>
        public void CheckOrder(List<ClassTemplate> list)
        {
            foreach (var cls in list)
            {
                int temp = 0;
                bool first = true;
                for (int i = 0; i < cls.fields.Count; i++)
                {
                    if (cls.fields[i].ignore)
                    {
                        continue;
                    }
                    else
                    {
                        if (first)
                        {
                            first = false;
                            temp = cls.fields[i].order;
                            if (temp != 0)
                                throw new Exception($"key must start from zero : {cls.fullname}");
                        }
                        else if (++temp != cls.fields[i].order)
                        {
                            foreach (var f in cls.fields)
                            {
                                Console.WriteLine(f.order);
                            }
                            throw new Exception($"keys must be sequenece : {cls.fullname}");
                        }
                    }
                }
            }
        }


        public void ReAllocateOrder(List<ClassTemplate> list)
        {
            foreach (var cls in list)
            {
                ReAllocateOrder(cls);
            }
        }

        public void ReAllocateOrder(ClassTemplate cls)
        {
            int order = GetStartOrder(cls);
            foreach (var field in cls.fields)
            {
                if (field.ignore)
                    continue;
                //field.attributes.Add($"Key({order++})");
                //var after = field.orderdic[field.order].Replace($"Key({field.order})", $"Key({order++})");
                //field.orderdic[field.order] = after;
                var before = field.orderdic[field.order];
                string extract = Regex.Match(before, string.Format("{0}.+{1}", "Key", "\\)")).Value;
                field.orderdic[field.order] = before.Replace(extract, $"Key({order++})");
            }
        }


        public int GetStartOrder(ClassTemplate cls)
        {
            if (cls.fullname == BaseMessage)
                return 1;
            if (string.IsNullOrEmpty(cls.super)) //userinfo, etc...
                return 0;

            int order = 0;
            if (!string.IsNullOrEmpty(cls.super))
            {
                clsDid.TryGetValue(cls.super, out ClassTemplate parent);
                if (parent == null)
                    throw new Exception($"can not find base class:{cls.super}");
                order += GetStartOrder(parent);
            }
            else
            {
                order += cls.sfieldcount;
            }
            return order;
        }


        public void GetAllClassSyntax(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var cls in classes)
                {
                    clsSyntaxDic.Add(cls.GetFullName(), cls);
                }
            }
        }


        public string GetPropertyCode(string name, ClassDeclarationSyntax clsSyntax)
        {
            var props = clsSyntax.ChildNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in props)
            {
                if (prop.Identifier.ToString() == name)
                    return prop.ToFullString();
            }
            throw new Exception($"can not find property [{name}] in {clsSyntax.GetFullName()}");
        }

    }
}
