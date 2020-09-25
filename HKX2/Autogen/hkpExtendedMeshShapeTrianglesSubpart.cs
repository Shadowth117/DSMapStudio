using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public class hkpExtendedMeshShapeTrianglesSubpart : hkpExtendedMeshShapeSubpart
    {
        public int m_numTriangleShapes;
        public int m_numVertices;
        public ushort m_vertexStriding;
        public int m_triangleOffset;
        public ushort m_indexStriding;
        public IndexStridingType m_stridingType;
        public sbyte m_flipAlternateTriangles;
        public Vector4 m_extrusion;
        public Matrix4x4 m_transform;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_numTriangleShapes = br.ReadInt32();
            br.AssertUInt64(0);
            br.AssertUInt32(0);
            m_numVertices = br.ReadInt32();
            br.AssertUInt64(0);
            br.AssertUInt32(0);
            m_vertexStriding = br.ReadUInt16();
            br.AssertUInt16(0);
            m_triangleOffset = br.ReadInt32();
            m_indexStriding = br.ReadUInt16();
            m_stridingType = (IndexStridingType)br.ReadSByte();
            m_flipAlternateTriangles = br.ReadSByte();
            br.AssertUInt32(0);
            m_extrusion = des.ReadVector4(br);
            m_transform = des.ReadQSTransform(br);
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            bw.WriteInt32(m_numTriangleShapes);
            bw.WriteUInt64(0);
            bw.WriteUInt32(0);
            bw.WriteInt32(m_numVertices);
            bw.WriteUInt64(0);
            bw.WriteUInt32(0);
            bw.WriteUInt16(m_vertexStriding);
            bw.WriteUInt16(0);
            bw.WriteInt32(m_triangleOffset);
            bw.WriteUInt16(m_indexStriding);
            bw.WriteSByte(m_flipAlternateTriangles);
            bw.WriteUInt32(0);
        }
    }
}