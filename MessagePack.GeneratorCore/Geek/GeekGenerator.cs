using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;
using Standart.Hash.xxHash;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MessagePackCompiler
{
    public class GeekGenerator
    {

        public const string KeyAttribute = "MessagePack.KeyAttribute";
        public const string IgnoreAttribute = "MessagePack.IgnoreMemberAttribute";
        public static string BaseMessage = "Geek.Server.Message";

        public static GeekGenerator Singleton = new GeekGenerator();
        //sub - parent
        private readonly Dictionary<int, string> sidDic = new Dictionary<int, string>();
        private readonly Dictionary<string, BaseTypeDeclarationSyntax> clsSyntaxDic = new Dictionary<string, BaseTypeDeclarationSyntax>();
        private readonly PolymorphicInfoFactory polymorphicInfos = new PolymorphicInfoFactory();
        private readonly List<PolymorphicInfo> finalClassList = new List<PolymorphicInfo>();
        private readonly MsgFactory msgFactory = new MsgFactory();
        private readonly List<ClassTemplate> clsTemps = new List<ClassTemplate>();
        private readonly List<GeekEnumTemplate> enumTemps = new List<GeekEnumTemplate>();

        public void GenCode(Compilation compilation, INamedTypeSymbol[] targetTypes, string output, string clientOutput)
        {
            Console.WriteLine($"-----------GenCode start Count:{targetTypes.Length}------------");
            GetAllEnumSyntax(compilation);
            GetAllClassSyntax(compilation);

            foreach (var type in targetTypes)
            {
                ClassTemplate clsTemp = new ClassTemplate();
                clsTemp.name = type.Name;
                clsTemp.fullname = type.ToString();

                if (type.TypeKind == TypeKind.Enum)
                    clsTemp.typename = "enum";
                else if (type.TypeKind == TypeKind.Class)
                    clsTemp.typename = "class";
                else if (type.TypeKind == TypeKind.Struct)
                    clsTemp.typename = "struct";
                else
                    throw new Exception($"unknown type:{type.Name}.{type.TypeKind}");

                //class syntax
                BaseTypeDeclarationSyntax clsSyntas = clsSyntaxDic[clsTemp.fullname];
                CompilationUnitSyntax root = clsSyntas.SyntaxTree.GetCompilationUnitRoot();
                foreach (UsingDirectiveSyntax element in root.Usings)
                {
                    clsTemp.usings.Add(element.Name.ToString());
                }

                //通过类型名字计算唯一hash
                if (clsTemp.typename != "enum")
                {
                    //clsTemp.sid = (int)MurmurHash3.Hash32(, 666);
                    //var nameBytes = System.Text.Encoding.UTF8.GetBytes(clsTemp.fullname);
                    //clsTemp.sid = (int)xxHash32.ComputeHash(nameBytes, nameBytes.Length, (uint)27);
                    clsTemp.sid = (int)MurmurHash3.Hash(clsTemp.fullname, 27);
                }

                //检查sid是否重复
                if (!sidDic.ContainsKey(clsTemp.sid))
                    sidDic.Add(clsTemp.sid, clsTemp.fullname);
                else
                    throw new Exception($"sid exists duplicate key: {clsTemp.fullname}---{sidDic[clsTemp.sid]}");


                if (type.TypeKind == TypeKind.Class && type.BaseType != null)
                {
                    if (!type.BaseType.ToString().Equals("object"))
                    {
                        //注册子类多态信息
                        clsTemp.super = type.BaseType.ToString();
                        PolymorphicInfo info = new PolymorphicInfo();
                        info.basename = clsTemp.super;
                        info.subname = clsTemp.fullname;
                        info.subsid = clsTemp.sid;
                        polymorphicInfos.infos.Add(info);
                    }
                    else
                    {
                        //注册基类多态信息
                        PolymorphicInfo info = new PolymorphicInfo();
                        info.basename = clsTemp.fullname;
                        info.subname = clsTemp.fullname;
                        info.subsid = clsTemp.sid;
                        polymorphicInfos.infos.Add(info);
                        finalClassList.Add(info);
                    }
                }

                if (!string.IsNullOrEmpty(clsTemp.super))
                    clsTemp.ismsg = clsTemp.super.Equals(BaseMessage);

                //命名空间
                clsTemp.space = type.ContainingNamespace.ToString();

                //属性
                var members = type.GetMembers().OfType<IPropertySymbol>();
                foreach (var m in members)
                {
                    FieldTemplate ftemp = new FieldTemplate();
                    ftemp.name = m.Name;
                    ftemp.clsname = m.Type.ToString();
                    ftemp.propcode = GetPropertyCode(ftemp.name, clsSyntas);
                    clsTemp.fields.Add(ftemp);
                }
                clsTemps.Add(clsTemp);

                MsgInfo msg = new MsgInfo();
                msg.sid = clsTemp.sid;
                msg.typename = clsTemp.fullname;
                msgFactory.msgs.Add(msg);
            }

            //清除并创建目录
            output.CreateDirectory();
            if (!output.Equals("no"))
                output.CreateDirectory();
            if (!clientOutput.Equals("no"))
                clientOutput.CreateDirectory();

            //MsgFactory
            var fctx = new TemplateContext();
            fctx.LoopLimit = 0;
            var fsobj = new ScriptObject();
            fsobj.Import(msgFactory);
            fctx.PushGlobal(fsobj);
            Template msgTemp = Template.Parse(File.ReadAllText("Geek/MsgFactory.liquid"));
            var msgstr = msgTemp.Render(fctx);

            if (!output.Equals("no"))
                File.WriteAllText($"{output}/MsgFactory.cs", msgstr);
            if (!clientOutput.Equals("no"))
                File.WriteAllText($"{clientOutput}/MsgFactory.cs", msgstr);


            //生成多态注册器
            RemoveNoSubClass();
            var rctx = new TemplateContext();
            rctx.LoopLimit = 0;
            var rsobj = new ScriptObject();
            rsobj.Import(polymorphicInfos);
            rctx.PushGlobal(rsobj);
            Template registerTemp = Template.Parse(File.ReadAllText("Geek/Register.liquid"));
            var rstr = registerTemp.Render(rctx);

            if (!output.Equals("no"))
                File.WriteAllText($"{output}/PolymorphicRegisterGen.cs", rstr);
            if (!clientOutput.Equals("no"))
                File.WriteAllText($"{clientOutput}/PolymorphicRegisterGen.cs", rstr);

            Template enumTemp = Template.Parse(File.ReadAllText("Geek/Enum.liquid"));
            foreach (var e in enumTemps)
            {
                var ectx = new TemplateContext();
                ectx.LoopLimit = 0;
                var esobj = new ScriptObject();
                esobj.Import(e);
                ectx.PushGlobal(esobj);
                var str = enumTemp.Render(ectx);
                if (!output.Equals("no"))
                    File.WriteAllText($"{output}/{e.fullname}.cs", str);
                if (!clientOutput.Equals("no"))
                    File.WriteAllText($"{clientOutput}/{e.fullname}.cs", str);
            }

            Template template = Template.Parse(File.ReadAllText("Geek/Proto.liquid"));
            foreach (var cls in clsTemps)
            {
                var ctx = new TemplateContext();
                ctx.LoopLimit = 0;
                var sobj = new ScriptObject();
                sobj.Import(cls);
                ctx.PushGlobal(sobj);
                var str = template.Render(ctx);
                if (!output.Equals("no"))
                    File.WriteAllText($"{output}/{cls.fullname}.cs", str);
                if (!clientOutput.Equals("no"))
                    File.WriteAllText($"{clientOutput}/{cls.fullname}.cs", str);
            }
        }

        private void RemoveNoSubClass()
        {
            foreach (var pinfo in finalClassList)
            {
                bool found = false;
                foreach (var p in polymorphicInfos.infos)
                {
                    if (pinfo != p && p.basename == pinfo.basename)
                    {
                        found = true;
                    }
                }
                if (!found)
                    polymorphicInfos.infos.Remove(pinfo);
            }
        }


        public void GetAllClassSyntax(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var classes = tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>();
                foreach (var cls in classes)
                {
                    clsSyntaxDic.Add(cls.GetFullName(), cls);
                }
            }
        }

        public void GetAllEnumSyntax(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var enums = tree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>();
                foreach (var e in enums)
                {
                    GeekEnumTemplate template = new GeekEnumTemplate();
                    template.enumcode = e.ToFullString();
                    //Console.WriteLine("enumcode:" + template.enumcode);
                    template.space = e.GetNameSpace();
                    template.fullname = e.GetFullName();
                    //Console.WriteLine(e.ToFullString() + "_" + template.space);
                    enumTemps.Add(template);
                }
            }
        }

        public string GetPropertyCode(string name, BaseTypeDeclarationSyntax clsSyntax)
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
