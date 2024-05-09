using SoulsFormats;
using System;
using System.Collections.Generic;

namespace StudioCore.MsbEditor.MSBTypes.MSBillyData;

public partial class MSBilly
{
    internal enum ModelType : uint
    {
        MapPiece = 0,
        Object = 1,
        Enemy = 2,
        Player = 4,
        Collision = 5,
    }

    /// <summary>
    /// Model files that are available for parts to use.
    /// </summary>
    public class ModelParam : Param<Model>, IMsbParam<IMsbModel>
    {
        internal override string Name => "MODEL_PARAM_ST";

        /// <summary>
        /// Models for fixed terrain and scenery.
        /// </summary>
        public List<Model.MapPiece> MapPieces { get; set; }

        /// <summary>
        /// Models for dynamic props.
        /// </summary>
        public List<Model.Object> Objects { get; set; }

        /// <summary>
        /// Models for non-player entities.
        /// </summary>
        public List<Model.Enemy> Enemies { get; set; }

        /// <summary>
        /// Models for player spawn points.
        /// </summary>
        public List<Model.Player> Players { get; set; }

        /// <summary>
        /// Models for physics collision.
        /// </summary>
        public List<Model.Collision> Collisions { get; set; }

        /// <summary>
        /// Creates an empty ModelParam.
        /// </summary>
        public ModelParam() : base()
        {
            MapPieces = new List<Model.MapPiece>();
            Objects = new List<Model.Object>();
            Enemies = new List<Model.Enemy>();
            Players = new List<Model.Player>();
            Collisions = new List<Model.Collision>();
        }

        internal override Model ReadEntry(BinaryReaderEx br)
        {
            ModelType type = br.GetEnum32<ModelType>(br.Position + 4);
            switch (type)
            {
                case ModelType.MapPiece:
                    return MapPieces.EchoAdd(new Model.MapPiece(br));

                case ModelType.Object:
                    return Objects.EchoAdd(new Model.Object(br));

                case ModelType.Enemy:
                    return Enemies.EchoAdd(new Model.Enemy(br));

                case ModelType.Player:
                    return Players.EchoAdd(new Model.Player(br));

                case ModelType.Collision:
                    return Collisions.EchoAdd(new Model.Collision(br));

                default:
                    throw new NotImplementedException($"Unimplemented model type: {type}");
            }
        }

        /// <summary>
        /// Adds a model to the appropriate list for its type; returns the model.
        /// </summary>
        public Model Add(Model model)
        {
            switch (model)
            {
                case Model.MapPiece m:
                    MapPieces.Add(m);
                    break;
                case Model.Object m:
                    Objects.Add(m);
                    break;
                case Model.Enemy m:
                    Enemies.Add(m);
                    break;
                case Model.Player m:
                    Players.Add(m);
                    break;
                case Model.Collision m:
                    Collisions.Add(m);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized type {model.GetType()}.", nameof(model));
            }
            return model;
        }
        IMsbModel IMsbParam<IMsbModel>.Add(IMsbModel item) => Add((Model)item);

        /// <summary>
        /// Returns every Model in the order they will be written.
        /// </summary>
        public override List<Model> GetEntries()
        {
            return SFUtil.ConcatAll<Model>(
                MapPieces, Objects, Enemies, Players, Collisions);
        }
        IReadOnlyList<IMsbModel> IMsbParam<IMsbModel>.GetEntries() => GetEntries();
    }

    /// <summary>
    /// A model file available for parts to reference.
    /// </summary>
    public abstract class Model : Entry, IMsbModel
    {
        private protected abstract ModelType Type { get; }

        /// <summary>
        /// A path to a .sib file, presumed to be some kind of editor placeholder.
        /// </summary>
        public string SibPath { get; set; }

        private int InstanceCount;

        private protected Model(string name)
        {
            Name = name;
            SibPath = "";
        }

        /// <summary>
        /// Creates a deep copy of the model.
        /// </summary>
        public Model DeepCopy()
        {
            return (Model)MemberwiseClone();
        }
        IMsbModel IMsbModel.DeepCopy() => DeepCopy();

        private protected Model(BinaryReaderEx br)
        {
        }

        internal override void Write(BinaryWriterEx bw, int id)
        {
        }

        /// <summary>
        /// Returns a string representation of the model.
        /// </summary>
        public override string ToString()
        {
            return $"{Name}";
        }

        /// <summary>
        /// A model for a static piece of visual map geometry.
        /// </summary>
        public class MapPiece : Model
        {
            private protected override ModelType Type => ModelType.MapPiece;

            /// <summary>
            /// Creates a MapPiece with default values.
            /// </summary>
            public MapPiece() : base("mXXXXBX") { }

            internal MapPiece(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// A model for a dynamic or interactible part.
        /// </summary>
        public class Object : Model
        {
            private protected override ModelType Type => ModelType.Object;

            /// <summary>
            /// Creates an Object with default values.
            /// </summary>
            public Object() : base("oXXXX") { }

            internal Object(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// A model for any non-player character.
        /// </summary>
        public class Enemy : Model
        {
            private protected override ModelType Type => ModelType.Enemy;

            /// <summary>
            /// Creates an Enemy with default values.
            /// </summary>
            public Enemy() : base("cXXXX") { }

            internal Enemy(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// A model for a player spawn point.
        /// </summary>
        public class Player : Model
        {
            private protected override ModelType Type => ModelType.Player;

            /// <summary>
            /// Creates a Player with default values.
            /// </summary>
            public Player() : base("c0000") { }

            internal Player(BinaryReaderEx br) : base(br) { }
        }

        /// <summary>
        /// A model for a static piece of physical map geometry.
        /// </summary>
        public class Collision : Model
        {
            private protected override ModelType Type => ModelType.Collision;

            /// <summary>
            /// Creates a Collision with default values.
            /// </summary>
            public Collision() : base("hXXXXBX") { }

            internal Collision(BinaryReaderEx br) : base(br) { }
        }
    }
}
