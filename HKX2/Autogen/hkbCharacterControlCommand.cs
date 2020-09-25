using SoulsFormats;
using System.Collections.Generic;
using System.Numerics;

namespace HKX2
{
    public enum CharacterControlCommand
    {
        COMMAND_HIDE = 0,
        COMMAND_SHOW = 1,
    }
    
    public class hkbCharacterControlCommand : hkReferencedObject
    {
        public ulong m_characterId;
        public CharacterControlCommand m_command;
        public int m_padding;
        
        public override void Read(PackFileDeserializer des, BinaryReaderEx br)
        {
            base.Read(des, br);
            m_characterId = br.ReadUInt64();
            m_command = (CharacterControlCommand)br.ReadByte();
            br.AssertUInt16(0);
            br.AssertByte(0);
            m_padding = br.ReadInt32();
        }
        
        public override void Write(BinaryWriterEx bw)
        {
            base.Write(bw);
            bw.WriteUInt64(m_characterId);
            bw.WriteUInt16(0);
            bw.WriteByte(0);
            bw.WriteInt32(m_padding);
        }
    }
}