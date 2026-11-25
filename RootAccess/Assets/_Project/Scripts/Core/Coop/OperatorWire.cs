using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// Binary codec for the LAN link: snapshots, level maps and commands become byte arrays that the Netcode named
    /// messages carry. Kept free of Netcode types so it can be unit-tested; every message starts with a version byte.
    /// </summary>
    public static class OperatorWire
    {
        public const byte Version = 1;

        public static byte[] EncodeSnapshot(OperatorSnapshot snapshot)
        {
            using var stream = new MemoryStream(64 + snapshot.Entities.Count * 24);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Version);
            Write(writer, snapshot.PlayerPosition);
            writer.Write(snapshot.Energy);
            writer.Write(snapshot.MaxEnergy);
            writer.Write((ushort)snapshot.Entities.Count);
            foreach (OperatorEntity entity in snapshot.Entities)
            {
                writer.Write(entity.Id ?? string.Empty);
                writer.Write((byte)entity.Kind);
                Write(writer, entity.Position);
                writer.Write((byte)((entity.Active ? 1 : 0) | (entity.Available ? 2 : 0)));
            }

            return stream.ToArray();
        }

        /// <summary>Decodes into <paramref name="into"/> (reused buffer on the receiving side).</summary>
        public static void DecodeSnapshot(byte[] data, OperatorSnapshot into)
        {
            using var reader = Reader(data);
            into.PlayerPosition = ReadVector(reader);
            into.Energy = reader.ReadSingle();
            into.MaxEnergy = reader.ReadSingle();
            int count = reader.ReadUInt16();
            into.Entities.Clear();
            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                var kind = (OperatorTargetKind)reader.ReadByte();
                Vector2 position = ReadVector(reader);
                byte flags = reader.ReadByte();
                into.Entities.Add(new OperatorEntity(id, kind, position, (flags & 1) != 0, (flags & 2) != 0));
            }
        }

        public static byte[] EncodeLevelMap(OperatorLevelMap map)
        {
            using var stream = new MemoryStream(64 + map.Solid.Length / 8);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Version);
            writer.Write(map.LevelName);
            Write(writer, map.Bounds.position);
            Write(writer, map.Bounds.size);
            writer.Write((ushort)map.Width);
            writer.Write((ushort)map.Height);
            var packed = new byte[(map.Solid.Length + 7) / 8];
            for (int i = 0; i < map.Solid.Length; i++)
                if (map.Solid[i]) packed[i >> 3] |= (byte)(1 << (i & 7));
            writer.Write(packed.Length);
            writer.Write(packed);
            return stream.ToArray();
        }

        public static OperatorLevelMap DecodeLevelMap(byte[] data)
        {
            using var reader = Reader(data);
            string name = reader.ReadString();
            Vector2 position = ReadVector(reader);
            Vector2 size = ReadVector(reader);
            int width = reader.ReadUInt16();
            int height = reader.ReadUInt16();
            byte[] packed = reader.ReadBytes(reader.ReadInt32());
            var solid = new bool[width * height];
            for (int i = 0; i < solid.Length; i++) solid[i] = (packed[i >> 3] & (1 << (i & 7))) != 0;
            return new OperatorLevelMap(name, new Rect(position, size), width, height, solid);
        }

        public static byte[] EncodeCommand(OperatorCommand command)
        {
            using var stream = new MemoryStream(32);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Version);
            writer.Write(command.TargetId ?? string.Empty);
            return stream.ToArray();
        }

        public static OperatorCommand DecodeCommand(byte[] data)
        {
            using var reader = Reader(data);
            return new OperatorCommand(reader.ReadString());
        }

        private static BinaryReader Reader(byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Empty operator message.");
            var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);
            byte version = reader.ReadByte();
            if (version != Version) throw new InvalidDataException($"Operator message version {version}, expected {Version}.");
            return reader;
        }

        private static void Write(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        private static Vector2 ReadVector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
    }
}
