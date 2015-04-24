using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public class IndexTypeConfig
	{
		private readonly List<LogConfig> _logs = new List<LogConfig>();

		public string Type { get; set; }
		public string MappingPath { get; set; }
		public string MappingJson { get; set; }

		public List<LogConfig> Logs { get { return _logs; } }
	}
}