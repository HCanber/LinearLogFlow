using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public interface IConfigParser
	{
		List<ServerConfig> GetConfigFromContent(string configContent, string configName);
	}
}