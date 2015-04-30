using System;
using System.IO;
using LogFlow;
using LogFlow.Builtins.Outputs;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinearLogFlow.Outputs
{
	public class ElasticsearchOutput : ElasticsearchOutputBase
	{
		private readonly ElasticsearchSettings _settings;
		private readonly JsonSerializer _serializer;

		public ElasticsearchOutput(ElasticsearchSettings settings, IElasticsearchIndexInitializer initializer = null)
			: this(settings, settings.CreateConnectionSettings(), initializer)
		{
		}

		public ElasticsearchOutput(ElasticsearchSettings settings, IConnectionSettingsValues clientSettings, IElasticsearchIndexInitializer initializer = null)
			: this(settings, clientSettings, new JsonSerializer
			{
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffK"
			}, initializer)
		{
		}

		public ElasticsearchOutput(ElasticsearchSettings settings, IConnectionSettingsValues clientSettings, JsonSerializer serializer, IElasticsearchIndexInitializer initializer = null)
			: base(settings, clientSettings, initializer)
		{
			_settings = settings;
			_serializer = serializer;
		}

		public JsonSerializer Serializer { get { return _serializer; } }

		protected override string CreateJsonBody(Result result, string id, DateTimeOffset timestamp)
		{
			SetLogLineId(result, id);
			SetMachineName(result);
			SetTtl(result);
			var json = SerializeResultJson(result.Json);
			return json;
		}

		protected virtual void SetLogLineId(Result result, string id)
		{
			result.Json[ElasticSearchFields.Id] = new JValue(id);
		}

		protected virtual void SetMachineName(Result result)
		{
			if(_settings.AddSourceField)
				result.Json[ElasticSearchFields.Source] = new JValue(Environment.MachineName);
		}

		protected virtual void SetTtl(Result result)
		{
			if(!string.IsNullOrWhiteSpace(_settings.Ttl))
			{
				result.Json[ElasticSearchFields.TTL] = new JValue(_settings.Ttl);
			}
		}

		protected virtual string SerializeResultJson(JObject jsonObject)
		{
			string json;
			using(var writer = new StringWriter())
			{
				_serializer.Serialize(writer, jsonObject);
				json = writer.ToString();
			}
			return json;
		}

		protected override string GetIndexTemplateJson()
		{
			return _settings.IndexTemplateJson ?? GetDefaultIndexTemplateJson();
		}

		private string GetDefaultIndexTemplateJson()
		{
			return null;
		}
	}
}