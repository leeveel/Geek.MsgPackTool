using System.Collections.Generic;
using System.Linq;
using System.Text;


public class MpcArgument
{
    public string Input;
    public string ClientOutput;
    public string ServerOutput;
    public string BaseMessageName;
    public string ConditionalSymbol;
    public string ResolverName = "GeneratedResolver";
    public string Namespace = "MessagePack";
    public bool UseMapMode;
    public string MultipleIfDirectiveOutputSymbols;
    public bool GeneratedFirst;
    public List<string> NoExportTypes;
}
