using System;
using System.Collections.Generic;

namespace LinearLogFlow.Serialization
{
	public static class ValidForHttpEncoder
	{
		private const string Alphabet = "0123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ";

		public static string ToString(Guid guid)
		{
			var byteArray = guid.ToByteArray();
			var firstWord = GetULong64(byteArray, 0);
			var secondWord = GetULong64(byteArray, 1);
			var httpString = ToString(firstWord) + ToString(secondWord);
			return httpString;
		}

		private static ulong GetULong64(IList<byte> byteArray, int wordIndex)
		{
			var firstIndex = wordIndex * 8;
			return ((ulong)byteArray[firstIndex] << (7 * 8)) + ((ulong)byteArray[firstIndex + 1] << (6 * 8)) + ((ulong)byteArray[firstIndex + 2] << (5 * 8)) + ((ulong)byteArray[firstIndex + 3] << (4 * 8)) +
						 ((ulong)byteArray[firstIndex + 4] << (3 * 8)) + ((ulong)byteArray[firstIndex + 5] << (2 * 8)) + ((ulong)byteArray[firstIndex + 6] << (1 * 8)) + ((ulong)byteArray[firstIndex + 7]);
		}

		public static string ToString(long value)
		{
			return BaseEncoder.Int64ToBaseString(value, Alphabet);
		}

		public static string ToString(ulong value)
		{
			return BaseEncoder.UInt64ToBaseString(value, Alphabet);
		}

		public static string ToString(uint value, bool reversed = true)
		{
			return BaseEncoder.UInt32ToBaseString(value, Alphabet, reversed);
		}
	}
}