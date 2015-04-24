using System;
using System.Text;

namespace LinearLogFlow.Config
{
	public class IndexNameParser : IIndexNameParser
	{
		public string ConvertToIndexNameFormat(string indexName)
		{
			var currentIndex = 0;
			var lastIndex = indexName.Length - 1;
			var sb = new StringBuilder();
			var state = State.Normal;
			while(currentIndex <= lastIndex)
			{
				var c = indexName[currentIndex];
				switch(c)
				{
					case '{':
						switch(state)
						{
							case State.Normal:
								state = State.NormalLeftBraceFound;
								break;
							case State.NormalLeftBraceFound:
								sb.Append("\\{");
								state = State.Normal;
								break;
							default:
								throw new ParseException(currentIndex, indexName, "Unexpected character '{' found");
						}
						break;
					case '}':
						switch(state)
						{
							case State.Normal:
								state = State.NormalRightBraceFound;
								break;
							case State.NormalLeftBraceFound:
								state = State.Normal;
								break;
							case State.InsideBrace:
								state = State.Normal;
								break;
							case State.NormalRightBraceFound:
								sb.Append("\\}");
								state = State.Normal;
								break;
						}
						break;
					default:
						if(c == '\\')
							return indexName;
						switch(state)
						{
							case State.Normal:
								sb.Append('\\').Append(c);
								break;
							case State.NormalLeftBraceFound:
							case State.InsideBrace:
								sb.Append(c);
								state = State.InsideBrace;
								break;
							case State.NormalRightBraceFound:
								throw new ParseException(currentIndex, indexName, String.Format("Unexpected character '{0}' found. Expected '}}'", c));
						}
						break;
				}
				currentIndex++;

			}
			if(state != State.Normal)
			{
				throw new ParseException(currentIndex, indexName, "Unexpected end of input. Missing '}'.");
			}
			return sb.ToString();
		}

		private enum State { Normal, NormalLeftBraceFound, InsideBrace, NormalRightBraceFound }
	}
}