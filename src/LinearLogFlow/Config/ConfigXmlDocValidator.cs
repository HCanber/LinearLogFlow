using System;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NLog;

namespace LinearLogFlow.Config
{
	public class ConfigXmlDocValidator : IConfigXmlDocValidator
	{
		private readonly IConfigXmlSchemasProvider _configXmlSchemasProvider;
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public ConfigXmlDocValidator(IConfigXmlSchemasProvider configXmlSchemasProvider)
		{
			_configXmlSchemasProvider = configXmlSchemasProvider;
		}

		public void ValidateConfigDocument(XDocument configDoc, string configFileName)
		{
			var schemas = _configXmlSchemasProvider.GetSchemaSet();

			try
			{
				configDoc.Validate(schemas, (sender, args) =>
				{
					var xObject = sender as XObject;
					if(args.Severity == XmlSeverityType.Warning)
					{
						if(xObject != null)
						{
							var lineInfo = (IXmlLineInfo)xObject;
							var name = xObject is XElement ? ((XElement)xObject).Name.LocalName : xObject is XAttribute ? ((XAttribute)xObject).Name.LocalName : "";
							_log.Warn("Validation warning in config file {0} {2} on line {3}, pos {4}: {1}", configFileName, args.Message, xObject.NodeType + " " + name, lineInfo.LineNumber, lineInfo.LinePosition);
						}
						else
							_log.Warn("Validation warning in config file {0}: {1}", configFileName, args.Message);
					}
					else if(xObject != null)
					{
						var lineInfo = (IXmlLineInfo)xObject;
						var name = xObject is XElement ? ((XElement)xObject).Name.LocalName : xObject is XAttribute ? ((XAttribute)xObject).Name.LocalName : "";
						throw new Exception(string.Format("Schema validation failed in config file {0} {2} on line {3}, pos {4}: {1}", configFileName, args.Message, xObject.NodeType + " " + name, lineInfo.LineNumber, lineInfo.LinePosition), args.Exception);
					}
					throw args.Exception;
				});
			}
			catch(Exception e)
			{
				throw new ConfigurationErrorsException("Validation error for config file " + configFileName, e);
			}
		}

	}
}