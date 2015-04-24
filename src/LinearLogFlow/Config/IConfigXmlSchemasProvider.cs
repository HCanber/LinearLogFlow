using System.Xml.Schema;

namespace LinearLogFlow.Config
{
	public interface IConfigXmlSchemasProvider
	{
		XmlSchemaSet GetSchemaSet();
	}
}