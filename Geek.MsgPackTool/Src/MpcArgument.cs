using System.Text;

namespace Geek.MsgPackTool
{
    public class MpcArgument
    {
        public string Input;
        public string ClientOutput;
        public string ServerOutput;
        public string ConditionalSymbol;
        public string ResolverName;
        public string Namespace;
        public bool UseMapMode;
        public string MultipleIfDirectiveOutputSymbols;
        public bool GeneratedFirst;

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
            sb.Append(" -gf "); sb.Append(GeneratedFirst);
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
