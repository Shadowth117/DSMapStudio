using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace StudioCore.MsbEditor.MSBTypes.MSBillyData;
public partial class MSBilly
{
    internal enum PartType : uint
    {
        MapPiece = 0,
        Object = 1,
        Enemy = 2,
        Player = 4,
        Collision = 5,
        Navmesh = 8,
        DummyObject = 9,
        DummyEnemy = 10,
        ConnectCollision = 11,
    }

    /// <summary>
    /// All instances of concrete things in the map.
    /// </summary>
    public class PartsParam : Param<Part>, IMsbParam<IMsbPart>
    {
        internal override string Name => "PARTS_PARAM_ST";

        /// <summary>
        /// All of the fixed visual geometry of the map.
        /// </summary>
        public List<Part.MapPiece> MapPieces { get; set; }

        /// <summary>
        /// Dynamic props and interactive things.
        /// </summary>
        public List<Part.Object> Objects { get; set; }

        /// <summary>
        /// All non-player characters.
        /// </summary>
        public List<Part.Enemy> Enemies { get; set; }

        /// <summary>
        /// These have something to do with player spawn points.
        /// </summary>
        public List<Part.Player> Players { get; set; }

        /// <summary>
        /// Invisible physical geometry of the map.
        /// </summary>
        public List<Part.Collision> Collisions { get; set; }

        /// <summary>
        /// Creates an empty PartsParam.
        /// </summary>
        public PartsParam() : base()
        {
            MapPieces = new List<Part.MapPiece>();
            Objects = new List<Part.Object>();
            Enemies = new List<Part.Enemy>();
            Players = new List<Part.Player>();
            Collisions = new List<Part.Collision>();
        }

        /// <summary>
        /// Adds a part to the appropriate list for its type; returns the part.
        /// </summary>
        public Part Add(Part part)
        {
            switch (part)
            {
                case Part.MapPiece p:
                    MapPieces.Add(p);
                    break;
                case Part.Object p:
                    Objects.Add(p);
                    break;
                case Part.Enemy p:
                    Enemies.Add(p);
                    break;
                case Part.Player p:
                    Players.Add(p);
                    break;
                case Part.Collision p:
                    Collisions.Add(p);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized type {part.GetType()}.", nameof(part));
            }
            return part;
        }
        IMsbPart IMsbParam<IMsbPart>.Add(IMsbPart item) => Add((Part)item);

        /// <summary>
        /// Returns every Part in the order they'll be written.
        /// </summary>
        public override List<Part> GetEntries()
        {
            return SFUtil.ConcatAll<Part>(
                MapPieces, Objects, Enemies, Players, Collisions);
        }
        IReadOnlyList<IMsbPart> IMsbParam<IMsbPart>.GetEntries() => GetEntries();

        internal override Part ReadEntry(BinaryReaderEx br)
        {
            PartType type = br.GetEnum32<PartType>(br.Position + 4);
            switch (type)
            {
                case PartType.MapPiece:
                    return MapPieces.EchoAdd(new Part.MapPiece(br));

                case PartType.Object:
                    return Objects.EchoAdd(new Part.Object(br));

                case PartType.Enemy:
                    return Enemies.EchoAdd(new Part.Enemy(br));

                case PartType.Player:
                    return Players.EchoAdd(new Part.Player(br));

                case PartType.Collision:
                    return Collisions.EchoAdd(new Part.Collision(br));

                default:
                    throw new NotImplementedException($"Unimplemented part type: {type}");
            }
        }
    }

    /// <summary>
    /// Common information for all concrete entities.
    /// </summary>
    public abstract class Part : Entry, IMsbPart
    {
        private protected abstract PartType Type { get; }

        /// <summary>
        /// The model of the Part, corresponding to an entry in the ModelParam.
        /// </summary>
        public string ModelName { get; set; }
        public int ModelIndex;

        /// <summary>
        /// Location of the part.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Rotation of the part, in degrees.
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// Scale of the part, only meaningful for map pieces and objects.
        /// </summary>
        public Vector3 Scale { get; set; }

        private protected Part(string name)
        {
        }

        /// <summary>
        /// Creates a deep copy of the part.
        /// </summary>
        public Part DeepCopy()
        {
            var part = (Part)MemberwiseClone();
            DeepCopyTo(part);
            return part;
        }
        IMsbPart IMsbPart.DeepCopy() => DeepCopy();

        private protected virtual void DeepCopyTo(Part part) { }

        private protected Part(BinaryReaderEx br)
        {
        }

        private void ReadEntityData(BinaryReaderEx br)
        {
        }

        private protected abstract void ReadTypeData(BinaryReaderEx br);

        internal override void Write(BinaryWriterEx bw, int id)
        {
        }

        private void WriteEntityData(BinaryWriterEx bw)
        {
        }

        private protected abstract void WriteTypeData(BinaryWriterEx bw);

        /// <summary>
        /// A visible but not physical model making up the map.
        /// </summary>
        public class MapPiece : Part
        {
            private protected override PartType Type => PartType.MapPiece;

            /// <summary>
            /// Creates a MapPiece with default values.
            /// </summary>
            public MapPiece() : base("mXXXXBX") { }

            internal MapPiece(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
            }
        }

        /// <summary>
        /// Common base data for objects and dummy objects.
        /// </summary>
        public abstract class ObjectBase : Part
        {
            private protected ObjectBase() : base("oXXXX_XXXX") { }

            private protected ObjectBase(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
            }
        }

        /// <summary>
        /// A dynamic or interactible part of the map.
        /// </summary>
        public class Object : ObjectBase
        {
            private protected override PartType Type => PartType.Object;

            /// <summary>
            /// Creates an Object with default values.
            /// </summary>
            public Object() : base() { }

            internal Object(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// Common base data for enemies and dummy enemies.
        /// </summary>
        public abstract class EnemyBase : Part
        {
            private protected EnemyBase() : base("cXXXX_XXXX")
            {
            }

            private protected override void DeepCopyTo(Part part)
            {
                var enemy = (EnemyBase)part;
            }

            private protected EnemyBase(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
            }
        }

        /// <summary>
        /// Any living entity besides the player character.
        /// </summary>
        public class Enemy : EnemyBase
        {
            private protected override PartType Type => PartType.Enemy;

            /// <summary>
            /// Creates an Enemy with default values.
            /// </summary>
            public Enemy() : base() { }

            internal Enemy(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// Unknown exactly what these do.
        /// </summary>
        public class Player : Part
        {
            private protected override PartType Type => PartType.Player;

            /// <summary>
            /// Creates a Player with default values.
            /// </summary>
            public Player() : base("c0000_XXXX") { }

            internal Player(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
            }
        }

        /// <summary>
        /// Invisible but physical geometry.
        /// </summary>
        public class Collision : Part
        {
            private protected override PartType Type => PartType.Collision;

            /// <summary>
            /// Controls displays of the map name on screen or the loading menu.
            /// </summary>
            public short MapNameID { get; set; }

            /// <summary>
            /// Creates a Collision with default values.
            /// </summary>
            public Collision() : base("hXXXXBX")
            {
            }

            private protected override void DeepCopyTo(Part part)
            {
                var collision = (Collision)part;
            }

            internal Collision(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
            }

        }
    }
}

