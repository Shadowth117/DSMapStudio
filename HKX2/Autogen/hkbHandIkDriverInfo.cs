using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public class hkbHandIkDriverInfo : hkReferencedObject
    {
        public List<hkbHandIkDriverInfoHand> m_hands;
        public BlendCurve m_fadeInOutCurve;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_hands = des.ReadClassArray<hkbHandIkDriverInfoHand>(br);
            m_fadeInOutCurve = (BlendCurve)br.ReadSByte();
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