using System.Collections.Generic;
using Nest;

namespace LinearLogFlow.Outputs
{
	public interface IElasticsearchSettings
	{
		IConnectionSettingsValues CreateConnectionSettings();
		string IndexNameFormat { get; }
		IReadOnlyCollection<string> TimestampPropertyNames { get; }
	}
}