using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public static class AMuleEcCodec
    {
        private const uint Flags = 0x20;

        public static byte[] Encode(AMuleEcPacket packet)
        {
            using var body = new MemoryStream();
            body.WriteByte(packet.OpCode);
            WriteUInt16(body, (ushort)packet.Tags.Count);

            foreach (var tag in packet.Tags)
            {
                WriteTag(body, tag);
            }

            using var framed = new MemoryStream();
            WriteUInt32(framed, Flags);
            WriteUInt32(framed, (uint)body.Length);
            body.Position = 0;
            body.CopyTo(framed);

            return framed.ToArray();
        }

        public static AMuleEcPacket Decode(byte[] payload)
        {
            using var stream = new MemoryStream(payload);
            var opCode = ReadByte(stream);
            var tagCount = ReadUInt16(stream);
            var packet = new AMuleEcPacket(opCode);

            for (var i = 0; i < tagCount; i++)
            {
                packet.Tags.Add(ReadTag(stream));
            }

            return packet;
        }

        public static int GetFrameLength(byte[] header)
        {
            if (header.Length != 8)
            {
                throw new ArgumentException("EC header must be 8 bytes.", nameof(header));
            }

            var flags = ReadUInt32(header, 0);

            if ((flags & 0x01) != 0)
            {
                throw new NotSupportedException("Compressed aMule EC packets are not supported.");
            }

            if ((flags & 0x02) != 0)
            {
                throw new NotSupportedException("UTF-8 encoded aMule EC numbers are not supported.");
            }

            return (int)ReadUInt32(header, 4);
        }

        public static byte[] CreatePasswordHash(string ecPassword, ulong salt)
        {
            var storedPasswordHash = IsMd5(ecPassword) ? ecPassword.ToLowerInvariant() : Md5Hex(ecPassword);
            var saltHash = Md5Hex(salt.ToString("X"));
            return HexToBytes(Md5Hex(storedPasswordHash + saltHash));
        }

        private static void WriteTag(Stream stream, AMuleEcTag tag)
        {
            var children = tag.Children ?? new List<AMuleEcTag>();
            var hasChildren = children.Any();
            var data = tag.Data ?? Array.Empty<byte>();

            using var body = new MemoryStream();

            if (hasChildren)
            {
                WriteUInt16(body, (ushort)children.Count);

                foreach (var child in children)
                {
                    WriteTag(body, child);
                }
            }

            body.Write(data, 0, data.Length);

            WriteUInt16(stream, (ushort)((tag.Name << 1) | (hasChildren ? 1 : 0)));
            stream.WriteByte(tag.Type);
            WriteUInt32(stream, (uint)(body.Length - (hasChildren ? 2 : 0)));
            body.Position = 0;
            body.CopyTo(stream);
        }

        private static AMuleEcTag ReadTag(Stream stream)
        {
            var rawName = ReadUInt16(stream);
            var hasChildren = (rawName & 1) == 1;
            var name = (ushort)(rawName >> 1);
            var type = ReadByte(stream);
            var length = ReadUInt32(stream);
            var remaining = length;
            var tag = new AMuleEcTag { Name = name, Type = type };

            if (hasChildren)
            {
                var childCount = ReadUInt16(stream);

                for (var i = 0; i < childCount; i++)
                {
                    var before = stream.Position;
                    tag.Children.Add(ReadTag(stream));
                    remaining -= (uint)(stream.Position - before);
                }
            }

            tag.Data = ReadBytes(stream, (int)remaining);

            return tag;
        }

        private static byte ReadByte(Stream stream)
        {
            var value = stream.ReadByte();

            if (value < 0)
            {
                throw new EndOfStreamException();
            }

            return (byte)value;
        }

        private static byte[] ReadBytes(Stream stream, int length)
        {
            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, length);

            if (read != length)
            {
                throw new EndOfStreamException();
            }

            return buffer;
        }

        private static ushort ReadUInt16(Stream stream)
        {
            var buffer = ReadBytes(stream, 2);
            return (ushort)((buffer[0] << 8) | buffer[1]);
        }

        private static uint ReadUInt32(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) | ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        private static uint ReadUInt32(Stream stream)
        {
            return ReadUInt32(ReadBytes(stream, 4), 0);
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        private static string Md5Hex(string value)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty))).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool IsMd5(string value)
        {
            return value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-fA-F0-9]{32}$");
        }

        private static byte[] HexToBytes(string value)
        {
            var result = new byte[value.Length / 2];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }

            return result;
        }
    }
}
