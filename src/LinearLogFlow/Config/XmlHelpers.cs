using System;
using System.Xml;
using System.Xml.Linq;

namespace LinearLogFlow.Config
{
	public static class XmlHelpers
	{
		public static string AttributeValue(this XElement element, string attributeName, string defaultValue = null, bool trim=true)
		{
			var xAttribute = element.Attribute(attributeName);
			return xAttribute == null ? defaultValue : trim ? xAttribute.Value.Trim() : xAttribute.Value;
		}

		public static bool AttributeBoolValue(this XElement element, string attributeName, bool defaultValue = false)
		{
			return AttributeValue(element, attributeName, v => string.Equals("true", v, StringComparison.OrdinalIgnoreCase), defaultValue);
		}

		public static T AttributeValue<T>(this XElement element, string attributeName, Func<string, T> converter, T defaultValue = default(T), bool trim = true)
		{
			var xAttribute = element.Attribute(attributeName);
			if(xAttribute == null) return defaultValue;

			return converter(trim ? xAttribute.Value.Trim() : xAttribute.Value);
		}

		public static string FormatXmlMessage(XObject obj, string filename, string format, params object[] args)
		{
			var element = obj as XElement;
			if(element != null)
				return FormatXmlMessage(element, filename, format, args);
			else
			{
				var attribute = obj as XAttribute;
				return FormatXmlMessage(attribute, filename, format, args);
			}
		}

		public static string FormatXmlMessage(XElement element, string filename, string format, params object[] args)
		{
			return FormatXmlMessage((IXmlLineInfo)element, filename, element.Name.LocalName, null, format, args);
		}

		public static string FormatXmlMessage(XAttribute attribute, string filename, string format, params object[] args)
		{
			return FormatXmlMessage((IXmlLineInfo)attribute, filename, null, attribute.Name.LocalName, format, args);
		}

		private static string FormatXmlMessage(IXmlLineInfo lineInfo, string filename, string elementName, string attributeName, string format, params object[] args)
		{
			var message = args == null || args.Length == 0 ? format : string.Format(format, args);
			var nameInfo = elementName != null ? " in element \"" + elementName + "\" on " : attributeName != null ? "in attribute \"" + attributeName + "\" on " : "";
			if(lineInfo.HasLineInfo())
				if(filename != null)
					return string.Format("{0} {4}Line {1}, Pos {2} in file \"{3}\"", message, lineInfo.LineNumber, lineInfo.LinePosition, filename, nameInfo);
				else
					return string.Format("{0} {3}Line {1}, Pos {2}", message, lineInfo.LineNumber, lineInfo.LinePosition, nameInfo);
			if(filename != null)
				return string.Format("{0} {2}in file \"{1}\"", message, filename, nameInfo);
			return message;
		}
	}
}