using System.Collections.Generic;
using MessagePackCompiler.CodeGenerator.CS;

namespace MessagePackCompiler.CodeGenerator.TS
{
    /// <summary>
    /// Scriban 对驼峰格式支持不友好
    /// 所有统一采用小写
    /// </summary>
    public class ClassTemplate
    {
        public string typename { get; set; }
        public string fullname { get; set; }
        public string name { get; set; }
        public bool ismsg { get; set; }
        public string space { get; set; }
        public string super { get; set; }

        /// <summary>
        /// 需要包含冒号
        /// </summary>
        public string supercode
        {
            get
            {
                if (string.IsNullOrEmpty(super))
                    return string.Empty;
                else
                {
                    if (ismsg)
                    {
                        return "implements " + super;
                    }
                    return "extends " + super;
                }
            }
        }


        public int sid { get; set; }

        public string atts { get; set; }

        public List<FieldTemplate> fields = new List<FieldTemplate>();

        /// <summary>
        /// 添加了Key注解的字段
        /// </summary>
        public int sfieldcount = 0;

        public List<string> usings = new List<string>();
    }

    public class FieldTemplate
    {
        public string clsname { get; set; }

        public string name { get; set; }

        public bool ignore { get; set; }

        public string propcode { get; set; }
    }


    public class GeekEnumTemplate
    {
        public string fullname { get; set; }
        public string name
        {
            get
            {
                var strs = fullname.Split(".");
                return strs[strs.Length - 1];
            }
        }
        public string space { get; set; }
        public string enumcode { get; set; }
    }

    public class MsgInfo
    {
        public string typename { get; set; }
        public int sid { get; set; }
    }

    public class MsgFactory
    {
        public List<MsgInfo> msgs = new List<MsgInfo>();
        public int count { get { return msgs.Count; } }
    }

}
