using MessagePackCompiler.CodeGenerator.CS;
using MessagePackCompiler.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MessagePackCompiler.CodeGenerator.TS
{
    public class InnerGenerator
    {

        public const string KeyAttribute = "MessagePack.KeyAttribute";
        public const string IgnoreAttribute = "MessagePack.IgnoreMemberAttribute";
        public static string BaseMessage = "Geek.Server.Message";
        public static List<string> NoExportTypes = new List<string>();

        public static CS.InnerGenerator Singleton = new CS.InnerGenerator();
        //sub - parent
        private readonly Dictionary<int, string> sidDic = new Dictionary<int, string>();
        private readonly Dictionary<string, BaseTypeDeclarationSyntax> clsSyntaxDic = new Dictionary<string, BaseTypeDeclarationSyntax>();
        private readonly MsgFactory msgFactory = new MsgFactory();
        private readonly List<ClassTemplate> clsTemps = new List<ClassTemplate>();
        private readonly List<GeekEnumTemplate> enumTemps = new List<GeekEnumTemplate>();

        private readonly Dictionary<string, string> embeddedTypeMap = new Dictionary<string, string>()
        {
            {  "short","number"},
            {  "int","number"},
            {  "long","number"},
            {  "ushort","number"},
            {  "uint","number"},
            {  "ulong","number"},
            {  "float","number"},
            {  "double","number"},
            {  "bool","boolean"},
            {  "byte","number"}, 
            {  "string","string"}
        };

        public void GenCode(Compilation compilation, INamedTypeSymbol[] targetTypes, string output)
        {
            GetAllEnumSyntax(compilation);
            GetAllClassSyntax(compilation);

            foreach (var type in targetTypes)
            {
                //Console.Write("处理类型:" + type.ToString());
                if (NoExportTypes.IndexOf(type.ToString()) >= 0)
                {
                    continue;
                }
                ClassTemplate clsTemp = new ClassTemplate();
                clsTemp.name = type.Name;
                clsTemp.fullname = type.ToString();

                if (type.TypeKind == TypeKind.Enum)
                    clsTemp.typename = "enum";
                else if (type.TypeKind == TypeKind.Class)
                    clsTemp.typename = "class";
                else if (type.TypeKind == TypeKind.Struct)
                    clsTemp.typename = "class";
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
                        var strs = type.BaseType.ToString().Split(".");
                        clsTemp.super = strs[strs.Length - 1];
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

            var outStr = new StringBuilder();

            ////MsgFactory
            //var fctx = new TemplateContext();
            //fctx.LoopLimit = 0;
            //var fsobj = new ScriptObject();
            //fsobj.Import(msgFactory);
            //fctx.PushGlobal(fsobj);
            //var msgTemp = Scriban.Template.Parse(File.ReadAllText("Liquid/TS/MsgFactory.liquid"));
            //var msgstr = msgTemp.Render(fctx);

            //File.WriteAllText($"{output}/MsgFactory.ts", msgstr);



            var enumTemp = Scriban.Template.Parse(File.ReadAllText("Liquid/TS/Enum.liquid"));
            foreach (var e in enumTemps)
            {
                var ectx = new TemplateContext();
                ectx.LoopLimit = 0;
                var esobj = new ScriptObject();
                esobj.Import(e);
                ectx.PushGlobal(esobj);
                var str = enumTemp.Render(ectx);

                outStr.Append(str);

                  //File.WriteAllText($"{output}/{e.fullname}.ts", str);
            }

            var template = Scriban.Template.Parse(File.ReadAllText("Liquid/TS/Proto.liquid"));
            foreach (var cls in clsTemps)
            {
                Console.WriteLine(cls.fullname);
                var ctx = new TemplateContext();
                ctx.LoopLimit = 0;
                var sobj = new ScriptObject();
                sobj.Import(cls);
                ctx.PushGlobal(sobj);
                var str = template.Render(ctx);
                 
                outStr.Append(str);

                // File.WriteAllText($"{output}/{cls.fullname}.ts", str);
            }
           var finalStr = Scriban.Template.Parse(File.ReadAllText("Liquid/TS/Final.liquid")).Render(new { body  = outStr.ToString() });
            File.WriteAllText($"{output}/proto.ts", finalStr);
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
                    template.enumcode = e.ToFullString().Replace("public", "export");
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
                {
                    var start = "";
                    var full = prop.ToFullString();
                    if (full.Contains("public "))
                        start += "public ";
                    if (full.Contains("protected "))
                        start += "protected ";
                    if (full.Contains("private "))
                        start += "private ";
                    if (full.Contains("static "))
                        start += "static ";
                    if (full.Contains("const "))
                        start += "const ";

                    var tsType = GetTSType(prop.Type);
                     
                    return start + $"{name}:{tsType}{GetDefaultInit(tsType, prop.Initializer?.ToString())};";
                }
            }
            throw new Exception($"can not find property [{name}] in {clsSyntax.GetFullName()}");
        }

        public string GetTSType(TypeSyntax type)
        {
            var defRet = "any";
            if (type is GenericNameSyntax genType)
            {
                var typeName = genType.Identifier.ToString();
                if (typeName == "Dictionary")
                {
                    var args = genType.TypeArgumentList.Arguments;
                    if (args.Count == 2)
                    {
                        var t0 = GetTSType(genType.TypeArgumentList.Arguments[0]);
                        var t1 = GetTSType(genType.TypeArgumentList.Arguments[1]);
                        return $"Map<{t0},{t1}>";
                    }
                    return defRet;
                }
                else if (typeName == "List")
                {
                    var t0 = GetTSType(genType.TypeArgumentList.Arguments[0]); 
                    return $"{t0}[]";
                }
            }

            var tName = type.ToString();
            if (embeddedTypeMap.TryGetValue(tName, out var v))
            {
                return v;
            }

            var enumT = enumTemps.Find(v => v.fullname.EndsWith("." + tName));
           if(enumT!=null)
            {
                return enumT.name;
            }

            var classT = clsTemps.Find(v => v.fullname.EndsWith("." + tName));
            if (classT != null)
            {
                return classT.name;
            }

            return defRet;
        }

        public string GetDefaultInit(string tsType,string defaultValue)
        {
            if(tsType.StartsWith("Map"))
            {
                return "=new Map()";
            }
            if (tsType.EndsWith("[]"))
            {
                return "=new Array()";
            }

            if(enumTemps.Find(v => v.fullname==tsType)!=null)
            {
                return defaultValue;
            }
            return "";
        }

    }
}
