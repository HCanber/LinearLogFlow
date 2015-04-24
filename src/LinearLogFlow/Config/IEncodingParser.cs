using System.Text;

namespace LinearLogFlow.Config
{
	public interface IEncodingParser
	{
		Encoding ParseEncoding(string encoding);
	}
}