using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum TargetLanguageType
{
    CS,
    TS
}
public class MpcArgument
{
    public string Input;
    public string ClientOutput;
    public string ServerOutput;
    public string TSOutput;
    public string BaseMessageName;
    public string ConditionalSymbol;
    public string ResolverName = "GeneratedResolver";
    public string Namespace = "MessagePack";
    public bool UseMapMode;
    public string MultipleIfDirectiveOutputSymbols;
    public bool GeneratedFirst;
    public TargetLanguageType targetLangType = TargetLanguageType.CS;
    public List<string> NoExportTypes;
}
