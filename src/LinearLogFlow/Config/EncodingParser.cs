using System;
using System.Collections.Generic;
using System.Text;

namespace LinearLogFlow.Config
{
	public class EncodingParser : IEncodingParser
	{
		private static readonly Dictionary<string, Encoding> _supportedEncodings = new Dictionary<string, Encoding>(StringComparer.OrdinalIgnoreCase)
		{
			{"ascii", Encoding.ASCII},
			{"unicode", Encoding.Unicode},
			{"utf8", Encoding.UTF8},
			{"utf-8", Encoding.UTF8},
			{"utf16", Encoding.Unicode},
			{"utf-16", Encoding.Unicode},
			{"utf16be", Encoding.Unicode},
			{"utf-16be", Encoding.Unicode},
			{"utf16le", Encoding.BigEndianUnicode},
			{"utf-16le", Encoding.BigEndianUnicode},
			{"utf32", Encoding.UTF32},
			{"utf-32", Encoding.UTF32},
			{"utf32le", Encoding.UTF32},
			{"utf-32le", Encoding.UTF32},
			{"utf32be", new UTF32Encoding(true,true)},
			{"utf-32be", new UTF32Encoding(true,true)},
			{"utf7", Encoding.UTF7},
			{"utf-7", Encoding.UTF7},
		};


		public Encoding ParseEncoding(string encoding)
		{
			Encoding enc;
			if(_supportedEncodings.TryGetValue(encoding, out enc))
				return enc;
			throw new Exception(string.Format("Invalid encoding: \"{0}\". Allowed values are: {1}.", encoding, string.Join(", ", _supportedEncodings.Keys)));
		}


	}
}