namespace LinearLogFlow.Config
{
	public class JsonWithPath
	{
		private readonly string _path;
		private readonly string _json;

		public JsonWithPath(string path, string json)
		{
			_path = path;
			_json = json;
		}

		public string Path { get { return _path; } }
		public string Json { get { return _json; } }
	}
}