using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public enum EventRangeMode
    {
        EVENT_MODE_SEND_ON_ENTER_RANGE = 0,
        EVENT_MODE_SEND_WHEN_IN_RANGE = 1,
    }
    
    public class hkbEventRangeData : IHavokObject
    {
        public float m_upperBound;
        public hkbEventProperty m_event;
        public EventRangeMode m_eventMode;
        
        public virtual void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            m_upperBound = br.ReadSingle();
            br.AssertUInt32(0);
            m_event = new hkbEventProperty();
            m_event.Read(des, br);
            m_eventMode = (EventRangeMode)br.ReadSByte();
            br.AssertUInt32(0);
            br.AssertUInt16(0);
            br.AssertByte(0);
        }
        
        public virtual void Write(BinaryWriterEx bw)
        {
            bw.WriteSingle(m_upperBound);
            bw.WriteUInt32(0);
            m_event.Write(bw);
            bw.WriteUInt32(0);
            bw.WriteUInt16(0);
            bw.WriteByte(0);
        }
    }
}