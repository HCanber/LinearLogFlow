using System.Collections.Generic;

namespace LinearLogFlow.Config
{
	public class IndexConfig
	{
		private readonly Dictionary<string, IndexTypeConfig> _indexTypeConfigByType = new Dictionary<string, IndexTypeConfig>();

		public string Index { get; set; }
		public string IndexTemplateJson { get; set; }
		public string IndexTemplatePath { get; set; }
		public Dictionary<string, IndexTypeConfig> IndexTypeConfigByType { get { return _indexTypeConfigByType; } }
	}
}