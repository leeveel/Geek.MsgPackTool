using System.Collections.Generic;

namespace MessagePackCompiler
{
    /// <summary>
    /// Scriban 对驼峰格式支持不友好
    /// 所有统一采用小写
    /// </summary>
    public class ClassTemplate
    {
        public string name { get; set; }
        public bool ismsg { get; set; }
        public string space { get; set; }
        /// <summary>
        /// 需要包含冒号
        /// </summary>
        public string super { get; set; }

        public int sid { get; set; }

        public string atts{ get; set; }

        public List<FieldTemplate> fields = new List<FieldTemplate>();
    }

    public class FieldTemplate
    {
        public string clsname { get; set; }

        public string name { get; set; }

        public List<string> attributes = new List<string>();
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

}
