// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Dicom.IO;

namespace Dicom.Data {
	public abstract class DcmElement : DcmItem {
		#region Protected Members
		protected ByteBuffer _bb;
		#endregion

		#region Public Constructors
		public DcmElement(DcmTag tag, DcmVR vr) : base(tag, vr) {
			_bb = new ByteBuffer();
		}

		public DcmElement(DcmTag tag, DcmVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
			_bb = new ByteBuffer(_endian);
		}

		public DcmElement(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian) {
			if (buffer == null && _endian != Endian.LocalMachine)
				_bb = new ByteBuffer(_endian);
			else
				_bb = buffer;
		}
		#endregion

		#region Public Properties
		public int Length {
			get {
				if (_bb == null)
					return 0;
				return _bb.Length;
			}
		}

		public ByteBuffer ByteBuffer {
			get {
				if (_bb == null)
					_bb = new ByteBuffer();
				return _bb;
			}
			set { _bb = value; }
		}
		#endregion

		#region Abstract Methods
		public abstract int GetVM();

		public abstract string GetValueString();
		public abstract void SetValueString(string val);

		public abstract Type GetValueType();
		public abstract object GetValueObject();
		public abstract object[] GetValueObjectArray();
		public abstract void SetValueObject(object val);
		public abstract void SetValueObjectArray(object[] vals);
		#endregion

		#region DcmItem Methods
		internal override uint CalculateWriteLength(DcmTS syntax, DicomWriteOptions options) {
			uint length = 4; // element tag
			if (syntax.IsExplicitVR) {
				length += 2; // vr
				if (VR.Is16BitLengthField)
					length += 2;
				else
					length += 6;
			} else {
				length += 4; // length tag				
			}
			length += (uint)Length;
			return length;
		}

		protected override void ChangeEndianInternal() {
			ByteBuffer.Endian = _endian;
			ByteBuffer.Swap(_vr.UnitSize);
		}

		internal override void Preload() {
			if (_bb != null)
				_bb.Preload();
		}

		internal override void Unload() {
			if (_bb != null)
				_bb.Unload();
		}

		public override void Dump(StringBuilder sb, string prefix, DicomDumpOptions options) {
			int ValueWidth = 40 - prefix.Length;
			int SbLength = sb.Length;

			sb.Append(prefix);
			sb.AppendFormat("{0} {1} ", Tag.ToString(), VR.VR);
			if (Length == 0) {
				String value = "(no value available)";
				sb.Append(value.PadRight(ValueWidth, ' '));
			} else {
				if (VR.IsString) {
					String value = null;
					if (VR == DcmVR.UI) {
						DcmUniqueIdentifier ui = this as DcmUniqueIdentifier;
						DcmUID uid = ui.GetUID();
						if (uid != null) {
							if (uid.Type == UidType.Unknown)
								value = "[" + uid.UID + "]";
							else
								value = "=" + uid.Description;
							if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
								if (value.Length > ValueWidth) {
									value = value.Substring(0, ValueWidth - 3);
								}
							}
						} else {
							value = "[" + GetValueString() + "]";
							if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
								if (value.Length > ValueWidth) {
									value = value.Substring(0, ValueWidth - 4) + "...]";
								}
							}
						}
					} else {
						value = "[" + GetValueString() + "]";
						if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
							if (value.Length > ValueWidth) {
								value = value.Substring(0, ValueWidth - 4) + "...]";
							}
						}
					}
					sb.Append(value.PadRight(ValueWidth, ' '));
				} else {
					String value = GetValueString();
					if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
						if (value.Length > ValueWidth) {
							value = value.Substring(0, ValueWidth - 3) + "...";
						}
					}
					sb.Append(value.PadRight(ValueWidth, ' '));
				}
			}
			sb.AppendFormat(" # {0,4} {2}", Length, GetVM(), Tag.Entry.Name);

			if (Flags.IsSet(options, DicomDumpOptions.Restrict80CharactersPerLine)) {
				if (sb.Length > (SbLength + 79)) {
					sb.Length = SbLength + 79;
					//sb.Append(">");
				}
			}
		}
		#endregion

		#region Static Create Methods
		public static DcmElement Create(DcmTag tag) {
			DcmVR vr = tag.Entry.DefaultVR;
			return Create(tag, vr);
		}

		public static DcmElement Create(DcmTag tag, DcmVR vr) {
			return Create(tag, vr, 0, Endian.LocalMachine);
		}

		public static DcmElement Create(DcmTag tag, DcmVR vr, long pos, Endian endian) {
			return Create(tag, vr, pos, endian, null);
		}

		public static DcmElement Create(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer) {
			if (vr == DcmVR.SQ)
				throw new DcmDataException("Sequence Elements should be created explicitly");

			switch (vr.VR) {
			case "AE":
				return new DcmApplicationEntity(tag, pos, endian, buffer);
			case "AS":
				return new DcmAgeString(tag, pos, endian, buffer);
			case "AT":
				return new DcmAttributeTag(tag, pos, endian, buffer);
			case "CS":
				return new DcmCodeString(tag, pos, endian, buffer);
			case "DA":
				return new DcmDate(tag, pos, endian, buffer);
			case "DS":
				return new DcmDecimalString(tag, pos, endian, buffer);
			case "DT":
				return new DcmDateTime(tag, pos, endian, buffer);
			case "FD":
				return new DcmFloatingPointDouble(tag, pos, endian, buffer);
			case "FL":
				return new DcmFloatingPointSingle(tag, pos, endian, buffer);
			case "IS":
				return new DcmIntegerString(tag, pos, endian, buffer);
			case "LO":
				return new DcmLongString(tag, pos, endian, buffer);
			case "LT":
				return new DcmLongText(tag, pos, endian, buffer);
			case "OB":
				return new DcmOtherByte(tag, pos, endian, buffer);
			case "OF":
				return new DcmOtherFloat(tag, pos, endian, buffer);
			case "OW":
				return new DcmOtherWord(tag, pos, endian, buffer);
			case "PN":
				return new DcmPersonName(tag, pos, endian, buffer);
			case "SH":
				return new DcmShortString(tag, pos, endian, buffer);
			case "SL":
				return new DcmSignedLong(tag, pos, endian, buffer);
			case "SS":
				return new DcmSignedShort(tag, pos, endian, buffer);
			case "ST":
				return new DcmShortText(tag, pos, endian, buffer);
			case "TM":
				return new DcmTime(tag, pos, endian, buffer);
			case "UI":
				return new DcmUniqueIdentifier(tag, pos, endian, buffer);
			case "UL":
				return new DcmUnsignedLong(tag, pos, endian, buffer);
			case "UN":
				return new DcmUnknown(tag, pos, endian, buffer);
			case "US":
				return new DcmUnsignedShort(tag, pos, endian, buffer);
			case "UT":
				return new DcmUnlimitedText(tag, pos, endian, buffer);
			default:
				break;
			}
			throw new DcmDataException("Unhandled VR: " + vr.VR);
		}
		#endregion
	}

	#region Base Types
	public class DcmStringElement : DcmElement {
		#region Public Constructors
		public DcmStringElement(DcmTag tag, DcmVR vr) : base(tag, vr) {
		}

		public DcmStringElement(DcmTag tag, DcmVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmStringElement(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return 1;
		}

		public override string GetValueString() {
			return ByteBuffer.GetString().TrimEnd(' ', '\0');
		}
		public override void SetValueString(string val) {
			ByteBuffer.SetString(val, VR.Padding);
		}

		public override Type GetValueType() {
			return typeof(string);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValue((string)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new string[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValues((string[])vals);
		}
		#endregion

		#region Public Members
		public string GetValue() {
			return GetValue(0);
		}

		public string GetValue(int index) {
			if (index != 0)
				throw new DcmDataException("Non-zero index used for single value string element");
			return GetValueString();
		}

		public string[] GetValues() {
			return GetValueString().Split('\\');
		}

		public void SetValue(string value) {
			ByteBuffer.SetString(value, VR.Padding);
		}

		public void SetValues(string[] values) {
			ByteBuffer.SetString(string.Join("\\", values), VR.Padding);
		}
		#endregion
	}

	public class DcmMultiStringElement : DcmElement {
		#region Public Constructors
		public DcmMultiStringElement(DcmTag tag, DcmVR vr) : base(tag, vr) {
		}

		public DcmMultiStringElement(DcmTag tag, DcmVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmMultiStringElement(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		private int _vm = -1;
		public override int GetVM() {
			if (_vm == -1)
				_vm = GetValues().Length;
			return _vm;
		}

		public override string GetValueString() {
			return ByteBuffer.GetString().TrimEnd(' ', '\0');
		}
		public override void SetValueString(string val) {
			ByteBuffer.SetString(val, VR.Padding);
		}

		public override Type GetValueType() {
			return typeof(string);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValue((string)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new string[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValues((string[])vals);
		}
		#endregion

		#region Public Members
		public string GetValue() {
			return GetValue(0);
		}

		public string GetValue(int index) {
			string[] vals = GetValues();
			if (index >= vals.Length)
				throw new DcmDataException("Value index out of range");
			return vals[index].TrimEnd(' ', '\0');
		}

		public string[] GetValues() {
			return GetValueString().Split('\\');
		}

		public void SetValue(string value) {
			ByteBuffer.SetString(value, VR.Padding);
		}

		public void SetValues(string[] values) {
			ByteBuffer.SetString(string.Join("\\", values), VR.Padding);
		}
		#endregion
	}

	public class DcmDateElementBase : DcmMultiStringElement {
		#region Protected Members
		protected string[] _formats;
		#endregion

		#region Public Constructors
		public DcmDateElementBase(DcmTag tag, DcmVR vr)
			: base(tag, vr) {
		}

		public DcmDateElementBase(DcmTag tag, DcmVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmDateElementBase(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Private Members
		private DateTime ParseDate(string date) {
			try {
				if (_formats != null)
					return DateTime.ParseExact(date, _formats, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault);
				else
					return DateTime.Parse(date);
			}
			catch {
				return DateTime.Today;
			}
		}

		private DateTime[] ParseDateRange(string date) {
			if (date == null || date.Length == 0) {
				DateTime[] r = new DateTime[2];
				r[0] = DateTime.MinValue;
				r[1] = DateTime.MinValue;
				return r;
			}

			int hypPos = date.IndexOf((Char)'-');
			if (hypPos == -1) {
				DateTime[] d = new DateTime[1];
				d[0] = ParseDate(date);
				return d;
			}
			DateTime[] range = new DateTime[2];
			try {
				range[0] = ParseDate(date.Substring(0, hypPos));
			} catch {
				range[0] = DateTime.MinValue;
			}
			try {
				range[1] = ParseDate(date.Substring(hypPos + 1));
			} catch {
				range[1] = DateTime.MinValue;
			}
			return range;
		}
		#endregion

		#region Abstract Overrides
		public override Type GetValueType() {
			return typeof(DateTime);
		}
		public override object GetValueObject() {
			return GetDateTime();
		}
		public override object[] GetValueObjectArray() {
			throw new DcmDataException("GetValueObjectArray() should not be called for DateTime types!");
		}
		public override void SetValueObject(object val) {
			if (val.GetType() == typeof(DcmDateRange))
				SetDateTimeRange((DcmDateRange)val);
			else if (val.GetType() == typeof(DateTime))
				SetDateTime((DateTime)val);
			else if (val.GetType() == typeof(string))
				SetValue((string)val);
			else
				throw new DcmDataException("Invalid type for Element VR!");
		}
		public override void SetValueObjectArray(object[] vals) {
			throw new DcmDataException("SetValueObjectArray() should not be called for DateTime types!");
		}
		#endregion

		#region Public Members
		public DateTime GetDateTime() {
			return GetDateTime(0);
		}
		public DateTime GetDateTime(int index) {
			return ParseDate(GetValue(index));
		}

		public DateTime[] GetDateTimes() {
			string[] strings = GetValues();
			DateTime[] values = new DateTime[strings.Length];
			for (int i = 0; i < strings.Length; i++) {
				values[i] = ParseDate(strings[i]);
			}
			return values;
		}

		public DcmDateRange GetDateTimeRange() {
			return new DcmDateRange(ParseDateRange(GetValue(0)));
		}

		public void SetDateTime(DateTime value) {
			if (_formats != null)
				SetValue(value.ToString(_formats[0]));
			else
				SetValue(value.ToString());
		}

		public void SetDateTimes(DateTime[] values) {
			string[] strings = new string[values.Length];
			for (int i = 0; i < strings.Length; i++) {
				if (_formats != null)
					strings[i] = values[i].ToString(_formats[0]);
				else
					strings[i] = values[i].ToString();
			}
			SetValues(strings);
		}

		public void SetDateTimeRange(DcmDateRange range) {
			if (range != null)
				SetValue(range.ToString(_formats[0]));
			else
				SetValue("");
		}
		#endregion
	}

	public class DcmValueElement<T> : DcmElement {
		#region Protected Members
		protected string StringFormat;
		protected NumberStyles NumberStyle;
		#endregion

		#region Public Constructors
		public DcmValueElement(DcmTag tag, DcmVR vr)
			: base(tag, vr) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}

		public DcmValueElement(DcmTag tag, DcmVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}

		public DcmValueElement(DcmTag tag, DcmVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return ByteBuffer.Length / VR.UnitSize;
		}

		public override string GetValueString() {
			T[] vals = GetValues();
			StringBuilder sb = new StringBuilder();
			foreach (T val in vals) {
				sb.AppendFormat(StringFormat, val).Append('\\');
			}
			if (sb.Length > 0) {
				sb.Length = sb.Length - 1;
			}
			return sb.ToString();
		}

		private object ParseNumber(string val) {
			try {
				if (typeof(T) == typeof(byte)) {
					return byte.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(sbyte)) {
					return sbyte.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(short)) {
					return short.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(ushort)) {
					return ushort.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(int)) {
					return int.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(uint)) {
					return uint.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(long)) {
					return long.Parse(val, NumberStyle);
				}
				if (typeof(T) == typeof(ulong)) {
					return ulong.Parse(val, NumberStyle);
				}
			} catch { }
			return null;
		}

		public override void SetValueString(string val) {
			if (val == null || val == String.Empty) {
				SetValues(new T[0]);
				return;
			}
			string[] strs = val.Split('\\');
			T[] vals = new T[strs.Length];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = (T)ParseNumber(strs[i]);
			}
			SetValues(vals);
		}

		public override Type GetValueType() {
			return typeof(T);
		}
		public override object GetValueObject() {
			if (GetVM() == 0)
				return null;
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			T[] v = GetValues();
			object[] o = new object[v.Length];
			Array.Copy(v, o, v.Length);
			return o;
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValue((T)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new T[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			T[] v = new T[vals.Length];
			Array.Copy(vals, v, vals.Length);
			SetValues(v);
		}
		#endregion

		#region Public Members
		public T GetValue() {
			return GetValue(0);
		}

		public T GetValue(int index) {
			if (index >= GetVM())
				throw new DcmDataException("Value index out of range");
			SelectByteOrder(Endian.LocalMachine);
			T[] vals = new T[1];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), index * VR.UnitSize,
				vals, 0, VR.UnitSize);
			return vals[0];
		}

		public T[] GetValues() {
			SelectByteOrder(Endian.LocalMachine);
			T[] vals = new T[GetVM()];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), 0, vals, 0, vals.Length * VR.UnitSize);
			return vals;
		}

		public void SetValue(T value) {
			T[] vals = new T[1];
			vals[0] = value;
			SetValues(vals);
		}

		public void SetValues(T[] vals) {
			SelectByteOrder(Endian.LocalMachine);
			byte[] bytes = new byte[vals.Length * VR.UnitSize];
			Buffer.BlockCopy(vals, 0, bytes, 0, bytes.Length);
			ByteBuffer.FromBytes(bytes);
		}
		#endregion
	}
	#endregion

	#region Value Types
	/// <summary>
	/// Application Entity (AE)
	/// </summary>
	public class DcmApplicationEntity : DcmMultiStringElement {
		#region Public Constructors
		public DcmApplicationEntity(DcmTag tag)
			: base(tag, DcmVR.AE) {
		}

		public DcmApplicationEntity(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.AE, pos, endian) {
		}

		public DcmApplicationEntity(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.AE, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Age String (AS)
	/// </summary>
	public class DcmAgeString : DcmMultiStringElement {
		#region Public Constructors
		public DcmAgeString(DcmTag tag)
			: base(tag, DcmVR.AS) {
		}

		public DcmAgeString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.AS, pos, endian) {
		}

		public DcmAgeString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.AS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Attribute Tag (AT)
	/// </summary>
	public class DcmAttributeTag : DcmElement {
		#region Public Constructors
		public DcmAttributeTag(DcmTag tag)
			: base(tag, DcmVR.AT) {
		}

		public DcmAttributeTag(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.AT, pos, endian) {
		}

		public DcmAttributeTag(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.AT, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return ByteBuffer.Length / 4;
		}

		public override string GetValueString() {
			DcmTag[] tags = GetValues();
			StringBuilder sb = new StringBuilder();
			foreach (DcmTag tag in tags) {
				sb.AppendFormat("{0:X4}{1:X4}\\", tag.Group, tag.Element);
			}
			if (sb.Length > 0) {
				sb.Length = sb.Length - 1;
			}
			return sb.ToString();
		}
		public override void SetValueString(string val) {
			string[] strs = val.Split('\\');
			DcmTag[] tags = new DcmTag[strs.Length];
			for (int i = 0; i < tags.Length; i++) {
				if (strs[i].Length == 8) {
					string gs = strs[i].Substring(0, 4);
					string es = strs[i].Substring(4, 4);
					ushort g = ushort.Parse(gs, NumberStyles.HexNumber);
					ushort e = ushort.Parse(es, NumberStyles.HexNumber);
					tags[i] = new DcmTag(g, e);
				}
			}
			SetValues(tags);
		}

		public override Type GetValueType() {
			return typeof(DcmTag);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValue((DcmTag)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new DcmTag[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DcmDataException("Invalid type for Element VR!");
			SetValues((DcmTag[])vals);
		}
		#endregion

		#region Public Members
		public DcmTag GetValue() {
			return GetValue(0);
		}

		public DcmTag GetValue(int index) {
			if (index >= GetVM())
				throw new DcmDataException("Value index out of range");
			SelectByteOrder(Endian.LocalMachine);
			ushort[] u16s = new ushort[2];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), index * 4, u16s, 0, 4);
			return new DcmTag(u16s[0], u16s[1]);
		}

		public DcmTag[] GetValues() {
			SelectByteOrder(Endian.LocalMachine);
			ushort[] u16s = new ushort[GetVM() * 2];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), 0, u16s, 0, u16s.Length * 2);
			DcmTag[] tags = new DcmTag[GetVM()];
			for (int i = 0, n = 0; i < tags.Length; i++) {
				tags[i] = new DcmTag(u16s[n++], u16s[n++]);
			}
			return tags;
		}

		public void SetValue(DcmTag val) {
			SelectByteOrder(Endian.LocalMachine);
			ByteBuffer.Clear();
			ByteBuffer.Writer.Write(val.Group);
			ByteBuffer.Writer.Write(val.Element);
		}

		public void SetValues(DcmTag[] vals) {
			SelectByteOrder(Endian.LocalMachine);
			ByteBuffer.Clear();
			foreach (DcmTag val in vals) {
				ByteBuffer.Writer.Write(val.Group);
				ByteBuffer.Writer.Write(val.Element);
			}
		}
		#endregion

		#region DcmItem Methods
		protected override void ChangeEndianInternal() {
			ByteBuffer.Swap(2);
		}
		#endregion
	}

	/// <summary>
	/// Code String (CS)
	/// </summary>
	public class DcmCodeString : DcmMultiStringElement {
		#region Public Constructors
		public DcmCodeString(DcmTag tag)
			: base(tag, DcmVR.CS) {
		}

		public DcmCodeString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.CS, pos, endian) {
		}

		public DcmCodeString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.CS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Date (DA)
	/// </summary>
	public class DcmDate : DcmDateElementBase {
		#region Public Constructors
		public DcmDate(DcmTag tag)
			: base(tag, DcmVR.DA) {
			InitFormats();
		}

		public DcmDate(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.DA, pos, endian) {
			InitFormats();
		}

		public DcmDate(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.DA, pos, endian, buffer) {
			InitFormats();
		}

		private void InitFormats() {
			if (_formats == null) {
				_formats = new string[6];
				_formats[0] = "yyyyMMdd";
				_formats[1] = "yyyy.MM.dd";
				_formats[2] = "yyyy/MM/dd";
				_formats[3] = "yyyy";
				_formats[4] = "yyyyMM";
				_formats[5] = "yyyy.MM";
			}
		}
		#endregion
	}

	/// <summary>
	/// Decimal String (DS)
	/// </summary>
	public class DcmDecimalString : DcmMultiStringElement {
		#region Public Constructors
		public DcmDecimalString(DcmTag tag)
			: base(tag, DcmVR.DS) {
		}

		public DcmDecimalString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.DS, pos, endian) {
		}

		public DcmDecimalString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.DS, pos, endian, buffer) {
		}
		#endregion

		#region Public Members
		public double GetDouble() {
			return GetDouble(0);
		}

		public double GetDouble(int index) {
			string val = GetValue(index);
			return double.Parse(val);
		}

		public double[] GetDoubles() {
			double[] vals = new double[GetVM()];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = GetDouble(i);
			}
			return vals;
		}

		public void SetDouble(double value) {
			SetValue(value.ToString());
		}

		public void SetDoubles(double[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}

		public decimal GetDecimal() {
			return GetDecimal(0);
		}

		public decimal GetDecimal(int index) {
			string val = GetValue(index);
			return decimal.Parse(val);
		}

		public decimal[] GetDecimals() {
			decimal[] vals = new decimal[GetVM()];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = GetDecimal(i);
			}
			return vals;
		}

		public void SetDecimal(decimal value) {
			SetValue(value.ToString());
		}

		public void SetDecimals(decimal[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}
		#endregion
	}

	/// <summary>
	/// Date Time (DT)
	/// </summary>
	public class DcmDateTime : DcmDateElementBase {
		#region Public Constructors
		public DcmDateTime(DcmTag tag)
			: base(tag, DcmVR.DT) {
		}

		public DcmDateTime(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.DT, pos, endian) {
		}

		public DcmDateTime(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.DT, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Floating Point Double (FD)
	/// </summary>
	public class DcmFloatingPointDouble : DcmValueElement<double> {
		#region Public Constructors
		public DcmFloatingPointDouble(DcmTag tag)
			: base(tag, DcmVR.FD) {
		}

		public DcmFloatingPointDouble(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.FD, pos, endian) {
		}

		public DcmFloatingPointDouble(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.FD, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Floating Point Single (FL)
	/// </summary>
	public class DcmFloatingPointSingle : DcmValueElement<float> {
		#region Public Constructors
		public DcmFloatingPointSingle(DcmTag tag) 
			: base(tag, DcmVR.FL) {
		}

		public DcmFloatingPointSingle(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.FL, pos, endian) {
		}

		public DcmFloatingPointSingle(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.FL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Integer String (IS)
	/// </summary>
	public class DcmIntegerString : DcmMultiStringElement {
		#region Public Constructors
		public DcmIntegerString(DcmTag tag)
			: base(tag, DcmVR.IS) {
		}

		public DcmIntegerString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.IS, pos, endian) {
		}

		public DcmIntegerString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.IS, pos, endian, buffer) {
		}
		#endregion

		#region Access Methods
		public int GetInt32() {
			return GetInt32(0);
		}

		public int GetInt32(int index) {
			string val = GetValue(index);
			return int.Parse(val);
		}

		public int[] GetInt32s() {
			int[] ints = new int[GetVM()];
			for (int i = 0; i < ints.Length; i++) {
				ints[i] = GetInt32(i);
			}
			return ints;
		}

		public void SetInt32(int value) {
			SetValue(value.ToString());
		}

		public void SetInt32s(int[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}
		#endregion
	}

	/// <summary>
	/// Long String (LO)
	/// </summary>
	public class DcmLongString : DcmStringElement {
		#region Public Constructors
		public DcmLongString(DcmTag tag)
			: base(tag, DcmVR.LO) {
		}

		public DcmLongString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.LO, pos, endian) {
		}

		public DcmLongString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.LO, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Long Text (LT)
	/// </summary>
	public class DcmLongText : DcmStringElement {
		#region Public Constructors
		public DcmLongText(DcmTag tag)
			: base(tag, DcmVR.LT) {
		}

		public DcmLongText(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.LT, pos, endian) {
		}

		public DcmLongText(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.LT, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Other Byte (OB)
	/// </summary>
	public class DcmOtherByte : DcmValueElement<byte> {
		#region Public Constructors
		public DcmOtherByte(DcmTag tag)
			: base(tag, DcmVR.OB) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherByte(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.OB, pos, endian) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherByte(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.OB, pos, endian, buffer) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>
	/// Other Word (OW)
	/// </summary>
	public class DcmOtherWord : DcmValueElement<ushort> {
		#region Public Constructors
		public DcmOtherWord(DcmTag tag)
			: base(tag, DcmVR.OW) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherWord(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.OW, pos, endian) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherWord(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.OW, pos, endian, buffer) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>
	/// Other Float (OF)
	/// </summary>
	public class DcmOtherFloat : DcmValueElement<float> {
		#region Public Constructors
		public DcmOtherFloat(DcmTag tag)
			: base(tag, DcmVR.OF) {
		}

		public DcmOtherFloat(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.OF, pos, endian) {
		}

		public DcmOtherFloat(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.OF, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Person Name (PN)
	/// </summary>
	public class DcmPersonName : DcmStringElement {
		#region Public Constructors
		public DcmPersonName(DcmTag tag)
			: base(tag, DcmVR.PN) {
		}

		public DcmPersonName(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.PN, pos, endian) {
		}

		public DcmPersonName(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.PN, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Short String (SH)
	/// </summary>
	public class DcmShortString : DcmStringElement {
		#region Public Constructors
		public DcmShortString(DcmTag tag)
			: base(tag, DcmVR.SH) {
		}

		public DcmShortString(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.SH, pos, endian) {
		}

		public DcmShortString(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.SH, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Signed Long (SL)
	/// </summary>
	public class DcmSignedLong : DcmValueElement<int> {
		#region Public Constructors
		public DcmSignedLong(DcmTag tag)
			: base(tag, DcmVR.SL) {
		}

		public DcmSignedLong(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.SL, pos, endian) {
		}

		public DcmSignedLong(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.SL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Signed Short (SS)
	/// </summary>
	public class DcmSignedShort : DcmValueElement<short> {
		#region Public Constructors
		public DcmSignedShort(DcmTag tag)
			: base(tag, DcmVR.SS) {
		}

		public DcmSignedShort(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.SS, pos, endian) {
		}

		public DcmSignedShort(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.SS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Short Text (ST)
	/// </summary>
	public class DcmShortText : DcmStringElement {
		#region Public Constructors
		public DcmShortText(DcmTag tag)
			: base(tag, DcmVR.ST) {
		}

		public DcmShortText(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.ST, pos, endian) {
		}

		public DcmShortText(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.ST, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Time (TM)
	/// </summary>
	public class DcmTime : DcmDateElementBase {
		#region Public Constructors
		public DcmTime(DcmTag tag)
			: base(tag, DcmVR.TM) {
			InitFormats();
		}

		public DcmTime(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.TM, pos, endian) {
			InitFormats();
		}

		public DcmTime(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.TM, pos, endian, buffer) {
			InitFormats();
		}

		private void InitFormats() {
			if (_formats == null) {
				_formats = new string[37];
				_formats[0] = "HH";
				_formats[1] = "HHmm";
				_formats[2] = "HHmmss";
				_formats[3] = "HHmmssf";
				_formats[4] = "HHmmssff";
				_formats[5] = "HHmmssfff";
				_formats[6] = "HHmmssffff";
				_formats[7] = "HHmmssfffff";
				_formats[8] = "HHmmssffffff";
				_formats[9] = "HHmmss.f";
				_formats[10] = "HHmmss.ff";
				_formats[11] = "HHmmss.fff";
				_formats[12] = "HHmmss.ffff";
				_formats[13] = "HHmmss.fffff";
				_formats[14] = "HHmmss.ffffff";
				_formats[15] = "HH.mm";
				_formats[16] = "HH.mm.ss";
				_formats[17] = "HH.mm.ss.f";
				_formats[18] = "HH.mm.ss.ff";
				_formats[19] = "HH.mm.ss.fff";
				_formats[20] = "HH.mm.ss.ffff";
				_formats[21] = "HH.mm.ss.fffff";
				_formats[22] = "HH.mm.ss.ffffff";
				_formats[23] = "HH:mm";
				_formats[24] = "HH:mm:ss";
				_formats[25] = "HH:mm:ss:f";
				_formats[26] = "HH:mm:ss:ff";
				_formats[27] = "HH:mm:ss:fff";
				_formats[28] = "HH:mm:ss:ffff";
				_formats[29] = "HH:mm:ss:fffff";
				_formats[30] = "HH:mm:ss:ffffff";
				_formats[25] = "HH:mm:ss.f";
				_formats[26] = "HH:mm:ss.ff";
				_formats[27] = "HH:mm:ss.fff";
				_formats[28] = "HH:mm:ss.ffff";
				_formats[29] = "HH:mm:ss.fffff";
				_formats[30] = "HH:mm:ss.ffffff";
			}
		}
		#endregion
	}

	/// <summary>
	/// Unique Identifier (UI)
	/// </summary>
	public class DcmUniqueIdentifier : DcmMultiStringElement {
		#region Public Constructors
		public DcmUniqueIdentifier(DcmTag tag)
			: base(tag, DcmVR.UI) {
		}

		public DcmUniqueIdentifier(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.UI, pos, endian) {
		}

		public DcmUniqueIdentifier(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.UI, pos, endian, buffer) {
		}
		#endregion

		#region Access Methods
		public DcmUID GetUID() {
			return DcmUIDs.Lookup(GetValue());
		}

		public DcmTS GetTS() {
			return DcmTSs.Lookup(GetUID());
		}

		public void SetUID(DcmUID ui) {
			SetValue(ui.UID);
		}

		public void SetTS(DcmTS ts) {
			SetUID(ts.UID);
		}
		#endregion
	}

	/// <summary>
	/// Unsigned Long (UL)
	/// </summary>
	public class DcmUnsignedLong : DcmValueElement<uint> {
		#region Public Constructors
		public DcmUnsignedLong(DcmTag tag)
			: base(tag, DcmVR.UL) {
		}

		public DcmUnsignedLong(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.UL, pos, endian) {
		}

		public DcmUnsignedLong(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.UL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Unknown (UN)
	/// </summary>
	public class DcmUnknown : DcmValueElement<byte> {
		#region Public Constructors
		public DcmUnknown(DcmTag tag)
			: base(tag, DcmVR.UN) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmUnknown(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.UN, pos, endian) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmUnknown(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.UN, pos, endian, buffer) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>
	/// Unsigned Short (US)
	/// </summary>
	public class DcmUnsignedShort : DcmValueElement<ushort> {
		#region Public Constructors
		public DcmUnsignedShort(DcmTag tag)
			: base(tag, DcmVR.US) {
		}

		public DcmUnsignedShort(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.US, pos, endian) {
		}

		public DcmUnsignedShort(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.US, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>
	/// Unlimited Text (UT)
	/// </summary>
	public class DcmUnlimitedText : DcmStringElement {
		#region Public Constructors
		public DcmUnlimitedText(DcmTag tag)
			: base(tag, DcmVR.UT) {
		}

		public DcmUnlimitedText(DcmTag tag, long pos, Endian endian)
			: base(tag, DcmVR.UT, pos, endian) {
		}

		public DcmUnlimitedText(DcmTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DcmVR.UT, pos, endian, buffer) {
		}
		#endregion
	}
	#endregion
}