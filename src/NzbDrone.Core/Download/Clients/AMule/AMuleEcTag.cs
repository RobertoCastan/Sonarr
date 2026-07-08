using System.Collections.Generic;
using System.Text;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleEcTag
    {
        public const byte TypeUnknown = 0;
        public const byte TypeCustom = 1;
        public const byte TypeUInt8 = 2;
        public const byte TypeUInt16 = 3;
        public const byte TypeUInt32 = 4;
        public const byte TypeUInt64 = 5;
        public const byte TypeString = 6;
        public const byte TypeHash16 = 9;

        public ushort Name { get; set; }
        public byte Type { get; set; }
        public byte[] Data { get; set; }
        public List<AMuleEcTag> Children { get; set; } = new ();

        public string StringValue => Data == null ? null : Encoding.UTF8.GetString(Data).TrimEnd('\0');

        public ulong IntegerValue
        {
            get
            {
                if (Data == null)
                {
                    return 0;
                }

                ulong value = 0;
                foreach (var b in Data)
                {
                    value = (value << 8) | b;
                }

                return value;
            }
        }

        public string HashValue => Data == null ? null : System.BitConverter.ToString(Data).Replace("-", string.Empty);

        public AMuleEcTag Find(ushort name)
        {
            return Children.Find(v => v.Name == name);
        }

        public static AMuleEcTag Empty(ushort name)
        {
            return new AMuleEcTag { Name = name, Type = TypeUnknown, Data = System.Array.Empty<byte>() };
        }

        public static AMuleEcTag String(ushort name, string value)
        {
            return new AMuleEcTag { Name = name, Type = TypeString, Data = Encoding.UTF8.GetBytes((value ?? string.Empty) + '\0') };
        }

        public static AMuleEcTag UInt(ushort name, ulong value)
        {
            if (value <= byte.MaxValue)
            {
                return new AMuleEcTag { Name = name, Type = TypeUInt8, Data = new[] { (byte)value } };
            }

            if (value <= ushort.MaxValue)
            {
                return new AMuleEcTag { Name = name, Type = TypeUInt16, Data = new[] { (byte)(value >> 8), (byte)value } };
            }

            if (value <= uint.MaxValue)
            {
                return new AMuleEcTag { Name = name, Type = TypeUInt32, Data = new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value } };
            }

            return new AMuleEcTag
            {
                Name = name,
                Type = TypeUInt64,
                Data = new[] { (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32), (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value }
            };
        }

        public static AMuleEcTag Hash(ushort name, byte[] value)
        {
            return new AMuleEcTag { Name = name, Type = TypeHash16, Data = value };
        }
    }
}
