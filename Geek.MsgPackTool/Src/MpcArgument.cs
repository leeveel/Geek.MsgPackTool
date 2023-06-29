using System.Linq;
using System.Text;

namespace Geek.MsgPackTool
{
    public class MpcArgument
    {
        public string Input;
        public string ClientOutput;
        public string ServerOutput;
        public string BaseMessageName;
        public string ConditionalSymbol;
        public string ResolverName;
        public string Namespace;
        public bool UseMapMode;
        public string MultipleIfDirectiveOutputSymbols;
        public bool GeneratedFirst;
        public List<string> NoExportTypes;

        public static MpcArgument Restore()
        {
            return new MpcArgument();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("-i "); sb.Append(Input);
            sb.Append(" -o "); sb.Append(ClientOutput);
            sb.Append(" -so "); sb.Append(ServerOutput);
            sb.Append(" -bmn "); sb.Append(BaseMessageName);
            sb.Append(" -gf "); sb.Append(GeneratedFirst);
            sb.Append(" -nets "); sb.Append(string.Join(",", NoExportTypes));
            if (!string.IsNullOrWhiteSpace(ConditionalSymbol))
            {
                sb.Append(" -c "); sb.Append(ConditionalSymbol);
            }
            if (!string.IsNullOrWhiteSpace(ResolverName))
            {
                sb.Append(" -r "); sb.Append(ResolverName);
            }
            if (UseMapMode)
            {
                sb.Append(" -m");
            }
            if (!string.IsNullOrWhiteSpace(Namespace))
            {
                sb.Append(" -n "); sb.Append(Namespace);
            }
            if (!string.IsNullOrWhiteSpace(MultipleIfDirectiveOutputSymbols))
            {
                sb.Append(" -ms "); sb.Append(MultipleIfDirectiveOutputSymbols);
            }

            return sb.ToString();
        }
    }
}
