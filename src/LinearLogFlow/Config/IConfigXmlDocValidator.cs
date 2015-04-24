using System.Xml.Linq;

namespace LinearLogFlow.Config
{
	public interface IConfigXmlDocValidator
	{
		void ValidateConfigDocument(XDocument configDoc, string configFileName);
	}
}