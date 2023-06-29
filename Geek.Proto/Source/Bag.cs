using MessagePack;
using System.Collections.Generic;
//背包消息
namespace Message.Bag
{

    public class ItemView : BaseMessage
    {
        public List<int> itemIdList { get; set; } = new List<int>();
    }

    public class BEquipInfo
    {
        //装备数量
        public int num { get; set; }
        //装备实例ID
        public long InstanceId { get; set; }
    }

    public class ResEquipSave : BaseMessage
    {
        public List<BEquipInfo> equipInfo { get; set; }
    }
}
