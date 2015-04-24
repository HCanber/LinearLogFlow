using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinearLogFlow.Config
{
	public class XmlConfigParser : IConfigParser
	{
		//private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly IConfigXmlSchemasProvider _configXmlSchemasProvider;
		private readonly IEncodingParser _encodingParser;
		private readonly ITtlValidator _ttlValidator;
		private readonly IConfigXmlDocValidator _configXmlDocValidator;
		private readonly IIndexNameParser _indexNameParser;

		public XmlConfigParser(IConfigXmlSchemasProvider configXmlSchemasProvider, IEncodingParser encodingParser, ITtlValidator ttlValidator, IConfigXmlDocValidator configXmlDocValidator, IIndexNameParser indexNameParser)
		{
			_configXmlSchemasProvider = configXmlSchemasProvider;
			_encodingParser = encodingParser;
			_ttlValidator = ttlValidator;
			_configXmlDocValidator = configXmlDocValidator;
			_indexNameParser = indexNameParser;
		}

		public List<ServerConfig> GetConfigFromContent(string configContent, string configName)
		{
			var parser = new InnerParser(_encodingParser, _ttlValidator, _configXmlDocValidator, _indexNameParser);
			return parser.GetConfigFromContent(configContent, configName);
		}


		// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
		// ReSharper disable once MemberCanBePrivate.Global
		protected class InnerParser
		{
			//private static readonly Logger _log = LogManager.GetCurrentClassLogger();
			private readonly IEncodingParser _encodingParser;
			private readonly ITtlValidator _ttlValidator;
			private readonly IConfigXmlDocValidator _configXmlDocValidator;
			private readonly IIndexNameParser _indexNameParser;
			private readonly Dictionary<string, bool> _seenLogFlowNames;

			public InnerParser(IEncodingParser encodingParser, ITtlValidator ttlValidator, IConfigXmlDocValidator configXmlDocValidator, IIndexNameParser indexNameParser)
			{
				_encodingParser = encodingParser;
				_ttlValidator = ttlValidator;
				_configXmlDocValidator = configXmlDocValidator;
				_indexNameParser = indexNameParser;
				_seenLogFlowNames = new Dictionary<string, bool>();
			}

			public List<ServerConfig> GetConfigFromContent(string configContent, string configName)
			{
				var configDoc = CreateXmlDocment(configContent, configName);
				ValidateConfigDocument(configName, configDoc);

				return ParseDocument(configDoc, configName);
			}

			protected virtual List<ServerConfig> ParseDocument(XDocument configDoc, string configName)
			{
				const string rootElementName = "config";
				var rootElement = configDoc.Element(rootElementName);
				if(rootElement == null) throw new Exception("Unexpected! No " + rootElementName + " element found. Schema validation isn't working.");

				const string serverElementName = "server";
				var serverElements = rootElement.Elements().ToList();
				return ParseServerElements(serverElements, configName);
			}

			protected virtual void ValidateConfigDocument(string configName, XDocument configDoc)
			{
				_configXmlDocValidator.ValidateConfigDocument(configDoc, configName);
			}

			protected virtual List<ServerConfig> ParseServerElements(List<XElement> serverElements, string configName)
			{
				var alreadyHandledUris = new HashSet<Uri>();
				var serverConfigs = new List<ServerConfig>();
				foreach(var serverElement in serverElements)
				{
					var serverConfig = ParseServerElementTree(configName, serverElement, alreadyHandledUris);
					serverConfigs.Add(serverConfig);
				}

				return serverConfigs;
			}

			protected virtual ServerConfig ParseServerElementTree(string configName, XElement serverElement, HashSet<Uri> alreadyHandledUris)
			{
				var serverConfig = ParseServerElementOnly(serverElement, alreadyHandledUris, configName);
				const string indexElementName = "index";
				var indexConfigByName = serverConfig.IndexConfigByName;
				var indexElements = serverElement.Elements().ToList();

				ParserIndexElements(indexElements, indexConfigByName, configName);
				return serverConfig;
			}

			protected virtual ServerConfig ParseServerElementOnly(XElement serverElement, ISet<Uri> alreadyHandledUris, string configName)
			{
				const string uriAttributeName = "uri";
				var uriAttribute = serverElement.Attribute(uriAttributeName);
				var uris = new List<Uri>();
				foreach(var uriStr in uriAttribute.Value.Split('|'))
				{
					Uri uri;
					if(!Uri.TryCreate(uriStr, UriKind.Absolute, out uri))
					{
						var message = XmlHelpers.FormatXmlMessage(uriAttribute, configName, "Invalid {0} \"{1}\"", uriAttributeName, uriStr);
						throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)uriAttribute).LineNumber);
					}
					if(alreadyHandledUris.Contains(uri))
					{
						var message = XmlHelpers.FormatXmlMessage(uriAttribute, configName, "The uri \"{0}\" has already been defined", uri);
						throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)uriAttribute).LineNumber);
					}
					alreadyHandledUris.Add(uri);
					uris.Add(uri);
				}

				const string isClusterAttribteName = "isCluster";
				var isCluster = serverElement.AttributeBoolValue(isClusterAttribteName);
				if(!isCluster && uris.Count > 1)
				{
					var message = XmlHelpers.FormatXmlMessage(uriAttribute, configName, "When defining more than one uri {0}=\"true\" must be set", isClusterAttribteName);
					throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)serverElement).LineNumber);
				}
				var serverConfig = new ServerConfig() { IsCluster = isCluster, Uris = uris };
				return serverConfig;
			}







			protected virtual void ParserIndexElements(List<XElement> indexElements, Dictionary<string, IndexConfig> indexConfigByName, string configName)
			{
				foreach(var indexElement in indexElements)
				{
					ParseIndexElementTree(indexElement, indexConfigByName, configName);
				}
			}

			protected virtual void ParseIndexElementTree(XElement indexElement, Dictionary<string, IndexConfig> indexConfigByName, string configName)
			{
				string defaultTtl;
				Encoding defaultEncoding;
				string defaultMappingJson;
				string indexName;
				var indexConfig = ParseIndexElementOnly(indexElement, configName, out indexName, out defaultTtl, out defaultEncoding, out defaultMappingJson);
				IndexConfig existingIndexConfig;
				Dictionary<string, IndexTypeConfig> indexTypeConfigByType;
				if(indexConfigByName.TryGetValue(indexName, out existingIndexConfig))
				{
					MergeIndexConfigIntoExisting(indexConfig, existingIndexConfig, configName, indexElement);
					indexTypeConfigByType = existingIndexConfig.IndexTypeConfigByType;
				}
				else
				{
					HandleNewIndexConfig(configName, indexConfig, indexElement);
					indexConfigByName.Add(indexName, indexConfig);
					indexTypeConfigByType = indexConfig.IndexTypeConfigByType;
				}

				var logElements = indexElement.Elements().ToList();
				ParseLogElements(logElements, indexTypeConfigByType, defaultEncoding, defaultTtl, defaultMappingJson, configName);
			}

			protected virtual IndexConfig ParseIndexElementOnly(XElement indexElement, string configName, out string indexName, out string defaultTtl, out Encoding defaultEncoding, out string defaultMappingJson)
			{
				indexName = ConvertToIndexNameFormat(indexElement.AttributeValue(XmlElementNames.Index.IndexName));

				var indexTemplatePath = indexElement.AttributeValue(XmlElementNames.Index.IndexTemplate, v => Path.GetFullPath(v));
				IndexConfig indexConfig = new IndexConfig
				{
					Index = indexName,
					IndexTemplatePath = indexTemplatePath,
				};
				defaultTtl = GetTtl(indexElement.Attribute(XmlElementNames.Index.DefaultTtl), configName);
				defaultEncoding = GetEncoding(indexElement.Attribute(XmlElementNames.Index.DefaultEncoding), configName);
				var defaultMappingAttribute = indexElement.Attribute(XmlElementNames.Index.DefaultMapping);
				var mappingJson = GetValidJson(defaultMappingAttribute, configName, "default mapping file");
				defaultMappingJson = mappingJson;

				return indexConfig;
			}

			protected virtual void HandleNewIndexConfig(string configName, IndexConfig indexConfig, XElement indexElement)
			{
				var indexTemplatePath = indexConfig.IndexTemplatePath;
				if(indexTemplatePath != null)
				{
					var json = GetValidJson(indexTemplatePath, configName, "index template file", indexElement);
					indexConfig.IndexTemplateJson = json;
				}
			}

			protected virtual void MergeIndexConfigIntoExisting(IndexConfig newIndexConfig, IndexConfig existingIndexConfig, string configName, XElement indexElement)
			{
				var newIndexTemplatePath = newIndexConfig.IndexTemplatePath;
				var existingIndexTemplatePath = existingIndexConfig.IndexTemplatePath;
				if(newIndexTemplatePath != null && existingIndexTemplatePath != null)
				{
					if(!string.Equals(newIndexTemplatePath, existingIndexTemplatePath, StringComparison.OrdinalIgnoreCase))
					{
						var message = XmlHelpers.FormatXmlMessage(indexElement, configName, "Indexes on the same server must share the same {0}. A different {0} was specified", XmlElementNames.Index.IndexTemplate);
						throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)indexElement).LineNumber);
					}
				}
			}





			protected virtual void ParseLogElements(IReadOnlyCollection<XElement> logElements, Dictionary<string, IndexTypeConfig> indexTypeConfigByType, Encoding defaultEncoding, string defaultTtl, string defaultMappingJson, string configName)
			{
				foreach(var logElement in logElements)
				{
					ParseLogElement(logElement, indexTypeConfigByType, defaultEncoding, defaultTtl, defaultMappingJson, configName);
				}
			}

			protected virtual void ParseLogElement(XElement logElement, Dictionary<string, IndexTypeConfig> indexTypeConfigByType, Encoding defaultEncoding, string defaultTtl, string defaultMappingJson, string configName)
			{
				var indexTypeConfig = ParseLogElementOnly(logElement, defaultEncoding, defaultTtl, configName);
				var type = indexTypeConfig.Type;
				IndexTypeConfig existingTypeConfig;
				if(indexTypeConfigByType.TryGetValue(type, out existingTypeConfig))
				{
					MergeIndexTypeConfigIntoExisting(indexTypeConfig, existingTypeConfig, configName, logElement);
				}
				else
				{
					HandleNewIndexTypeConfig(indexTypeConfig, logElement, defaultMappingJson, configName);
					indexTypeConfigByType.Add(type, indexTypeConfig);
				}
			}

			protected virtual IndexTypeConfig ParseLogElementOnly(XElement logElement, Encoding defaultEncoding, string defaultTtl, string configName)
			{
				var indexTypeConfig = ParseIndexTypeFromLogElementOnly(logElement);
				var logConfig = ParseLogInfoFromLogElementOnly(logElement, configName, indexTypeConfig.Type);
				logConfig.Encoding = logConfig.Encoding ?? defaultEncoding;
				logConfig.Ttl = logConfig.Ttl ?? defaultTtl;
				indexTypeConfig.Logs.Add(logConfig);
				return indexTypeConfig;
			}


			protected virtual IndexTypeConfig ParseIndexTypeFromLogElementOnly(XElement logElement)
			{
				const string typeAttributeName = XmlElementNames.Log.Type;
				const string mappingAttributeName = XmlElementNames.Log.Mapping;

				var type = logElement.AttributeValue(typeAttributeName);

				var mappingPath = logElement.AttributeValue(mappingAttributeName, v => Path.GetFullPath(v));
				var indexTypeConfig = new IndexTypeConfig
				{
					Type = type,
					MappingPath = mappingPath,
				};
				return indexTypeConfig;
			}


			protected virtual LogConfig ParseLogInfoFromLogElementOnly(XElement logElement, string configName, string type)
			{
				var name = logElement.AttributeValue(XmlElementNames.Log.Name);
				var nameFromAttribute = name != null;
				if(!nameFromAttribute)
					name = type;

				bool seenWasNameAttribute;
				if(_seenLogFlowNames.TryGetValue(name, out seenWasNameAttribute))
				{
					string message;
					if(nameFromAttribute)
						message = XmlHelpers.FormatXmlMessage(logElement, configName, string.Format("The {0}=\"{1}\" is not unique. It was specified as {2} previously. Change the {0} attribute ", XmlElementNames.Log.Name, name, seenWasNameAttribute ? XmlElementNames.Log.Name + " attribute " : XmlElementNames.Log.Type + " attribute, which became the name for that log, "));
					else
						message = XmlHelpers.FormatXmlMessage(logElement, configName, string.Format("No {0} specified so {3}=\"{1}\" is used instead. But there already exists a log with that name specified on its {2} attribute. Names must be unique. Add a {0} attribute ", XmlElementNames.Log.Name, name, seenWasNameAttribute ? XmlElementNames.Log.Name : XmlElementNames.Log.Type, XmlElementNames.Log.Type));

					throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)logElement).LineNumber);
				}
				_seenLogFlowNames.Add(name, nameFromAttribute);
				var logConfig = new LogConfig()
				{
					LogFlowName = logElement.AttributeValue(XmlElementNames.Log.Name, type),
					Path = logElement.AttributeValue(XmlElementNames.Log.Path),
					AddSourceField = logElement.AttributeBoolValue(XmlElementNames.Log.AddSource),
					Encoding = GetEncoding(logElement.Attribute(XmlElementNames.Log.Encoding), configName),
					Ttl = GetTtl(logElement.Attribute(XmlElementNames.Log.Ttl), configName),
					TimestampPropertyNames = logElement.AttributeValue(XmlElementNames.Log.Timestamp, v=>v.Split(new []{'|'},StringSplitOptions.RemoveEmptyEntries))
				};
				return logConfig;
			}

			protected virtual void HandleNewIndexTypeConfig(IndexTypeConfig indexTypeConfig, XElement logElement, string defaultMappingJson, string configName)
			{
				var mappingJson = GetValidJson(indexTypeConfig.MappingPath, configName, "mapping file", logElement) ?? defaultMappingJson;
				if(mappingJson != null)
					indexTypeConfig.MappingJson = "{\"" + indexTypeConfig.Type + "\":" + mappingJson + "}";
			}

			protected virtual void MergeIndexTypeConfigIntoExisting(IndexTypeConfig newIndexTypeConfig, IndexTypeConfig existingTypeConfig, string configName, XElement logElement)
			{
				var newPath = newIndexTypeConfig.MappingPath;
				var existingPath = existingTypeConfig.MappingPath;
				if(newPath != null && existingPath != null)
				{
					if(!string.Equals(newPath, existingPath, StringComparison.OrdinalIgnoreCase))
					{
						var message = XmlHelpers.FormatXmlMessage(logElement, configName, "Mappings for the same index type on the same server must point to same file. A different path was specified");
						throw new ConfigurationErrorsException(message, configName, ((IXmlLineInfo)logElement).LineNumber);
					}
				}
				existingTypeConfig.Logs.AddRange(newIndexTypeConfig.Logs);
			}


			protected virtual XDocument CreateXmlDocment(string configContent, string configFileName)
			{
				XDocument configDoc;
				try
				{
					configDoc = XDocument.Load(new StringReader(configContent), LoadOptions.SetLineInfo);
				}
				catch(Exception e)
				{
					throw new Exception(string.Format("An error occurred while reading the file {0}", configFileName), e);
				}
				return configDoc;
			}

			protected virtual string ConvertToIndexNameFormat(string indexName)
			{
				return _indexNameParser.ConvertToIndexNameFormat(indexName);
			}

			protected virtual Encoding GetEncoding(XAttribute encodingAttribute, string configFileName)
			{
				if(encodingAttribute == null) return null;

				try
				{
					return _encodingParser.ParseEncoding(encodingAttribute.Value);
				}
				catch(Exception e)
				{
					var message = XmlHelpers.FormatXmlMessage(encodingAttribute, configFileName, "Invalid encoding: \"{0}\"", encodingAttribute.Value);
					throw new ConfigurationErrorsException(message, e, configFileName, ((IXmlLineInfo)encodingAttribute).LineNumber);
				}
			}


			protected string GetValidJson(XAttribute jsonFilePathAttribute, string filename, string fileType)
			{
				if(jsonFilePathAttribute == null) return null;
				return GetValidJson(jsonFilePathAttribute.Value, filename, fileType, jsonFilePathAttribute);
			}


			protected virtual string GetValidJson(string jsonFilePath, string filename, string fileType, XObject xobject)
			{
				if(string.IsNullOrEmpty(jsonFilePath)) return null;
				jsonFilePath = Path.GetFullPath(jsonFilePath);
				if(!File.Exists(jsonFilePath))
				{
					var message = XmlHelpers.FormatXmlMessage(xobject, filename, "Could not find the {0} \"{1}\"", fileType, jsonFilePath);
					throw new ConfigurationErrorsException(message, filename, ((IXmlLineInfo)xobject).LineNumber);
				}
				var indexTemplateJson = File.ReadAllText(jsonFilePath);
				try
				{
					var json = (JObject)JsonConvert.DeserializeObject(indexTemplateJson);

					return indexTemplateJson;
				}
				catch(Exception e)
				{
					var message = XmlHelpers.FormatXmlMessage(xobject, filename, "Invalid json in {0} \"{1}\" which was configured on", fileType, jsonFilePath);
					throw new ConfigurationErrorsException(message, e, filename, ((IXmlLineInfo)xobject).LineNumber);
				}
			}


			protected virtual string GetTtl(XAttribute ttlAttribute, string configFileName)
			{
				string ttl = null;
				if(ttlAttribute != null)
				{
					ttl = ttlAttribute.Value;
					if(!_ttlValidator.IsValidElasticTime(ttl))
					{
						var message = XmlHelpers.FormatXmlMessage(ttlAttribute, configFileName, "Invalid ttl=\"{0}\"", ttl);
						throw new ConfigurationErrorsException(message, configFileName, ((IXmlLineInfo)ttlAttribute).LineNumber);
					}
				}
				return ttl;
			}

			protected static class XmlElementNames
			{
				public static class Index
				{
					public const string IndexName = "indexName";
					public const string DefaultTtl = "defaultTtl";
					public const string DefaultMapping = "defaultMapping";
					public const string DefaultEncoding = "defaultEncoding";
					public const string IndexTemplate = "indexTemplate";
				}

				public static class Log
				{
					public const string Type = "type";
					public const string Path = "path";
					public const string Encoding = "encoding";
					public const string Mapping = "mapping";
					public const string Ttl = "ttl";
					public const string AddSource = "addSource";
					public const string Name = "name";
					public const string Timestamp = "timestamp";
				}
			}
		}
	}
}