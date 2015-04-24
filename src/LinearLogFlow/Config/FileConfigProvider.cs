using System.Collections.Generic;
using System.IO;

namespace LinearLogFlow.Config
{
	public class FileConfigProvider : IConfigProvider
	{
		private readonly IConfigParser _parser;

		public FileConfigProvider(IConfigParser parser)
		{
			_parser = parser;
		}

		public ConfigAndName GetConfig()
		{
			var configFileNames = GetConfigFileNames();
			foreach(var configFileName in configFileNames)
			{
				var path = Path.GetFullPath(configFileName);
				var configContent = GetConfigContentFromFile(path);
				if(configContent != null)
				{
					var serverConfigs = ParseConfig(configContent, configFileName);
					return new ConfigAndName(serverConfigs, path);
				}
			}
			return null;
		}

		public string GetNotFoundMessage()
		{
			return string.Format("No config file found. Looked for {0}", string.Join(", ", GetConfigFileNames()));
		}

		protected virtual IReadOnlyCollection<string> GetConfigFileNames()
		{
			return new[] { "logs.config", "LogFlow.config" };
		}


		private List<ServerConfig> ParseConfig(string configContent, string configFileName)
		{
			var serverConfigs = _parser.GetConfigFromContent(configContent, configFileName);
			return serverConfigs;
		}

		protected string GetConfigContentFromFile(string configFileName)
		{
			configFileName = Path.GetFullPath(configFileName);

			if(!File.Exists(configFileName)) return null;

			var configContent = File.ReadAllText(configFileName);
			return configContent;
		}

	}
}