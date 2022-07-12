using System.Collections.Generic;

namespace MessagePackCompiler
{
    /// <summary>
    /// Scriban 对驼峰格式支持不友好
    /// 所有统一采用小写
    /// </summary>
    public class ClassTemplate
    {
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
                if(string.IsNullOrEmpty(super))
                    return string.Empty;
                else
                    return ": " + super;
            }
        }


        public int sid { get; set; }

        public string atts{ get; set; }

        public List<FieldTemplate> fields = new List<FieldTemplate>();

        /// <summary>
        /// 添加了Key注解的字段
        /// </summary>
        public int sfieldcount = 0;

        public bool autonew { get; set; }

        public List<string> usings = new List<string>();
    }

    public class FieldTemplate
    {
        public string clsname { get; set; }

        public string name { get; set; }

        public int order { get; set; } = -1;

        public bool ignore { get; set; }

        public List<string> attributes = new List<string>();

        public Dictionary<int, string> orderdic = new Dictionary<int, string>();

        public string propcode
        {
            get 
            {
                if (ignore)
                    return ignorepropcode;
                return orderdic[order]; 
            }
        }
        public string ignorepropcode { get; set; }
    }

    public class PolymorphicInfo
    {
        public string basename { get; set; }

        public string subname { get; set; }

        public int subsid { get; set; }
    }

    public class PolymorphicInfoFactory
    {
        public List<PolymorphicInfo> infos = new List<PolymorphicInfo>();
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
