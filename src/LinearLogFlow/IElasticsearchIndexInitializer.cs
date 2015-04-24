using Nest;

namespace LinearLogFlow
{
	public interface IElasticsearchIndexInitializer
	{
		void Initialize(ElasticClient client, string indexName);
	}
}