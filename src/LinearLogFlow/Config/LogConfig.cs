using System.Collections.Generic;
using System.Text;

namespace LinearLogFlow.Config
{
	public class LogConfig
	{
		public string Path { get; set; }
		public Encoding Encoding { get; set; }
		public string Ttl { get; set; }
		public bool AddSourceField { get; set; }
		public string LogFlowName { get; set; }
		public IReadOnlyCollection<string> TimestampPropertyNames { get; set; }
	}
}