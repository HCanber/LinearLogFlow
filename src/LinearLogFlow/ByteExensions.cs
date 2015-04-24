using System.Text;

namespace LinearLogFlow
{
	public static class ByteExensions
	{
		public static string ToUtf8String(this byte[] bytes)
		{
			if(bytes != null)
				return Encoding.UTF8.GetString(bytes);
			return null;
		}
	}
}