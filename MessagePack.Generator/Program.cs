// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MessagePack.Generator
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var instance = MSBuildLocator.RegisterDefaults();

            Console.WriteLine("Geek.MsgPackTool start....");

            //初始化配置信息
            if (!Setting.Init())
            {
                Console.WriteLine("----配置错误，启动失败----");
                return;
            }

            while (true)
            {
                Console.WriteLine("请按需输入指令:");
                Console.WriteLine("1.导出服务器");
                Console.WriteLine("2.导出客户端");
                Console.WriteLine("3.导出服务器+客户端");
                Console.WriteLine("4.导出TS");

                var key = Console.ReadKey().KeyChar;
                Console.WriteLine("你输入了:" + key.ToString());
                Task? task = null;
                switch (key)
                {
                    case '1':
                        task = Gen(1);
                        break;
                    case '2':
                        task = Gen(2);
                        break;
                    case '3':
                        task = Gen(3);
                        break;
                    case '4':
                        task = Gen(4);
                        break;
                    default:
                        Console.WriteLine("输入指令错误");
                        break;
                }
                task?.Wait();
            }
        }

        static async Task Gen(int model)
        {
            MpcArgument mpcArgument = new MpcArgument();
            mpcArgument.Input = Setting.Ins.ProjectPath;
            mpcArgument.GeneratedFirst = Setting.Ins.GeneratedFirst;
            mpcArgument.BaseMessageName = Setting.Ins.BaseMessageName;
            mpcArgument.NoExportTypes = Setting.Ins.NoExportList;
            if (model == 1)
            {
                mpcArgument.ServerOutput = Setting.Ins.ServerOutPath;
                mpcArgument.ClientOutput = "no";
            }
            else if (model == 2)
            {
                mpcArgument.ServerOutput = "no";
                mpcArgument.ClientOutput = Setting.Ins.ClientOutPath;
            }
            else if (model == 3)
            {
                mpcArgument.ServerOutput = Setting.Ins.ServerOutPath;
                mpcArgument.ClientOutput = Setting.Ins.ClientOutPath;
            }
            else if (model == 4)
            {
                mpcArgument.targetLangType = TargetLanguageType.TS;
                mpcArgument.TSOutput = Setting.Ins.TSOutPath;
            }
            else
            {
                Console.WriteLine("输入指令错误");
                return;
            }

            await RunAsync(mpcArgument);
        }

        public static async Task RunAsync(MpcArgument args)
        {
            if (args.targetLangType == TargetLanguageType.CS)
            {
                MessagePackCompiler.CodeGenerator.CS.InnerGenerator.BaseMessage = args.BaseMessageName;
                MessagePackCompiler.CodeGenerator.CS.InnerGenerator.NoExportTypes = new List<string>();
                if (args.NoExportTypes != null)
                    MessagePackCompiler.CodeGenerator.CS.InnerGenerator.NoExportTypes.AddRange(args.NoExportTypes);
            }
            if (args.targetLangType == TargetLanguageType.TS)
            {
                MessagePackCompiler.CodeGenerator.TS.InnerGenerator.BaseMessage = args.BaseMessageName;
                MessagePackCompiler.CodeGenerator.TS.InnerGenerator.NoExportTypes = new List<string>();
                if (args.NoExportTypes != null)
                    MessagePackCompiler.CodeGenerator.TS.InnerGenerator.NoExportTypes.AddRange(args.NoExportTypes);
            }

            Workspace? workspace = null;
            try
            {
                Compilation compilation;
                if (Directory.Exists(args.Input))
                {
                    string[]? conditionalSymbols = args.ConditionalSymbol?.Split(',');
                    compilation = await PseudoCompilation.CreateFromDirectoryAsync(args.Input, conditionalSymbols, CancellationToken.None);
                }
                else
                {
                    (workspace, compilation) = await OpenMSBuildProjectAsync(args.Input, CancellationToken.None);
                }
                if (args.targetLangType == TargetLanguageType.CS)
                {
                    await new MessagePackCompiler.CodeGenerator.CS.CodeGenerator(x => Console.WriteLine(x), CancellationToken.None)
                        .GenerateFileAsync(
                            compilation,
                            args.ClientOutput,
                            args.ServerOutput,
                            args.GeneratedFirst,
                            args.ResolverName,
                            args.Namespace,
                            args.UseMapMode,
                            args.MultipleIfDirectiveOutputSymbols,
                            null).ConfigureAwait(false);
                }
                else if (args.targetLangType == TargetLanguageType.TS)
                {
                    await new MessagePackCompiler.CodeGenerator.TS.CodeGenerator(x => Console.WriteLine(x), CancellationToken.None)
                        .GenerateFileAsync(
                            compilation,
                            args.TSOutput,
                            args.Namespace,
                            args.UseMapMode,
                            args.MultipleIfDirectiveOutputSymbols,
                            null).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error:" + e.Message);
            }
            finally
            {
                //   MSBuildLocator.Unregister();
            }
        }

        static private async Task<(Workspace Workspace, Compilation Compilation)> OpenMSBuildProjectAsync(string projectPath, CancellationToken cancellationToken)
        {
            var workspace = MSBuildWorkspace.Create();
            try
            {
                var logger = new ConsoleLogger(Microsoft.Build.Framework.LoggerVerbosity.Quiet);
                var project = await workspace.OpenProjectAsync(projectPath, logger, null, cancellationToken);
                var compilation = await project.GetCompilationAsync(cancellationToken);


                if (compilation is null)
                {
                    throw new NotSupportedException("The project does not support creating Compilation.");
                }

                return (workspace, compilation);
            }
            catch
            {
                workspace.Dispose();
                throw;
            }
        }
    }
}
