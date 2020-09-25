using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public class hclGatherSomeVerticesOperator : hclOperator
    {
        public List<hclGatherSomeVerticesOperatorVertexPair> m_vertexPairs;
        public uint m_inputBufferIdx;
        public uint m_outputBufferIdx;
        public bool m_gatherNormals;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_vertexPairs = des.ReadClassArray<hclGatherSomeVerticesOperatorVertexPair>(br);
            m_inputBufferIdx = br.ReadUInt32();
            m_outputBufferIdx = br.ReadUInt32();
            m_gatherNormals = br.ReadBoolean();
            br.AssertUInt32(0);
            br.AssertUInt16(0);
            br.AssertByte(0);
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            bw.WriteUInt32(m_inputBufferIdx);
            bw.WriteUInt32(m_outputBufferIdx);
            bw.WriteBoolean(m_gatherNormals);
            bw.WriteUInt32(0);
            bw.WriteUInt16(0);
            bw.WriteByte(0);
        }
    }
}