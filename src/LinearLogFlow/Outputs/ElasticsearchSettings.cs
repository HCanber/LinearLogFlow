using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net.ConnectionPool;
using Nest;

namespace LinearLogFlow.Outputs
{
	public class ElasticsearchSettings : IElasticsearchSettings
	{
		public ElasticsearchSettings()
		{
			Uris = new[] { new Uri("http://localhost:9200") };
			IndexNameFormat = @"logflow-{yyyyMM}";
		}

		public IReadOnlyCollection<Uri> Uris { get; set; }
		public string Ttl { get; set; }
		public string IndexNameFormat { get; set; }
		public IReadOnlyCollection<string> TimestampPropertyNames { get; set; }

		public string IndexTemplateJson { get; set; }	//See https://gist.github.com/mivano/9688328
		public bool AddSourceField { get; set; }
		public bool IsCluster { get; set; }
		//public string MappingJson { get; set; } //See http://www.elastic.co/guide/en/elasticsearch/reference/1.4/indices-put-mapping.html but start one level lower, i.e. without the mapping type

		public IConnectionSettingsValues CreateConnectionSettings()
		{
			if(IsCluster)
			{
				var connectionPool = new SniffingConnectionPool(Uris);
				var settings = new ConnectionSettings(connectionPool)
					.SniffOnConnectionFault()
					.SniffOnStartup(false)
					.SniffLifeSpan(TimeSpan.FromMinutes(1))
					.ExposeRawResponse();
				return settings;
			}
			var clientSettings = new ConnectionSettings(Uris.First());
			return clientSettings;
		}
	}
}