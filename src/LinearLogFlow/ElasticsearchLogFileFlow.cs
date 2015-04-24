using System;
using System.Text;
using LinearLogFlow.Outputs;
using LogFlow;
using LogFlow.Builtins.Inputs;

namespace LinearLogFlow
{
	public class ElasticsearchLogFileFlow : Flow
	{

		public ElasticsearchLogFileFlow(string logFilePath, string logFlowName, string logType, Encoding encoding, ElasticsearchSettings elasticConfig, IElasticsearchIndexInitializer elasticsearchIndexInitializer)
		{
			if(logFilePath == null) throw new ArgumentNullException("logFilePath");
			if(elasticConfig == null) throw new ArgumentNullException("elasticConfig");

			var fileInput = encoding != null ? new FileInput(logFilePath, encoding, false) : new FileInput(logFilePath);
			CreateProcess(logType, logFlowName)
				.FromInput(fileInput)
				.Then(new ElasticsearchLogProcessor())
				.ToOutput(new ElasticsearchOutput(elasticConfig, elasticsearchIndexInitializer));
		}
	}
}
