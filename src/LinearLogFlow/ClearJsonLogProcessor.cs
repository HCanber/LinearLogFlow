using LogFlow;
using Newtonsoft.Json.Linq;

namespace LinearLogFlow
{
	/// <summary>
	/// A <see cref="LogProcessor"/> that clears the <see cref="Result.Json"/> property.
	/// </summary>
	public class ClearJsonLogProcessor : LogProcessor
	{
		public override Result Process(Result result)
		{
			result.Json=new JObject();
			return result;
		}
	}
}