using Nest;

namespace LinearLogFlow
{
	public interface IElasticsearchIndexInitializer
	{
		void Initialize(IElasticClient client, string indexName);
	}
}