namespace LinearLogFlow.Config
{
	public interface IIndexNameParser
	{
		string ConvertToIndexNameFormat(string indexName);
	}
}