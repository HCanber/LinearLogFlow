using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using LinearLogFlow.Config;
using Nest;
using NLog;

namespace LinearLogFlow
{
	public class ElasticsearchIndexInitializer : IElasticsearchIndexInitializer
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly HashSet<string> _initializedIndexes = new HashSet<string>();
		private readonly object _lock = new object();

		private readonly List<IndexTypeConfig> _configs;

		public ElasticsearchIndexInitializer(List<IndexTypeConfig> configs)
		{
			_configs = configs;
		}

		public void Initialize(IElasticClient client, string indexName)
		{
			if(_initializedIndexes.Contains(indexName)) return;
			lock(_lock)
			{
				if(_initializedIndexes.Contains(indexName)) return;
				_initializedIndexes.Add(indexName);
			}
			PerformInitialization(client, indexName);
		}

		protected virtual void PerformInitialization(IElasticClient client, string indexName)
		{
			var configsWithMappings = _configs.Where(c => !string.IsNullOrEmpty(c.MappingJson)).ToList();
			if(_log.IsDebugEnabled)
				_log.Debug("Performing initialization for index {0} by putting mappings for the types: {1}", indexName, string.Join(", ", configsWithMappings.Select(c => c.Type)));

			foreach(var config in configsWithMappings)
			{
				const int numberOfAttempts = 3;
				var mappingJson = config.MappingJson;
				var elasticType = config.Type;
				var wasSuccesful = false;
				ElasticsearchResponse<DynamicDictionary> response;
				var attempt = 1;
				do
				{
					_log.Trace("About to put mapping for type {1} in index {0}. Attempt {2}/{3}", indexName, elasticType, attempt, numberOfAttempts);

					response = client.Raw.IndicesPutMapping(indexName, elasticType, mappingJson);

					//If the response is successful or has a known error (400-500 range). The client should not retry this call
					if(response.SuccessOrKnownError)
					{
						if(response.Success)
							wasSuccesful = true;
						else
							_log.Error("Unable to {3} index mapping for type {0} in index {1} on {2}: {4}", elasticType, indexName, response.RequestUrl, response.RequestMethod, response.ResponseRaw.ToUtf8String());
						break; //Stop attempting
					}
					attempt++;
				} while(attempt < numberOfAttempts);
				if(wasSuccesful)
					_log.Trace("Mapping for type {1} in index {0} was successful. Attempt {2}/{3}", indexName, elasticType, attempt, numberOfAttempts);
				else
					_log.Error("Unable to {3} index mapping for type {0} in index {1} on {2}. Made {4} attempts: {5}", elasticType, indexName, response.RequestUrl, response.RequestMethod, numberOfAttempts, response.ResponseRaw.ToUtf8String());
			}
		}
	}
}