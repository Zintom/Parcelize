using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Zintom.Parcelize.Helpers;

namespace Zintom.Parcelize
{
    enum ParcelItemTypeCode
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
        /// <summary>
        /// Converts the current object into its <see cref="Parcel"/> form.
        /// </summary>
        /// <remarks><b>Implementors:</b> Please provide a '<c>public static YourType.FromParcel()</c>' method in order for callers to be able to deserialize your object.</remarks>
        public Parcel ToParcel();
    }

    /// <summary>
    /// An easy way to store, transmit, and read primitives and <see cref="IParcelable"/>'s.
    /// </summary>
    /// <remarks>Not currently thread-safe (specifically with <see cref="ReadNext"/> or <see cref="ReadNext{T}"/>)</remarks>
    public class Parcel
    {

        private readonly List<object?> _values = new();

        /// <summary>
        /// Inserts the given <paramref name="value"/> into the parcel.
        /// </summary>
        /// <param name="value">The variable that is inserted into the parcel (written when <see cref="Parcel.ToBytes"/> is called).</param>
        /// <returns>The current <see cref="Parcel"/>, used for chaining calls.</returns>
        public Parcel WriteInt(int? value)
        {
            _values.Add(value);
            return this;
        }

        /// <inheritdoc cref="WriteInt(int?)"/>
        public Parcel WriteFloat(float? value)
        {
            _values.Add(value);
            return this;
        }

        /// <inheritdoc cref="WriteInt(int?)"/>
        public Parcel WriteString(string? value)
        {
            _values.Add(value);
            return this;
        }

        /// <inheritdoc cref="WriteInt(int?)"/>
        public Parcel WriteVector2(Vector2? value)
        {
            _values.Add(value);
            return this;
        }

        ///// <summary>
        ///// Parcelizes the given <paramref name="parcelable"/>. Use <c>YourType.FromParcel((<see cref="Parcel"/>)p.ReadNext())</c> to deserialize.
        ///// </summary>
        ///// <param name="parcelable"></param>
        //public Parcel WriteParcelable(IParcelable? parcelable)
        //{
        //    _values.Add(parcelable);
        //    return this;
        //}

        /// <summary>
        /// Writes the given <paramref name="parcel"/> as a sub-parcel to this <see cref="Parcel"/>
        /// </summary>
        /// <remarks>Use <c>YourType.FromParcel(p.ReadNext&lt;<see cref="Parcel"/>&gt;())</c> to deserialize.</remarks>
        /// <param name="parcelable"></param>
        public Parcel WriteParcel(Parcel? parcel)
        {
            _values.Add(parcel);
            return this;
        }

        /// <summary>
        /// Reads the next item from the parcel.
        /// </summary>
        public object? ReadNext()
        {
            if (_values.Count == 0) throw new Exception("No more items to read.");

            object? value = _values[0];
            _values.RemoveAt(0);

            return value;
        }

        /// <summary>
        /// Reads the next item from the parcel as <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The next <see langword="object"/> from the parcel, cast to a <typeparamref name="T"/>.</returns>
        public T? ReadNext<T>()
        {
            if (_values.Count == 0) throw new Exception("No more items to read.");

            object? value = _values[0];
            _values.RemoveAt(0);

            return (T?)value;
        }

        /// <summary>
        /// Decodes the given <paramref name="source"/> bytes into a <see cref="Parcel"/>.
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

                if (type == ParcelItemTypeCode.Empty)
                {
                    // Empty type codes are treated as null.
                    parcel._values.Add(null);
                    continue;
                }

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
                        // We assume Object is Parcel as other special
                        // cases will have been handled by now.
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
                object? value = _values[i];
                ParcelItemTypeCode type = (ParcelItemTypeCode)Type.GetTypeCode(value?.GetType() ?? null);

                if (type == ParcelItemTypeCode.Object)
                {
                    if (value is Vector2)
                        type = ParcelItemTypeCode.Vector2;
                }

                byte[] typeAsBytes = BitConverter.GetBytes((int)type);

                byte[] itemAsBytes;

                if (value == null)
                {
                    itemAsBytes = Array.Empty<byte>();
                }
                else if (value is Parcel parcel)
                {
                    itemAsBytes = parcel.ToBytes();
                }
                else if (value is Vector2 vector2)
                {
                    itemAsBytes = Encoding.ASCII.GetBytes(vector2.X + "," + vector2.Y);
                }
                else
                {
                    // No specific encoding.
                    itemAsBytes = Encoding.ASCII.GetBytes(value.ToString() ?? "");
                }

                byte[] length = BitConverter.GetBytes(itemAsBytes.Length);

                // Each serialized parcel item is in the order: length, typecode, value

                jaggedByteArray[i] = ArrayHelpers.CombineArrays(length, typeAsBytes, itemAsBytes);
            }

            return ArrayHelpers.CombineArrays(jaggedByteArray);
        }

    }
}
