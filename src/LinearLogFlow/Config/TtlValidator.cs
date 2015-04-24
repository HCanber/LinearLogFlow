using System.Text.RegularExpressions;

namespace LinearLogFlow.Config
{
	public class TtlValidator : ITtlValidator
	{
		private static readonly Regex _validElasticsearchTime = new Regex("^[0-9]+(S|ms|s|m|H|h|d|w)?$", RegexOptions.Compiled); //https://github.com/elastic/elasticsearch/blob/master/src/main/java/org/elasticsearch/common/unit/TimeValue.java#L228

		public bool IsValidElasticTime(string time)
		{
			if(string.IsNullOrEmpty(time)) return false;
			return _validElasticsearchTime.IsMatch(time);
		}
	}
}