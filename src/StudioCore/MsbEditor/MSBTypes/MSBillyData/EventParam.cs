using SoulsFormats;
using System;
using System.Collections.Generic;

namespace StudioCore.MsbEditor.MSBTypes.MSBillyData;
public partial class MSBilly
{
    internal enum EventType : uint
    {
        Light = 0,
        Sound = 1,
        SFX = 2,
        Wind = 3,
        Treasure = 4,
        Generator = 5,
        Message = 6,
        ObjAct = 7,
        SpawnPoint = 8,
        MapOffset = 9,
        Navmesh = 10,
        Environment = 11,
        PseudoMultiplayer = 12,
    }

    /// <summary>
    /// Contains abstract entities that control various dynamic elements in the map.
    /// </summary>
    public class EventParam : Param<Event>, IMsbParam<IMsbEvent>
    {
        internal override string Name => "EVENT_PARAM_ST";

        /// <summary>
        /// Unknown exactly what this is for.
        /// </summary>
        public List<Event.SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Creates an empty EventParam.
        /// </summary>
        public EventParam() : base()
        {
            SpawnPoints = new List<Event.SpawnPoint>();
        }

        /// <summary>
        /// Adds an event to the appropriate list for its type; returns the event.
        /// </summary>
        public Event Add(Event evnt)
        {
            switch (evnt)
            {
                case Event.SpawnPoint e: SpawnPoints.Add(e); break;

                default:
                    throw new ArgumentException($"Unrecognized type {evnt.GetType()}.", nameof(evnt));
            }
            return evnt;
        }
        IMsbEvent IMsbParam<IMsbEvent>.Add(IMsbEvent item) => Add((Event)item);

        /// <summary>
        /// Returns a list of every event in the order they'll be written.
        /// </summary>
        public override List<Event> GetEntries()
        {
            return SFUtil.ConcatAll<Event>(SpawnPoints);
        }
        IReadOnlyList<IMsbEvent> IMsbParam<IMsbEvent>.GetEntries() => GetEntries();

        internal override Event ReadEntry(BinaryReaderEx br)
        {
            EventType type = br.GetEnum32<EventType>(br.Position + 8);
            switch (type)
            {
                case EventType.SpawnPoint:
                    return SpawnPoints.EchoAdd(new Event.SpawnPoint(br));
                default:
                    throw new NotImplementedException($"Unsupported event type: {type}");
            }
        }
    }

    /// <summary>
    /// Common data for all dynamic events.
    /// </summary>
    public abstract class Event : Entry, IMsbEvent
    {
        private protected abstract EventType Type { get; }

        /// <summary>
        /// Identifies the event in external files.
        /// </summary>
        public int EntityID { get; set; }

        private protected Event(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a deep copy of the event.
        /// </summary>
        public Event DeepCopy()
        {
            var evnt = (Event)MemberwiseClone();
            DeepCopyTo(evnt);
            return evnt;
        }
        IMsbEvent IMsbEvent.DeepCopy() => DeepCopy();

        private protected virtual void DeepCopyTo(Event evnt) { }

        private protected Event(BinaryReaderEx br)
        {
        }

        private protected abstract void ReadTypeData(BinaryReaderEx br);

        internal override void Write(BinaryWriterEx bw, int id)
        {
        }

        private protected abstract void WriteTypeData(BinaryWriterEx bw);

        /// <summary>
        /// Returns the type and name of the event.
        /// </summary>
        public override string ToString()
        {
            return $"{Type} {Name}";
        }

        /// <summary>
        /// Unknown what this accomplishes beyond just having the region.
        /// </summary>
        public class SpawnPoint : Event
        {
            private protected override EventType Type => EventType.SpawnPoint;

            /// <summary>
            /// Point for the SpawnPoint to spawn at.
            /// </summary>
            [MSBReference(ReferenceType = typeof(Region))]
            public string SpawnPointName { get; set; }
            private int SpawnPointIndex;

            /// <summary>
            /// Creates a SpawnPoint with default values.
            /// </summary>
            public SpawnPoint() : base($"{nameof(Event)}: {nameof(SpawnPoint)}") { }

            internal SpawnPoint(BinaryReaderEx br) : base(br) { }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                SpawnPointIndex = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(SpawnPointIndex);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
        }
    }
}
