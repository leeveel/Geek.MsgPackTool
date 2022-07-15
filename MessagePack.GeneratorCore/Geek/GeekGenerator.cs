using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
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
        public const string SerializeAttribute = "Geek.Server.Proto.SerializeAttribute";
        public const string BaseMessage = "Geek.Server.Message";

        public static GeekGenerator Singleton = new GeekGenerator();
        //sub - parent
        private readonly Dictionary<int, string> sidDic = new Dictionary<int, string>();
        private readonly Dictionary<string, BaseTypeDeclarationSyntax> clsSyntaxDic = new Dictionary<string, BaseTypeDeclarationSyntax>();
        private readonly PolymorphicInfoFactory polymorphicInfos = new PolymorphicInfoFactory();
        private readonly MsgFactory msgFactory = new MsgFactory();
        private readonly List<ClassTemplate> clsTemps = new List<ClassTemplate>();
        private readonly List<GeekEnumTemplate> enumTemps = new List<GeekEnumTemplate>();

        public void GenCode(Compilation compilation, INamedTypeSymbol[] targetTypes, string output, string clientOutput)
        {
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
                
                var atts = type.GetAttributes();
                bool hasSerialize = false;
                foreach (var att in atts)
                {
                    if (att.ToString().Contains(SerializeAttribute))
                    {
                        hasSerialize = true;
                        if (att.ConstructorArguments != null && att.ConstructorArguments.Length > 0)
                        {
                            var obj = att.ConstructorArguments[0].Value;
                            clsTemp.sid = obj == null ? 0 : (int)obj;
                        }
                    }
                }

                //非枚举必须包含SerializeAttribute
                if (!hasSerialize && clsTemp.typename != "enum")
                {
                    throw new Exception($"non enum type must has {SerializeAttribute} :{type.Name}.{type.TypeKind}");
                }
                //枚举不能添加SerializeAttribute
                else if (hasSerialize && clsTemp.typename == "enum")
                {
                    throw new Exception($"enum type cannot add {SerializeAttribute} :{type.Name}.{type.TypeKind}");
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
                        //Console.WriteLine(clsTemp.super);
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
                    }
                }

                if(!string.IsNullOrEmpty(clsTemp.super))
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

            Template enumTemp = Template.Parse(File.ReadAllText("Geek/Enum.liquid"));
            foreach (var e in enumTemps)
            {
                var str = enumTemp.Render(e);
                if (!output.Equals("no"))
                    File.WriteAllText($"{output}/{e.fullname}.cs", str);
                if (!clientOutput.Equals("no"))
                    File.WriteAllText($"{clientOutput}/{e.fullname}.cs", str);
            }

            Template template = Template.Parse(File.ReadAllText("Geek/Proto.liquid"));
            foreach (var cls in clsTemps)
            {
                var str = template.Render(cls);
                if (!output.Equals("no"))
                    File.WriteAllText($"{output}/{cls.fullname}.cs", str);
                if (!clientOutput.Equals("no"))
                    File.WriteAllText($"{clientOutput}/{cls.fullname}.cs", str);
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
