using System;
using LogFlow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace LinearLogFlow
{
	public class ElasticsearchLogProcessor : LogProcessor
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public override Result Process(Result result)
		{
			var json = Deserialize(result);
			CopyExistingValues(result, json);
			result.Json = json;

			return result;
		}

		private static JObject Deserialize(Result result)
		{
			try
			{
				return (JObject)JsonConvert.DeserializeObject(result.Line);
			}
			catch(Exception e)
			{
				_log.ErrorException(string.Format("Unable to deserialize {0}", result.Line), e);
				throw;
			}
		}

		private static void CopyExistingValues(Result result, JObject json)
		{
			foreach(var kvp in result.Json)
			{
				var propertyName = kvp.Key;
				JToken ignored;
				if(!json.TryGetValue(propertyName, out ignored))
					json[propertyName] = kvp.Value;
			}
		}
	}
}