using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public class IndexTypeConfig
	{
		private readonly List<LogConfig> _logs = new List<LogConfig>();

		public IndexTypeConfig()
		{
			MappingPaths = new List<string>();
			MappingJsons = new List<JsonWithPath>();
		}
		public string Type { get; set; }
		public IReadOnlyCollection<string> MappingPaths { get; set; }
		public IReadOnlyCollection<JsonWithPath> MappingJsons { get; set; }

		public List<LogConfig> Logs { get { return _logs; } }
	}
}