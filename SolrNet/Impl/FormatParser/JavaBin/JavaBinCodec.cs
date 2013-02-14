#region license

// This File is based on the JavaBinCodec created by the Terry Liang and the easynet Project (http://easynet.codeplex.com).
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SolrNet.Exceptions;

namespace SolrNet.Impl.FormatParser.JavaBin
{
	internal class JavaBinCodec : IDisposable
	{
		private static readonly DateTime utcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public const byte
				  NULL = 0,
				  BOOL_TRUE = 1,
				  BOOL_FALSE = 2,
				  BYTE = 3,
				  SHORT = 4,
				  DOUBLE = 5,
				  INT = 6,
				  LONG = 7,
				  FLOAT = 8,
				  DATE = 9,
				  MAP = 10,
				  SOLRDOC = 11,
				  SOLRDOCLST = 12,
				  BYTEARR = 13,
				  ITERATOR = 14,
				  END = 15,
				  TAG_AND_LEN = (byte)(1 << 5),
				  STR = (byte)(1 << 5),
				  SINT = (byte)(2 << 5),
				  SLONG = (byte)(3 << 5),
				  ARR = (byte)(4 << 5), //
				  ORDERED_MAP = (byte)(5 << 5),
				  NAMED_LST = (byte)(6 << 5),
				  EXTERN_STRING = (byte)(7 << 5);

		private static byte VERSION = 2;
		protected FastOutputStream daos;

		private byte version;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				daos.Close();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public object Unmarshal(Stream inputStream)
		{
			FastInputStream dis = new FastInputStream(inputStream);
			version = (byte)dis.ReadByte();
			if (version != VERSION)
			{
				throw new ApplicationException("Invalid version or the data in not in 'javabin' format");
			}
			return ReadVal(dis);
		}

		public SolrResponseDocumentNode UnmarshalDocument(Stream inputStream)
		{
			FastInputStream dis = new FastInputStream(inputStream);
			version = (byte)dis.ReadByte();
			if (version != VERSION)
			{
				throw new ApplicationException("Invalid version or the data in not in 'javabin' format");
			}
			return ReadNode(dis) as SolrResponseDocumentNode;
		}

		public List<SolrResponseDocumentNode> ReadNamedList(FastInputStream dis)
		{
			var list = new List<SolrResponseDocumentNode>();
			int sz = ReadSize(dis);
			for (int i = 0; i < sz; i++)
			{
				string name = (string)ReadVal(dis);
				var node = ReadNode(dis, name) as SolrResponseDocumentNode;
				if (node != null)
					list.Add(node);
			}
			return list;
		}

		//public NamedList ReadNamedList(FastInputStream dis, bool isOrdered = false)
		//{
		//	int sz = ReadSize(dis);
		//	NamedList nl = new NamedList();
		//	nl.IsOrdered = isOrdered;
		//	for (int i = 0; i < sz; i++)
		//	{
		//		string name = (string)ReadVal(dis);
		//		object val = ReadVal(dis);
		//		nl.Add(name, val);
		//	}
		//	return nl;
		//}

		protected static readonly object END_OBJ = new object();

		protected byte tagByte;

		public object ReadNode(FastInputStream dis, string name = "")
		{
			SolrResponseDocumentNode node = new SolrResponseDocumentNode(name);
			tagByte = (byte)dis.ReadByte();

			switch (tagByte >> 5)
			{
				case STR >> 5:
					node.SolrType = SolrResponseDocumentNodeType.String;
					node.Value = ReadStr(dis);
					break;

				case EXTERN_STRING >> 5:
					node.SolrType = SolrResponseDocumentNodeType.String;
					node.Value = ReadExternString(dis);
					break;

				case SINT >> 5:
					node.SolrType = SolrResponseDocumentNodeType.Int;
					node.Value = ReadSmallInt(dis).ToString();
					break;

				case SLONG >> 5:
					node.SolrType = SolrResponseDocumentNodeType.Int;
					node.Value = ReadSmallLong(dis).ToString();
					break;

				case ARR >> 5:
					return ReadArray(dis);
				case ORDERED_MAP >> 5:
				case NAMED_LST >> 5:
					node.SolrType = SolrResponseDocumentNodeType.Array;
					if (node.Collection == null)
						node.Collection = new List<SolrResponseDocumentNode>();
					node.Collection.AddRange(ReadNamedList(dis));
					break;
			}
			switch (tagByte)
			{
				case NULL:
					return null;
				case DATE:
					node.SolrType = SolrResponseDocumentNodeType.Date;
					DateTime date;
					try
					{
						date = utcDateTime.AddMilliseconds(dis.ReadLong()).ToLocalTime();
					}
					catch
					{
						date = new DateTime();
					}
					node.RawValue = date;
					//node.Value = DateTime.SpecifyKind(date, DateTimeKind.Local).ToString("yyyy-MM-ddTHH:mm:ssK");
					break;

				case INT:
					node.SolrType = SolrResponseDocumentNodeType.Int;
					node.Value = dis.ReadInt().ToString();
					break;

				case BOOL_TRUE:
					node.SolrType = SolrResponseDocumentNodeType.Boolean;
					node.Value = "true";
					break;

				case BOOL_FALSE:
					node.SolrType = SolrResponseDocumentNodeType.Boolean;
					node.Value = "false";
					break;

				case FLOAT:
					node.SolrType = SolrResponseDocumentNodeType.Float;
					node.Value = dis.ReadFloat().ToString();
					break;

				case DOUBLE:
					node.SolrType = SolrResponseDocumentNodeType.Float;
					node.Value = dis.ReadDouble().ToString();
					break;

				case LONG:
					node.SolrType = SolrResponseDocumentNodeType.Int;
					node.Value = dis.ReadLong().ToString();
					break;

				case SHORT:
					node.SolrType = SolrResponseDocumentNodeType.Int;
					node.Value = dis.ReadShort().ToString();
					break;

				case SOLRDOC:
					return ReadSolrDocument(dis);
				case SOLRDOCLST:
					return ReadSolrDocumentList(dis);

				case MAP:
					throw new NotImplementedException("MAP parsing not implemented.");

				// dictionary
				//return ReadMap(dis);


				case ITERATOR:
					throw new NotImplementedException("ITERATOR handling not implemented.");

				//return ReadIterator(dis);
				case END:
					throw new NotImplementedException("END handling not implemented.");

				//return END_OBJ;
				case BYTE:
					throw new NotImplementedException("Byte parsing not implemented.");
				case BYTEARR:
					throw new NotImplementedException("Byte array parsing not implemented.");
			}
			return node;
		}

		public object ReadVal(FastInputStream dis)
		{
			tagByte = (byte)dis.ReadByte();

			// try type + size in single byte
			switch (tagByte >> 5)
			{
				case STR >> 5:
					return ReadStr(dis);
				case SINT >> 5:
					return ReadSmallInt(dis);
				case SLONG >> 5:
					return ReadSmallLong(dis);
				case ARR >> 5:
					return ReadArray(dis);
				case ORDERED_MAP >> 5:
					//return ReadNamedList(dis, true);
				case NAMED_LST >> 5:
					return ReadNamedList(dis);
				case EXTERN_STRING >> 5:
					return ReadExternString(dis);
			}

			switch (tagByte)
			{
				case NULL:
					return null;
				case DATE:
					try
					{
						return utcDateTime.AddMilliseconds(dis.ReadLong()).ToLocalTime();
					}
					catch
					{
						return new DateTime();
					}
				case INT:
					return dis.ReadInt();
				case BOOL_TRUE:
					return true;
				case BOOL_FALSE:
					return false;
				case FLOAT:
					return dis.ReadFloat();
				case DOUBLE:
					return dis.ReadDouble();
				case LONG:
					return dis.ReadLong();
				case BYTE:
					return dis.ReadByte();
				case SHORT:
					return dis.ReadShort();
				case MAP:
					return ReadMap(dis);
				case SOLRDOC:
					return ReadSolrDocument(dis);
				case SOLRDOCLST:
					return ReadSolrDocumentList(dis);
				case BYTEARR:
					return ReadByteArray(dis);
				case ITERATOR:
					return ReadIterator(dis);
				case END:
					return END_OBJ;
			}

			throw new ApplicationException("Unknown type " + tagByte);
		}

		public List<SolrResponseDocumentNode> ReadArray(FastInputStream dis)
		{
			int arraySize = ReadSize(dis);
			if (arraySize <= 0)

				//empty array
				return null;

			List<SolrResponseDocumentNode> arr = new List<SolrResponseDocumentNode>();
			for (int i = 0; i < arraySize; i++)
			{
				var node = ReadNode(dis) as SolrResponseDocumentNode;
				if (node != null)
					arr.Add(node);
			}
			return arr;
		}

		public SolrResponseDocumentNode ReadSolrDocument(FastInputStream dis)
		{
			tagByte = (byte)dis.ReadByte();
			if (tagByte >> 5 != ORDERED_MAP >> 5)
				throw new ApplicationException("Invalid Document Structure");

			SolrResponseDocumentNode doc = new SolrResponseDocumentNode("", SolrResponseDocumentNodeType.Document);
			int kvCount = ReadSize(dis);
			if (kvCount > 0)
				doc.Collection = new List<SolrResponseDocumentNode>();
			for (int k = 0; k < kvCount; k++)
			{
				string name = (string)ReadVal(dis);

				var node = ReadNode(dis, name);
				if (node is SolrResponseDocumentNode)
					doc.Collection.Add((SolrResponseDocumentNode)node);
				else if (node is List<SolrResponseDocumentNode>)
					doc.Collection.Add(new SolrResponseDocumentNode(name,SolrResponseDocumentNodeType.Array){Collection=(List<SolrResponseDocumentNode>)node});
				else if (node!=null)
					throw new TypeNotSupportedException(node.GetType().FullName);
			}
			return doc;
		}

		public SolrResponseDocumentNode ReadSolrDocumentList(FastInputStream dis)
		{
			SolrResponseDocumentNode node = new SolrResponseDocumentNode("", SolrResponseDocumentNodeType.Results);
			node.Collection = ReadNode(dis) as List<SolrResponseDocumentNode> ?? new List<SolrResponseDocumentNode>();
			for (int i = 0; i < node.Collection.Count; i++)
			{
				switch (i)
				{
					case 0:
						node.Collection[i].Name = "numFound";
						break;

					case 1:
						node.Collection[i].Name = "start";
						break;

					case 2:
						node.Collection[i].Name = "maxScore";
						break;
				}
			}
			var docNodes = ReadNode(dis) as List<SolrResponseDocumentNode>;
			if (docNodes != null)
				node.Collection.AddRange(docNodes);
			return node;
		}

		public IDictionary<object, object> ReadMap(FastInputStream dis)
		{
			int sz = ReadVInt(dis);
			IDictionary<object, object> m = new LinkedHashMap<object, object>();
			for (int i = 0; i < sz; i++)
			{
				object key = ReadVal(dis);
				object val = ReadVal(dis);
				m[key] = val;
			}
			return m;
		}

		public IList ReadIterator(FastInputStream fis)
		{
			ArrayList l = new ArrayList();
			while (true)
			{
				object o = ReadVal(fis);
				if (o == END_OBJ) break;
				l.Add(o);
			}
			return l;
		}

		//public IList ReadArray(FastInputStream dis)
		//{
		//	int sz = ReadSize(dis);
		//	ArrayList l = new ArrayList(sz);
		//	for (int i = 0; i < sz; i++)
		//	{
		//		l.Add(ReadVal(dis));
		//	}
		//	return l;
		//}

		private byte[] bytes;
		private char[] chars;

		public string ReadStr(FastInputStream dis)
		{
			int sz = ReadSize(dis);
			if (chars == null || chars.Length < sz) chars = new char[sz];
			if (bytes == null || bytes.Length < sz) bytes = new byte[sz];
			dis.ReadFully(bytes, 0, sz);
			int outUpto = 0;
			for (int i = 0; i < sz; )
			{
				int b = bytes[i++] & 0xff;
				int ch;
				if (b < 0xc0)
				{
					ch = b;
				}
				else if (b < 0xe0)
				{
					ch = ((b & 0x1f) << 6) + (bytes[i++] & 0x3f);
				}
				else if (b < 0xf0)
				{
					ch = ((b & 0xf) << 12) + ((bytes[i++] & 0x3f) << 6) + (bytes[i++] & 0x3f);
				}
				else
				{
					ch = ((b & 0x7) << 18) + ((bytes[i++] & 0x3f) << 12) + ((bytes[i++] & 0x3f) << 6) + (bytes[i++] & 0x3f);
				}
				if (ch <= 0xFFFF)
				{
					// target is a character <= 0xFFFF
					chars[outUpto++] = (char)ch;
				}
				else
				{
					// target is a character in range 0xFFFF - 0x10FFFF
					int chHalf = ch - 0x10000;
					chars[outUpto++] = (char)((chHalf >> 0xA) + 0xD800);
					chars[outUpto++] = (char)((chHalf & 0x3FF) + 0xDC00);
				}
			}
			return new String(chars, 0, outUpto);
		}

		public int ReadSmallInt(FastInputStream dis)
		{
			int v = tagByte & 0x0F;
			if ((tagByte & 0x10) != 0)
				v = (ReadVInt(dis) << 4) | v;
			return v;
		}

		public long ReadSmallLong(FastInputStream dis)
		{
			long v = tagByte & 0x0F;
			if ((tagByte & 0x10) != 0)
				v = (ReadVLong(dis) << 4) | v;
			return v;
		}

		public int ReadSize(FastInputStream intputStream)
		{
			int sz = tagByte & 0x1f;
			if (sz == 0x1f) sz += ReadVInt(intputStream);
			return sz;
		}

		public static int ReadVInt(FastInputStream fs)
		{
			byte b = (byte)fs.ReadByte();
			int i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7)
			{
				b = (byte)fs.ReadByte();
				i |= (b & 0x7F) << shift;
			}
			return i;
		}

		public static void WriteVLong(long i, FastOutputStream fs)
		{
			while ((i & ~0x7F) != 0)
			{
				fs.WriteByte((byte)((i & 0x7f) | 0x80));
				i >>= 7;
			}
			fs.WriteByte((byte)i);
		}

		public static long ReadVLong(FastInputStream fs)
		{
			byte b = (byte)fs.ReadByte();
			long i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7)
			{
				b = (byte)fs.ReadByte();
				i |= (long)(b & 0x7F) << shift;
			}
			return i;
		}

		private int stringsCount = 0;
		private IDictionary<string, int?> stringsMap;
		private IList<string> stringsList;

		public string ReadExternString(FastInputStream fis)
		{
			int idx = ReadSize(fis);
			if (idx != 0)
			{// idx != 0 is the index of the extern string
				return stringsList[idx - 1];
			}
			else
			{// idx == 0 means it has a string value
				string s = (string)ReadVal(fis);
				if (stringsList == null) stringsList = new List<string>();
				stringsList.Add(s);
				return s;
			}
		}

		public byte[] ReadByteArray(FastInputStream dis)
		{
			byte[] arr = new byte[ReadVInt(dis)];
			dis.ReadFully(arr);
			return arr;
		}

		#region write methods

		//public void Marshal(object nl, Stream os)
		//{
		//	daos = new FastOutputStream(os);

		//	try
		//	{
		//		daos.WriteByte(VERSION);
		//		WriteVal(nl);
		//	}
		//	finally
		//	{
		//		daos.Flush();
		//		daos.Close();
		//	}
		//}

		//public void WriteNamedList(NamedList nl)
		//{
		//	WriteTag(nl.IsOrdered ? ORDERED_MAP : NAMED_LST, nl.Count);

		//	for (int i = 0; i < nl.Count; i++)
		//	{
		//		string name = nl.GetName(i);
		//		WriteExternString(name);
		//		object val = nl.GetVal(i);
		//		WriteVal(val);
		//	}
		//}

		//public void WriteVal(object val)
		//{
		//	WriteKnownType(val);
		//}
		//public void WriteSolrDocumentList(SolrDocumentList docs)
		//{
		//	WriteTag(SOLRDOCLST);
		//	IList l = new ArrayList(3);
		//	l.Add(docs.NumFound);
		//	l.Add(docs.Start);
		//	l.Add(docs.MaxScore);
		//	WriteArray(l);
		//	WriteArray(docs);
		//}
		//public void WriteIterator(IEnumerator iter)
		//{
		//	WriteTag(ITERATOR);

		//	while (iter.MoveNext())
		//	{
		//		WriteVal(iter.Current);
		//	}

		//	WriteVal(END_OBJ);
		//}
		//public void WriteArray(IList l)
		//{
		//	WriteTag(ARR, l.Count);

		//	for (int i = 0; i < l.Count; i++)
		//	{
		//		WriteVal(l[i]);
		//	}
		//}

		//public void WriteArray(object[] arr)
		//{
		//	WriteTag(ARR, arr.Length);

		//	for (int i = 0; i < arr.Length; i++)
		//	{
		//		object o = arr[i];

		//		WriteVal(o);
		//	}
		//}

		//public void WriteStr(string s)
		//{
		//	if (s == null)
		//	{
		//		WriteTag(NULL);

		//		return;
		//	}
		//	int end = s.Length;
		//	int maxSize = end * 4;
		//	if (bytes == null || bytes.Length < maxSize) bytes = new byte[maxSize];
		//	int upto = 0;
		//	for (int i = 0; i < end; i++)
		//	{
		//		int code = s[i];

		//		if (code < 0x80)
		//			bytes[upto++] = (byte)code;
		//		else if (code < 0x800)
		//		{
		//			bytes[upto++] = (byte)(0xC0 | (code >> 6));
		//			bytes[upto++] = (byte)(0x80 | (code & 0x3F));
		//		}
		//		else if (code < 0xD800 || code > 0xDFFF)
		//		{
		//			bytes[upto++] = (byte)(0xE0 | (code >> 12));
		//			bytes[upto++] = (byte)(0x80 | ((code >> 6) & 0x3F));
		//			bytes[upto++] = (byte)(0x80 | (code & 0x3F));
		//		}
		//		else
		//		{
		//			// surrogate pair
		//			// confirm valid high surrogate
		//			if (code < 0xDC00 && (i < end - 1))
		//			{
		//				int utf32 = s[i + 1];
		//				// confirm valid low surrogate and write pair
		//				if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
		//				{
		//					utf32 = ((code - 0xD7C0) << 10) + (utf32 & 0x3FF);
		//					i++;
		//					bytes[upto++] = (byte)(0xF0 | (utf32 >> 18));
		//					bytes[upto++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
		//					bytes[upto++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
		//					bytes[upto++] = (byte)(0x80 | (utf32 & 0x3F));
		//					continue;
		//				}
		//			}
		//			// replace unpaired surrogate or out-of-order low surrogate
		//			// with substitution character
		//			bytes[upto++] = (byte)0xEF;
		//			bytes[upto++] = (byte)0xBF;
		//			bytes[upto++] = (byte)0xBD;
		//		}
		//	}
		//	WriteTag(STR, upto);
		//	daos.Write(bytes, 0, upto);
		//}
		//public void WriteInt(int val)
		//{
		//	if (val > 0)
		//	{
		//		int b = SINT | (val & 0x0f);

		//		if (val >= 0x0f)
		//		{
		//			b |= 0x10;
		//			daos.WriteByte(b);
		//			WriteVInt(val >> 4, daos);
		//		}
		//		else
		//		{
		//			daos.WriteByte(b);
		//		}

		//	}
		//	else
		//	{
		//		daos.WriteByte(INT);
		//		daos.WriteInt(val);
		//	}
		//}
		//public void WriteLong(long val)
		//{
		//	if (((ulong)val & 0xff00000000000000L) == 0)
		//	{
		//		int b = SLONG | ((int)val & 0x0f);
		//		if (val >= 0x0f)
		//		{
		//			b |= 0x10;
		//			daos.WriteByte(b);
		//			WriteVLong(val >> 4, daos);
		//		}
		//		else
		//		{
		//			daos.WriteByte(b);
		//		}
		//	}
		//	else
		//	{
		//		daos.WriteByte(LONG);
		//		daos.WriteLong(val);
		//	}
		//}
		//public static void WriteVInt(int i, FastOutputStream fs)
		//{
		//	while ((i & ~0x7F) != 0)
		//	{
		//		fs.WriteByte((byte)((i & 0x7f) | 0x80));
		//		i >>= 7;
		//	}
		//	fs.WriteByte((byte)i);
		//}

		//public bool WritePrimitive(object val)
		//{
		//	if (val == null)
		//	{
		//		daos.WriteByte(NULL);
		//		return true;
		//	}
		//	else if (val is string)
		//	{
		//		WriteStr((string)val);
		//		return true;
		//	}
		//	else if (val is int)
		//	{
		//		WriteInt((int)val);
		//		return true;
		//	}
		//	else if (val is long)
		//	{
		//		WriteLong((long)val);
		//		return true;
		//	}
		//	else if (val is float)
		//	{
		//		daos.WriteByte(FLOAT);
		//		daos.WriteFloat((float)val);
		//		return true;
		//	}
		//	else if (val is DateTime)
		//	{
		//		daos.WriteByte(DATE);
		//		daos.WriteLong((long)((DateTime)val).ToUniversalTime().Subtract(utcDateTime).TotalMilliseconds);
		//		return true;
		//	}
		//	else if (val is Boolean)
		//	{
		//		if ((Boolean)val) daos.WriteByte(BOOL_TRUE);
		//		else daos.WriteByte(BOOL_FALSE);
		//		return true;
		//	}
		//	else if (val is double)
		//	{
		//		daos.WriteByte(DOUBLE);
		//		daos.WriteDouble((Double)val);
		//		return true;
		//	}
		//	else if (val is byte)
		//	{
		//		daos.WriteByte(BYTE);
		//		daos.WriteByte((Byte)val);
		//		return true;
		//	}
		//	else if (val is short)
		//	{
		//		daos.WriteByte(SHORT);
		//		daos.WriteShort((short)val);
		//		return true;
		//	}
		//	else if (val is byte[])
		//	{
		//		WriteByteArray((byte[])val, 0, ((byte[])val).Length);
		//		return true;
		//	}
		//	//else if (val is ByteBuffer)
		//	//{
		//	//    ByteBuffer buf = (ByteBuffer)val;
		//	//    writeByteArray(buf.array(), buf.position(), buf.limit() - buf.position());
		//	//    return true;
		//	//}
		//	else if (val == END_OBJ)
		//	{
		//		WriteTag(END);
		//		return true;
		//	}
		//	return false;
		//}

		//public void WriteMap(IDictionary val)
		//{
		//	WriteTag(MAP, val.Count);

		//	foreach (DictionaryEntry entry in val)
		//	{
		//		object key = entry.Key;
		//		if (key is string)
		//		{
		//			WriteExternString((string)key);
		//		}
		//		else
		//		{
		//			WriteVal(key);
		//		}
		//		WriteVal(entry.Value);
		//	}
		//}

		//public void WriteExternString(string s)
		//{
		//	if (s == null)
		//	{
		//		WriteTag(NULL);
		//		return;
		//	}
		//	int? idx;
		//	if (stringsMap == null)
		//	{
		//		idx = null;
		//	}
		//	else
		//	{
		//		stringsMap.TryGetValue(s, out idx);
		//	}

		//	if (idx == null) idx = 0;
		//	WriteTag(EXTERN_STRING, idx.Value);
		//	if (idx == 0)
		//	{
		//		WriteStr(s);
		//		if (stringsMap == null) stringsMap = new Dictionary<String, int?>();
		//		stringsMap[s] = (++stringsCount);
		//	}

		//}
		//public bool WriteKnownType(object val)
		//{
		//	if (WritePrimitive(val)) return true;

		//	if (val is NamedList)
		//	{
		//		WriteNamedList((NamedList)val);
		//		return true;
		//	}
		//	if (val is SolrDocumentList)
		//	{ // SolrDocumentList is a IList, so must come before IList check
		//		WriteSolrDocumentList((SolrDocumentList)val);
		//		return true;
		//	}
		//	if (val is IList)
		//	{
		//		WriteArray((IList)val);
		//		return true;
		//	}
		//	if (val is object[])
		//	{
		//		WriteArray((object[])val);
		//		return true;
		//	}
		//	if (val is SolrDocument)
		//	{
		//		WriteSolrDocument((SolrDocument)val);

		//		return true;
		//	}
		//	if (val is IDictionary)
		//	{
		//		WriteMap((IDictionary)val);
		//		return true;
		//	}
		//	if (val is IEnumerator)
		//	{
		//		WriteIterator((IEnumerator)val);
		//		return true;
		//	}
		//	if (val is IEnumerable)
		//	{
		//		WriteIterator(((IEnumerable)val).GetEnumerator());
		//		return true;
		//	}
		//	return false;
		//}

		//public void WriteTag(byte tag)
		//{
		//	daos.WriteByte(tag);
		//}

		//public void WriteTag(byte tag, int size)
		//{
		//	if ((tag & 0xe0) != 0)
		//	{
		//		if (size < 0x1f)
		//		{
		//			daos.WriteByte(tag | size);
		//		}
		//		else
		//		{
		//			daos.WriteByte(tag | 0x1f);
		//			WriteVInt(size - 0x1f, daos);
		//		}
		//	}
		//	else
		//	{
		//		daos.WriteByte(tag);
		//		WriteVInt(size, daos);
		//	}
		//}

		//public void WriteByteArray(byte[] arr, int offset, int len)
		//{
		//	WriteTag(BYTEARR, len);
		//	daos.Write(arr, offset, len);
		//}

		//public void WriteSolrDocument(SolrDocument doc)
		//{
		//	WriteSolrDocument(doc, null);
		//}

		//public void WriteSolrDocument(SolrDocument doc, HashSet<string> fields)
		//{
		//	int count = 0;
		//	if (fields == null)
		//	{
		//		count = doc.GetFieldNames().Count;
		//	}
		//	else
		//	{
		//		foreach (KeyValuePair<string, object> entry in doc)
		//		{
		//			if (fields.Contains(entry.Key)) count++;
		//		}
		//	}

		//	WriteTag(SOLRDOC);
		//	WriteTag(ORDERED_MAP, count);

		//	foreach (KeyValuePair<string, object> entry in doc)
		//	{
		//		if (fields == null || fields.Contains(entry.Key))
		//		{
		//			string name = entry.Key;
		//			WriteExternString(name);
		//			object val = entry.Value;
		//			WriteVal(val);
		//		}
		//	}
		//}

		#endregion write methods
	}
}