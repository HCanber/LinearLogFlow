namespace LinearLogFlow.Config
{
	public interface IConfigProvider
	{
		ConfigAndName GetConfig();
		string GetNotFoundMessage();
	}
}