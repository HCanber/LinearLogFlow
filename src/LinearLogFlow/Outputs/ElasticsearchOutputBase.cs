using System;
using System.Collections.Generic;
using System.Globalization;
using LinearLogFlow.Serialization;
using LogFlow;
using Nest;
using Newtonsoft.Json.Linq;
using NLog;

namespace LinearLogFlow.Outputs
{
	public abstract class ElasticsearchOutputBase : LogOutput
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly IElasticsearchSettings _settings;
		private readonly IElasticsearchIndexInitializer _initializer;
		private readonly HashSet<string> _indexNames = new HashSet<string>();
		private readonly ElasticClient _client;
		private readonly bool _indexNameDependsOnTimestamp;

		protected ElasticsearchOutputBase(IElasticsearchSettings settings, IElasticsearchIndexInitializer initializer = null)
			: this(settings, settings.CreateConnectionSettings(), initializer)
		{
			//Intentionally left blank
		}

		protected ElasticsearchOutputBase(IElasticsearchSettings settings, IConnectionSettingsValues clientSettings, IElasticsearchIndexInitializer initializer = null)
		{
			_settings = settings;
			_initializer = initializer;
			_client = new ElasticClient(clientSettings);
			var indexNameDependsOnTimestamp = DoIndexNameChangeBasedOnTimestamp(settings.IndexNameFormat);
			_indexNameDependsOnTimestamp = indexNameDependsOnTimestamp;
		}

		private static bool DoIndexNameChangeBasedOnTimestamp(string indexNameFormat)
		{
			var name1 = new DateTimeOffset(1, 2, 3, 4, 5, 6, 7, new TimeSpan(8, 9, 0)).ToString(indexNameFormat);
			var name2 = new DateTimeOffset(13, 12, 14, 15, 16, 17,18, new TimeSpan(10, 11, 0)).ToString(indexNameFormat);
			var indexNameDependsOnTimestamp = !string.Equals(name1, name2);
			return indexNameDependsOnTimestamp;
		}

		protected static Logger Log { get { return _log; } }
		protected ElasticClient Client { get { return _client; } }

		public override void Process(Result result)
		{
			var id = SerializeLogLineId(result.Id);
			var timestamp = GetTimestamp(result);
			var json = CreateJsonBody(result, id, timestamp);
			IndexLog(json, timestamp, LogContext.LogType, id);
		}

		protected virtual DateTimeOffset GetTimestamp(Result result)
		{

			var timestamp = GetTimestampFromResultJson(result);
			if(timestamp != null) return timestamp.Value;

			var resultTimeStamp = result.EventTimeStamp;
			if(resultTimeStamp.HasValue)
			{
				return resultTimeStamp.Value;
			}
			if(_indexNameDependsOnTimestamp)
				throw new Exception(string.Format("No timestamp value found on log line"));
			return DateTimeOffset.Now;
		}

		protected abstract string CreateJsonBody(Result result, string id, DateTimeOffset timestamp);

		protected virtual string SerializeLogLineId(Guid idGuid)
		{
			return ValidForHttpEncoder.ToString(idGuid);
		}

		protected virtual void IndexLog(string jsonBody, DateTimeOffset timestamp, string logType, string lineId)
		{
			var indexName = BuildIndexName(timestamp).ToLowerInvariant();

			EnsureIndexExists(indexName);
			var indexResult = _client.Raw.IndexPut(indexName, logType, lineId, jsonBody);

			if(!indexResult.Success)
			{
				throw new ApplicationException(string.Format("Failed to index: '{0}'. Response: '{1}'.", jsonBody, indexResult.ResponseRaw.ToUtf8String()));
			}

			_log.Trace("{0}: ({1}) Indexed successfully.", LogContext.LogType, lineId);
		}


		protected virtual string BuildIndexName(DateTimeOffset timestamp)
		{
			return timestamp.ToString(_settings.IndexNameFormat,CultureInfo.InvariantCulture);
		}

		protected void EnsureIndexExists(string indexName)
		{
			if(_indexNames.Contains(indexName))
				return;

			if(!_client.IndexExists(indexName).Exists)
			{
				CreateIndex(indexName);
			}
			if(_initializer != null)
			{
				_initializer.Initialize(_client, indexName);
			}
			_indexNames.Add(indexName);

		}

		protected virtual DateTimeOffset? GetTimestampFromResultJson(Result result)
		{
			var json = result.Json;
			foreach(var timestampPropertyName in _settings.TimestampPropertyNames)
			{

				JToken jToken;
				if(json.TryGetValue(timestampPropertyName, StringComparison.OrdinalIgnoreCase, out jToken))
				{
					DateTimeOffset timestamp;
					var timestampString = jToken.ToString();
					if(DateTimeOffset.TryParse(timestampString, out timestamp))
						return timestamp;
					throw new Exception(string.Format("Timestamp parse error. Unable to parse {0}:\"{1}\" as a DateTimeOffset", timestampString, timestampPropertyName));
				}
				else
				{
					return null;
				}
			}

			return null;
		}

		protected virtual void CreateIndex(string indexName)
		{
			var indexTemplateJson = GetIndexTemplateJson();

			var response = _client.Raw.IndicesCreatePost(indexName, indexTemplateJson);

			if(!response.Success || response.HttpStatusCode != 200)
			{

				throw new ApplicationException(string.Format("Failed to create index: '{0}'.\nRequest:\n{1}\nResponse:\n{2}", indexName, response.Request.ToUtf8String(), response.ResponseRaw.ToUtf8String()));
			}

			_log.Trace("{0}: Index '{1}' successfully created.", LogContext.LogType, indexName);
		}

		protected virtual string GetIndexTemplateJson()
		{
			return null;
		}
	}
}