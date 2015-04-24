using System;
using System.Text;

namespace LinearLogFlow.Serialization
{
	public static class BaseEncoder
	{
		public static string Int64ToBaseString(long value, string alphabet)
		{
			if(value == 0) return alphabet[0].ToString();
			var alphabetLength = alphabet.Length;
			var isNegative = value < 0;
			var lowIndex = 0;
			var sb = new StringBuilder();
			if(isNegative)
			{
				sb.Append("-");
				lowIndex = 1;
			}
			while(value != 0)
			{
				var alphabetPosition = Math.Abs((int)(value % alphabetLength));
				value = value / alphabetLength;
				sb.Append(alphabet[alphabetPosition]);
			}
			Reverse(sb, lowIndex, sb.Length - 1);
			return sb.ToString();
		}

		public static string UInt64ToBaseString(ulong value, string alphabet)
		{
			if(value == 0) return alphabet[0].ToString();
			var alphabetLength = (ulong)alphabet.Length;

			var sb = new StringBuilder();
			while(value != 0)
			{
				var alphabetPosition = (int)(value % alphabetLength);
				value = value / alphabetLength;
				sb.Append(alphabet[alphabetPosition]);
			}
			Reverse(sb);
			return sb.ToString();
		}

		public static string UInt32ToBaseString(uint value, string alphabet, bool reversed = true)
		{
			if(value == 0) return alphabet[0].ToString();
			var alphabetLength = (uint)alphabet.Length;

			var sb = new StringBuilder();
			while(value != 0)
			{
				var alphabetPosition = (int)(value % alphabetLength);
				value = value / alphabetLength;
				sb.Append(alphabet[alphabetPosition]);
			}
			if(!reversed)
				Reverse(sb);
			return sb.ToString();
		}

		private static void Reverse(StringBuilder builder)
		{
			Reverse(builder, 0, builder.Length - 1);
		}

		private static void Reverse(StringBuilder builder, int lowIndex, int highIndex)
		{
			while(lowIndex <= highIndex)
			{
				char c = builder[lowIndex];
				builder[lowIndex] = builder[highIndex];
				builder[highIndex] = c;
				lowIndex++;
				highIndex--;
			}
		}
	}
}