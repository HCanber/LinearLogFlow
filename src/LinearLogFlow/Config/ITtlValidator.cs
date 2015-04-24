namespace LinearLogFlow.Config
{
	public interface ITtlValidator
	{
		bool IsValidElasticTime(string time);
	}
}