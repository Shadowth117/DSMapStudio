using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public enum ShaderType
    {
        EFFECT_TYPE_INVALID = 0,
        EFFECT_TYPE_UNKNOWN = 1,
        EFFECT_TYPE_HLSL_INLINE = 2,
        EFFECT_TYPE_CG_INLINE = 3,
        EFFECT_TYPE_HLSL_FILENAME = 4,
        EFFECT_TYPE_CG_FILENAME = 5,
        EFFECT_TYPE_MAX_ID = 6,
    }
    
    public class hkxMaterialShader : hkReferencedObject
    {
        public string m_name;
        public ShaderType m_type;
        public string m_vertexEntryName;
        public string m_geomEntryName;
        public string m_pixelEntryName;
        public List<byte> m_data;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_name = des.ReadStringPointer(br);
            m_type = (ShaderType)br.ReadByte();
            br.AssertUInt32(0);
            br.AssertUInt16(0);
            br.AssertByte(0);
            m_vertexEntryName = des.ReadStringPointer(br);
            m_geomEntryName = des.ReadStringPointer(br);
            m_pixelEntryName = des.ReadStringPointer(br);
            m_data = des.ReadByteArray(br);
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