using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace LinearLogFlow.Config
{
	[Serializable]
	[DebuggerDisplay("{Message,nq} string={Value}, Index={Index}")]
	public class ParseException : Exception
	{
		private readonly int _index;
		private readonly string _value;
		// Guidelines: http://msdn.microsoft.com/en-us/library/vstudio/ms229064(v=vs.100).aspx

		// This constructor is needed for serialization.
		protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }


		public ParseException(int index, string value, string message)
			: base(message)
		{
			_index = index;
			_value = value;
		}

		public ParseException(int index, string value, string message, Exception inner)
			: base(message, inner)
		{
			_index = index;
			_value = value;
		}

		public int Index { get { return _index; } }

		public string Value { get { return _value; } }

		public override string ToString()
		{
			return string.Format("{0}, Index: {1}, Value: {2}", base.ToString(), _index, _value);
		}
	}
}