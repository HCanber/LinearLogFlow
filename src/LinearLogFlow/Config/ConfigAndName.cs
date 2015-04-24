using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public class ConfigAndName
	{
		private readonly IReadOnlyCollection<ServerConfig> _config;
		private readonly string _configName;

		public ConfigAndName(IReadOnlyCollection<ServerConfig> config, string configName)
		{
			_config = config;
			_configName = configName;
		}

		public IReadOnlyCollection<ServerConfig> Config { get { return _config; } }
		public string ConfigName { get { return _configName; } }

	}
}