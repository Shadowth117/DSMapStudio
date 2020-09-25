using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public enum CollectionType
    {
        COLLECTION_LIST = 0,
        COLLECTION_EXTENDED_MESH = 1,
        COLLECTION_TRISAMPLED_HEIGHTFIELD = 2,
        COLLECTION_USER = 3,
        COLLECTION_SIMPLE_MESH = 4,
        COLLECTION_MESH_SHAPE = 5,
        COLLECTION_COMPRESSED_MESH = 6,
        COLLECTION_MAX = 7,
    }
    
    public class hkpShapeCollection : hkpShape
    {
        public bool m_disableWelding;
        public CollectionType m_collectionType;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            br.AssertUInt64(0);
            m_disableWelding = br.ReadBoolean();
            m_collectionType = (CollectionType)br.ReadByte();
            br.AssertUInt32(0);
            br.AssertUInt16(0);
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            bw.WriteUInt64(0);
            bw.WriteBoolean(m_disableWelding);
            bw.WriteUInt32(0);
            bw.WriteUInt16(0);
        }
    }
}