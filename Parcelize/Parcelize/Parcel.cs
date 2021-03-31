using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Zintom.Parcelize
{
    public enum ParcelItemTypeCode
    {
        Empty = 0,
        Object = 1,
        DBNull = 2,
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10,
        Int64 = 11,
        UInt64 = 12,
        Single = 13,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18,
        /// <summary>
        /// A <see cref="Microsoft.Xna.Framework.Point"/>
        /// </summary>
        Point = 19,
        /// <summary>
        /// A <see cref="System.Numerics.Vector2"/>
        /// </summary>
        Vector2 = 20
    }

    /// <summary>
    /// This object can be parcelized.
    /// </summary>
    public interface IParcelable
    {
        public Parcel ToParcel();
    }

    /// <summary>
    /// An easy way to store, transmit, and read primitives and <see cref="IParcelable"/>'s.
    /// </summary>
    public class Parcel
    {

        private readonly List<object> _values = new();

        public Parcel WriteInt(int value)
        {
            _values.Add(value);
            return this;
        }

        public Parcel WriteFloat(float value)
        {
            _values.Add(value);
            return this;
        }

        public Parcel WriteString(string value)
        {
            _values.Add(value);
            return this;
        }

        public Parcel WriteVector2(Vector2 value)
        {
            _values.Add(value);
            return this;
        }

        /// <summary>
        /// Parcelizes the given <paramref name="parcelable"/>. Use <c>YourType.FromParcel((<see cref="Parcel"/>)p.ReadNext())</c> to deserialize.
        /// </summary>
        /// <param name="parcelable"></param>
        public Parcel WriteParcelable(IParcelable parcelable)
        {
            _values.Add(parcelable);
            return this;
        }

        public Parcel WriteParcel(Parcel parcel)
        {
            _values.Add(parcel);
            return this;
        }

        /// <summary>
        /// Reads the next value from the parcel.
        /// </summary>
        public object ReadNext()
        {
            if (_values.Count == 0) throw new Exception("No more items to read.");

            object value = _values[0];
            _values.RemoveAt(0);

            return value;
        }

        /// <summary>
        /// Reads the next value from the parcel.
        /// </summary>
        public T ReadNext<T>()
        {
            if (_values.Count == 0) throw new Exception("No more items to read.");

            object value = _values[0];
            _values.RemoveAt(0);

            return (T)value;
        }

        /// <summary>
        /// Converts the given <paramref name="source"/> bytes into their <see cref="Parcel"/> form.
        /// </summary>
        public static Parcel FromBytes(ReadOnlySpan<byte> source)
        {
            var parcel = new Parcel();

            int bytesRead = 0;
            while (bytesRead < source.Length)
            {
                // Read length prefix
                int length = BitConverter.ToInt32(source.Slice(bytesRead));
                bytesRead += sizeof(int);

                // Read item type
                ParcelItemTypeCode type = (ParcelItemTypeCode)BitConverter.ToInt32(source.Slice(bytesRead));
                bytesRead += sizeof(int);

                // Read item data(value)
                ReadOnlySpan<byte> itemBytes = source.Slice(bytesRead, length);
                bytesRead += length;

                switch (type)
                {
                    case ParcelItemTypeCode.Int32:
                        parcel._values.Add(int.Parse(Encoding.ASCII.GetString(itemBytes)));
                        break;
                    case ParcelItemTypeCode.Single:
                        parcel._values.Add(float.Parse(Encoding.ASCII.GetString(itemBytes)));
                        break;
                    case ParcelItemTypeCode.String:
                        parcel._values.Add(Encoding.ASCII.GetString(itemBytes));
                        break;
                    case ParcelItemTypeCode.Vector2:
                        string encodedVector2 = Encoding.ASCII.GetString(itemBytes);
                        float vecX = float.Parse(encodedVector2.Substring(0, encodedVector2.IndexOf(",")));
                        float vecY = float.Parse(encodedVector2.Substring(encodedVector2.IndexOf(",") + 1));
                        parcel._values.Add(new Vector2(vecX, vecY));
                        break;
                    case ParcelItemTypeCode.Object:
                        // We assume the object a Parcel as
                        // that is the only non-primitive type that can be added into the Parcel.
                        parcel._values.Add(Parcel.FromBytes(itemBytes));
                        break;
                }
            }

            return parcel;
        }

        /// <summary>
        /// Serializes this <see cref="Parcel"/> into bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            byte[][] jaggedByteArray = new byte[_values.Count][];

            for (int i = 0; i < _values.Count; i++)
            {
                ParcelItemTypeCode type = (ParcelItemTypeCode)Type.GetTypeCode(_values[i].GetType());

                if (type == ParcelItemTypeCode.Object)
                {
                    if (_values[i] is Vector2)
                        type = ParcelItemTypeCode.Vector2;
                }

                byte[] typeAsBytes = BitConverter.GetBytes((int)type);

                byte[] itemAsBytes =
                    _values[i] is IParcelable parcelable
                    ? parcelable.ToParcel().ToBytes()
                    : _values[i] is Parcel parcel
                    ? parcel.ToBytes()
                    : _values[i] is Vector2 vector2
                    ? Encoding.ASCII.GetBytes(vector2.X + "," + vector2.Y)
                    : Encoding.ASCII.GetBytes(_values[i].ToString()!);

                byte[] length = BitConverter.GetBytes(itemAsBytes.Length);

                // Each serialized parcel item is in the format: length, typecode, value

                jaggedByteArray[i] = ArrayHelpers.CombineArrays(length, typeAsBytes, itemAsBytes);
            }

            return ArrayHelpers.CombineArrays(jaggedByteArray);
        }

    }
}
