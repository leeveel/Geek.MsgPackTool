using MessagePack;

namespace Geek.Server.Proto
{

	[MessagePackObject]
    [Serialize(110001)]
	public class ReqLogin
	{
		[Key(0)]
		public string UserName { get; set; }
		[Key(1)]
		public string Platform { get; set; }
		[Key(2)]
		public int SdkType { get; set; }
		[Key(3)]
		public string SdkToken { get; set; }
		[Key(4)]
		public string Device { get; set; }
		[Key(5)]
		public UserInfo User { get; set; }
	}

	[MessagePackObject]
	[Serialize(110002)]
	public class UserInfo
	{
        [Key(0)]
		public int Age { get; set; }
		[Key(1)]
		public string Name { get; set; }
	}

}