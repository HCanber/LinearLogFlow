using System;
using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public class ServerConfig
	{
		private readonly Dictionary<string, IndexConfig> _indexConfigByName = new Dictionary<string, IndexConfig>();

		public List<Uri> Uris { get; set; }
		public bool IsCluster { get; set; }
		public Dictionary<string, IndexConfig> IndexConfigByName { get { return _indexConfigByName; } }
	}
}