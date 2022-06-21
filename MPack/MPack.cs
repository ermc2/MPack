// LICENSE INFORMATION ////////////////////////////////////////////////////////////////////////
//                                                                                           //
//                                          MPack                                            //
//                                                                                           //
//  MPack is an implementation of the MessagePack binary serialization format.               //
//  It is a direct mapping to JSON, and the official specification can be found here:        //
//  https://github.com/msgpack/msgpack/blob/master/spec.md                                   //
//                                                                                           //
//  This MPack implementation is inspired by the work of ymofen (ymofen@diocp.org):          //
//  https://github.com/ymofen/SimpleMsgPack.Net                                              //
//                                                                                           //
//  this implementation has been completely reworked from the ground up including many       //
//  bux fixes and an API that remains lightweight compared to the official one,              //
//  while remaining robust and easy to use.                                                  //
//                                                                                           //
//  Written by Caelan Sayler [caelantsayler]at[gmail]dot[com]                                //
//  Original URL: https://github.com/caesay/MPack                                            //
//  Licensed: Attribution 4.0 (CC BY 4.0) http://creativecommons.org/licenses/by/4.0/        //
//                                                                                           //
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MPack
{
    public class MPack : IEquatable<MPack>, IConvertible
    {
        private readonly object _value = null;
        private MPackType _type = MPackType.Null;

        public virtual object Value => _value;

        public virtual MPackType ValueType => _type;

        internal MPack(object value, MPackType type)
        {
            _value = value;
            _type = type;
        }

        protected MPack()
        {
        }

        public virtual MPack this[int index]
        {
            get
            {
                if (this is MPackMap)
                {
                    return this[(MPack)index];
                }
                throw new NotSupportedException("Array indexor not supported in this context.");
            }
            set
            {
                if (this is MPackMap)
                {
                    this[(MPack)index] = value;
                }
                else
                    throw new NotSupportedException("Array indexor not supported in this context.");
            }
        }

        public virtual MPack this[MPack key]
        {
            get
            {
                throw new NotSupportedException("Map indexor not supported in this context.");
            }
            set
            {
                throw new NotSupportedException("Map indexor not supported in this context.");
            }
        }

        public static MPack Null()
        {
            return new MPack() { _type = MPackType.Null };
        }

        public static MPack From(object value)
        {
            return From(value, value.GetType());
        }

        public static MPack From(object value, Type type)
        {
            if (value == null)
            {
                return new MPack(null, MPackType.Null);
            }

            if (!type.IsInstanceOfType(value))
            {
                throw new ArgumentException("Type does not match provided object.");
            }
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if (elementType == typeof(byte))
                {
                    return new MPack(value, MPackType.Binary);
                }
                if (elementType == typeof(MPack))
                {
                    return new MPackArray((MPack[])value);
                }

                int elementTypeCode = (int)Type.GetTypeCode(elementType);
                if (elementTypeCode <= 2 || elementTypeCode == 16)
                {
                    throw new NotSupportedException(string.Format("The specified array type ({0}) is not supported by MsgPack", elementType.Name));
                }

                MPackArray resultArray = new();
                Array inputArray = (Array)value;
                foreach (object obj in inputArray)
                {
                    resultArray.Add(From(obj));
                }
                return resultArray;
            }

            TypeCode code = Type.GetTypeCode(type);
            return code switch
            {
                TypeCode.Boolean => new MPack(value, MPackType.Bool),
                TypeCode.Char => new MPack(value, MPackType.UInt),
                TypeCode.SByte => new MPack(value, MPackType.SInt),
                TypeCode.Byte => new MPack(value, MPackType.UInt),
                TypeCode.Int16 => new MPack(value, MPackType.SInt),
                TypeCode.UInt16 => new MPack(value, MPackType.UInt),
                TypeCode.Int32 => new MPack(value, MPackType.SInt),
                TypeCode.UInt32 => new MPack(value, MPackType.UInt),
                TypeCode.Int64 => new MPack(value, MPackType.SInt),
                TypeCode.UInt64 => new MPack(value, MPackType.UInt),
                TypeCode.Single => new MPack(value, MPackType.Single),
                TypeCode.Double => new MPack(value, MPackType.Double),
                TypeCode.Decimal => new MPack((double)(decimal)value, MPackType.Double),
                TypeCode.String => new MPack(value, MPackType.String),
                _ => throw new NotSupportedException("Tried to create MPack object from unsupported type: " + type.Name),
            };
        }

        public object To(Type t)
        {
            if (ValueType == MPackType.Null)
            {
                return null;
            }
            if (t == typeof(object))
            {
                return Value;
            }

            // handle basic array types, ex. string[], int[], etc.
            // will fail if one of the child objects is of the incorrect type.
            if (t.IsArray)
            {
                Type elementType = t.GetElementType();
                if (elementType == typeof(byte))
                {
                    return (byte[])Value;
                }

                if (elementType == typeof(object))
                {
                    throw new ArgumentException("Array element type must not equal typeof(object).", nameof(t));
                }

                int elementTypeCode = (int)Type.GetTypeCode(elementType);
                if (elementTypeCode <= 2 || elementTypeCode == 16)
                {
                    throw new NotSupportedException(string.Format("Casting to an array of type {0} is not supported.", elementType.Name));
                }

                MPackArray mpackArray = Value as MPackArray;
                if (mpackArray == null)
                {
                    throw new ArgumentException(String.Format("Cannot conver MPack type {0} into type {1} (it is not an array).", ValueType, t.Name));
                }

                if (elementType == typeof(MPack))
                {
                    return mpackArray.ToArray();
                }

                int count = mpackArray.Count;
                Array objArray = Array.CreateInstance(elementType, count);
                for (int i = 0; i < count; i++)
                {
                    objArray.SetValue(mpackArray[i].To(elementType), i);
                }

                return objArray;
            }

            return Convert.ChangeType(Value, t);
        }

        public T To<T>()
        {
            return (T)To(typeof(T));
        }

        public T ToOrDefault<T>()
        {
            try
            {
                return To<T>();
            }
            catch
            {
                return default;
            }
        }

        #region boolean operators implemet IEquatable

        public static bool operator ==(MPack m1, MPack m2)
        {
            if (ReferenceEquals(m1, m2))
            {
                return true;
            }
            if (m1 is not null)
            {
                return m1.Equals(m2);
            }
            return false;
        }

        public static bool operator !=(MPack m1, MPack m2)
        {
            if (ReferenceEquals(m1, m2))
            {
                return false;
            }
            if (m1 is not null)
            {
                return !m1.Equals(m2);
            }
            return true;
        }

        #endregion boolean operators implemet IEquatable

        #region IConvertable From

        public static implicit operator MPack(bool value)
        {
            return From(value);
        }

        public static implicit operator MPack(float value)
        {
            return From(value);
        }

        public static implicit operator MPack(double value)
        {
            return From(value);
        }

        public static implicit operator MPack(byte value)
        {
            return From(value);
        }

        public static implicit operator MPack(ushort value)
        {
            return From(value);
        }

        public static implicit operator MPack(uint value)
        {
            return From(value);
        }

        public static implicit operator MPack(ulong value)
        {
            return From(value);
        }

        public static implicit operator MPack(sbyte value)
        {
            return From(value);
        }

        public static implicit operator MPack(short value)
        {
            return From(value);
        }

        public static implicit operator MPack(int value)
        {
            return From(value);
        }

        public static implicit operator MPack(long value)
        {
            return From(value);
        }

        public static implicit operator MPack(string value)
        {
            return From(value);
        }

        public static implicit operator MPack(byte[] value)
        {
            return From(value);
        }

        public static implicit operator MPack(MPack[] value)
        {
            return From(value);
        }

        #endregion IConvertable From

        #region IConvertable To

        public static explicit operator bool(MPack value)
        {
            return value.To<bool>();
        }

        public static explicit operator float(MPack value)
        {
            return value.To<float>();
        }

        public static explicit operator double(MPack value)
        {
            return value.To<double>();
        }

        public static explicit operator byte(MPack value)
        {
            return value.To<byte>();
        }

        public static explicit operator ushort(MPack value)
        {
            return value.To<ushort>();
        }

        public static explicit operator uint(MPack value)
        {
            return value.To<uint>();
        }

        public static explicit operator ulong(MPack value)
        {
            return value.To<ulong>();
        }

        public static explicit operator sbyte(MPack value)
        {
            return value.To<sbyte>();
        }

        public static explicit operator short(MPack value)
        {
            return value.To<short>();
        }

        public static explicit operator int(MPack value)
        {
            return value.To<int>();
        }

        public static explicit operator long(MPack value)
        {
            return value.To<long>();
        }

        public static explicit operator string(MPack value)
        {
            return value.To<string>();
        }

        public static explicit operator byte[](MPack value)
        {
            return value.To<byte[]>();
        }

        public static explicit operator MPack[](MPack value)
        {
            return value.To<MPack[]>();
        }

        #endregion IConvertable To

        public static MPack ParseFromBytes(byte[] array)
        {
            using MemoryStream ms = new(array);
            return ParseFromStream(ms);
        }

        public static MPack ParseFromStream(Stream stream)
        {
            return Reader.ParseFromStream(stream);
        }

        public static Task<MPack> ParseFromStreamAsync(Stream stream)
        {
            return ParseFromStreamAsync(stream, CancellationToken.None);
        }

        public static Task<MPack> ParseFromStreamAsync(Stream stream, CancellationToken token)
        {
            return Reader.ParseFromStreamAsync(stream, token);
        }

        public void EncodeToStream(Stream stream)
        {
            Writer.EncodeToStream(stream, this);
        }

        public Task EncodeToStreamAsync(Stream stream)
        {
            return EncodeToStreamAsync(stream, CancellationToken.None);
        }

        public async Task EncodeToStreamAsync(Stream stream, CancellationToken token)
        {
            MemoryStream ms = new();

            Writer.EncodeToStream(ms, this);

            ms.Position = 0;
            await ms.CopyToAsync(stream, 65535, token);
        }

        public byte[] EncodeToBytes()
        {
            using MemoryStream ms = new();

            Writer.EncodeToStream(ms, this);
            return ms.ToArray();
        }

        public bool Equals(MPack other)
        {
            if (other is null)
            {
                return false;
            }

            if (this is MPackArray arr1 && other is MPackArray arr2)
            {
                if (arr1.Count == arr2.Count)
                {
                    return arr1.SequenceEqual(arr2);
                }
            }
            else if (this is MPackMap map1 && other is MPackMap map2)
            {
                if (map1.Count == map2.Count)
                {
                    return map1.OrderBy(r => r.Key).SequenceEqual(map2.OrderBy(r => r.Key));
                }
            }
            else if ((ValueType == MPackType.SInt || ValueType == MPackType.UInt) &&
                     (other.ValueType == MPackType.SInt || other.ValueType == MPackType.UInt))
            {
                decimal xd = Convert.ToDecimal(Value);
                decimal yd = Convert.ToDecimal(other.Value);
                return xd == yd;
            }
            else
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is MPack pack)
            {
                return Equals(pack);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            if (Value == null)
            {
                return "null";
            }
            return Value.ToString();
        }

        TypeCode IConvertible.GetTypeCode()
        {
            if (ValueType == MPackType.Null)
            {
                return TypeCode.Object;
            }
            return Type.GetTypeCode(Value.GetType());
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return To<bool>();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return To<char>();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return To<sbyte>();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return To<byte>();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return To<short>();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return To<ushort>();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return To<int>();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return To<uint>();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return To<long>();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return To<ulong>();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return To<float>();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return To<double>();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return To<decimal>();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return To<DateTime>();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return To<string>();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return To(conversionType);
        }
    }
}