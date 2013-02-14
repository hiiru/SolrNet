using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SolrNet.Impl.FormatParser.JavaBin
{
	[StructLayout(LayoutKind.Explicit)]
		public struct Converter
		{
			[FieldOffset(0)]
			public readonly int Int;

			[FieldOffset(0)]
			public readonly float Float;
		
			[FieldOffset(1)]
			public readonly double Double;

			[FieldOffset(1)]
			public readonly long Long;

			public Converter(float val) : this()
			{
				Float = val;
			}
			public Converter(int val) : this()
			{
				Int = val;
			}
			public Converter(double val) : this()
			{
				Double = val;
			}
			public Converter(long val) : this()
			{
				Long = val;
			}

			public static int ToInt(float val)
			{
				return new Converter(val).Int;
			}

			public static float ToFloat(int val)
			{
				return new Converter(val).Float;
			}
		
			public static double ToDouble(long val)
			{
				return new Converter(val).Int;
			}
			public static long ToLong(double val)
			{
				return new Converter(val).Int;
			}
		}
}
