using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public class hkaiEdgeGeometry : hkReferencedObject
    {
        public enum FaceFlagBits
        {
            FLAGS_NONE = 0,
            FLAGS_INPUT_GEOMETRY = 1,
            FLAGS_PAINTER = 2,
            FLAGS_CARVER = 4,
            FLAGS_EXTRUDED = 8,
            FLAGS_ALL = 15,
        }
        
        public List<hkaiEdgeGeometryEdge> m_edges;
        public List<hkaiEdgeGeometryFace> m_faces;
        public List<Vector4> m_vertices;
        public hkaiEdgeGeometryFace m_zeroFace;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_edges = des.ReadClassArray<hkaiEdgeGeometryEdge>(br);
            m_faces = des.ReadClassArray<hkaiEdgeGeometryFace>(br);
            m_vertices = des.ReadVector4Array(br);
            m_zeroFace = new hkaiEdgeGeometryFace();
            m_zeroFace.Read(des, br);
            br.AssertUInt32(0);
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            m_zeroFace.Write(bw);
            bw.WriteUInt32(0);
        }
    }
}