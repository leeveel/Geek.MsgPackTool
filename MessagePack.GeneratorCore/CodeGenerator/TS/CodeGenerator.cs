// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePackCompiler.CodeAnalysis;
using MessagePackCompiler.CodeGenerator.CS;
using MessagePackCompiler.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using InnerGenerator = MessagePackCompiler.CodeGenerator.TS.InnerGenerator;

namespace MessagePackCompiler.CodeGenerator.TS
{
    public class CodeGenerator
    {
        private static readonly HashSet<char> InvalidFileCharSet = new(Path.GetInvalidFileNameChars());

        private static readonly Encoding NoBomUtf8 = new UTF8Encoding(false);

        private readonly Action<string> logger;

        public CodeGenerator(Action<string> logger, CancellationToken cancellationToken)
        {
            this.logger = logger;
        }

        public async Task GenerateFileAsync(
        Compilation compilation,
        string output,
        string? @namespace,
        bool useMapMode,
        string? multipleIfDirectiveOutputSymbols,
        string[]? externalIgnoreTypeNames)
        {
            var namespaceDot = string.IsNullOrWhiteSpace(@namespace) ? string.Empty : @namespace + ".";
            var multipleOutputSymbols = multipleIfDirectiveOutputSymbols?.Split(',') ?? Array.Empty<string>();

            foreach (var multiOutputSymbol in multipleOutputSymbols.Length == 0 ? new[] { string.Empty } : multipleOutputSymbols)
            {
                logger("开始收集类型...");
                var collector = new TypeCollector(compilation, true, useMapMode, externalIgnoreTypeNames, Console.WriteLine);


                var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect(InnerGenerator.NoExportTypes);

                logger("开始生成协议...");
                //生成协议代码
                new InnerGenerator().GenCode(compilation, collector.TargetTypes, output);


                if (objectInfo.Length == 0 && enumInfo.Length == 0 && genericInfo.Length == 0 && unionInfo.Length == 0)
                {
                    logger("生成结果为空，是否正确?");
                }
            }

            logger("结束..........................................................");
            logger("输入操作继续...");
        }  
    }
}
