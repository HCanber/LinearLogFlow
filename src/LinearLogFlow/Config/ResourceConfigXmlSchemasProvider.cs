using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace LinearLogFlow.Config
{
	public class ResourceConfigXmlSchemasProvider : IConfigXmlSchemasProvider
	{
		private static readonly Lazy<XmlSchemaSet> _lazySchema = new Lazy<XmlSchemaSet>(CreateXmlSchemaSet);
		public XmlSchemaSet GetSchemaSet()
		{
			return _lazySchema.Value;
		}

		private static XmlSchemaSet CreateXmlSchemaSet()
		{
			var schemaFileContent = GetResource("LogFlowConfig.xsd");
			var schemas = new XmlSchemaSet();
			var schemaDocument = XmlReader.Create(new StringReader(schemaFileContent));
			schemas.Add("", schemaDocument);
			return schemas;
		}

		private static string GetResource(string resourceName)
		{
			var thisType = typeof(ResourceConfigXmlSchemasProvider);
			var assembly = thisType.Assembly;
			using(var stream = assembly.GetManifestResourceStream(thisType.Namespace + "." + resourceName))
			using(var reader = new StreamReader(stream))
			{
				var content = reader.ReadToEnd();
				return content;
			}
		}
	}
}