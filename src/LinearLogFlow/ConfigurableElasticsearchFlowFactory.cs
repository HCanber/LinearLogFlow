using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LinearLogFlow.Config;
using LinearLogFlow.Outputs;
using LogFlow;

namespace LinearLogFlow
{
	[DoNotAutoCreate]
	// ReSharper disable once UnusedMember.Global
	public class ConfigurableElasticsearchFlowFactory : IFlowFactory
	{
		//private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly IConfigProvider _configProvider;
		public static readonly IReadOnlyCollection<string> DefaultTimestampPropertyNames =
			new ReadOnlyCollection<string>(new []
			{
				"@timestamp" ,
				"timestamp" ,
				"datetime",
				"date",
				"time"
			});

		public ConfigurableElasticsearchFlowFactory()
			: this(new FileConfigProvider(new XmlConfigParser(new ResourceConfigXmlSchemasProvider(), new EncodingParser(), new TtlValidator(), new ConfigXmlDocValidator(new ResourceConfigXmlSchemasProvider()), new IndexNameParser())))
		{

		}

		public ConfigurableElasticsearchFlowFactory(IConfigProvider configProvider)
		{
			_configProvider = configProvider;
		}

		public IEnumerable<Flow> CreateFlows()
		{
			var configAndName = _configProvider.GetConfig();
			if(configAndName == null)
				throw new Exception(_configProvider.GetNotFoundMessage());

			return CreateFlows(configAndName.Config);
		}

		private IEnumerable<Flow> CreateFlows(IReadOnlyCollection<ServerConfig> serverConfigs)
		{
			var flows = new List<Flow>();
			foreach(var serverConfig in serverConfigs)
			{
				foreach(var kvp in serverConfig.IndexConfigByName)
				{
					var indexConfig = kvp.Value;
					var indexTypeConfigs = indexConfig.IndexTypeConfigByType.Values.ToList();
					var initializer = new ElasticsearchIndexInitializer(indexTypeConfigs);
					foreach(var indexTypeConfig in indexConfig.IndexTypeConfigByType.Values)
					{
						foreach(var logConfig in indexTypeConfig.Logs)
						{
							var flow = CreateFlow(serverConfig, indexConfig, logConfig, indexTypeConfig, initializer);
							flows.Add(flow);
						}
					}
				}
			}
			return flows;
		}




		private ElasticsearchLogFileFlow CreateFlow(ServerConfig serverConfig, IndexConfig indexConfig, LogConfig logConfig, IndexTypeConfig indexTypeConfig, ElasticsearchIndexInitializer initializer)
		{
			var settings = CreateSettings(serverConfig, indexConfig, logConfig);
			return new ElasticsearchLogFileFlow(logConfig.Path, logConfig.LogFlowName, indexTypeConfig.Type, logConfig.Encoding, settings, initializer);
		}

		private static ElasticsearchSettings CreateSettings(ServerConfig serverConfig, IndexConfig indexConfig, LogConfig logConfig)
		{
			var index = indexConfig.Index;
			var settings = new ElasticsearchSettings()
			{
				Uris = serverConfig.Uris,
				IsCluster = serverConfig.IsCluster,
				Ttl = logConfig.Ttl,
				IndexNameFormat = index,
				IndexTemplateJson = indexConfig.IndexTemplateJson,
				AddSourceField = logConfig.AddSourceField,
				TimestampPropertyNames = logConfig.TimestampPropertyNames ?? DefaultTimestampPropertyNames
			};
			return settings;
		}
	}
}