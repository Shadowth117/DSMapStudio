using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public enum PathPointBits
    {
        EDGE_TYPE_USER_START = 1,
        EDGE_TYPE_USER_END = 2,
        EDGE_TYPE_SEGMENT_START = 4,
        EDGE_TYPE_SEGMENT_END = 8,
    }
    
    public enum ReferenceFrame
    {
        REFERENCE_FRAME_WORLD = 0,
        REFERENCE_FRAME_SECTION_LOCAL = 1,
        REFERENCE_FRAME_SECTION_FIXED = 2,
    }
    
    public class hkaiPath : hkReferencedObject
    {
        public List<hkaiPathPathPoint> m_points;
        public ReferenceFrame m_referenceFrame;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_points = des.ReadClassArray<hkaiPathPathPoint>(br);
            m_referenceFrame = (ReferenceFrame)br.ReadByte();
            br.AssertUInt32(0);
            br.AssertUInt16(0);
            br.AssertByte(0);
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            bw.WriteUInt32(0);
            bw.WriteUInt16(0);
            bw.WriteByte(0);
        }
    }
}