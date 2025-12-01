using System;
using System.IO;

namespace sidplay
{
    /// <summary>
    /// CIA 1 specifics: Generates IRQs
    /// </summary>
    public class C64cia1 : MOS6526
    {
        private Player m_player;

        private short lp;

        public override void interrupt(bool state)
        {
            m_player.interruptIRQ(state);
        }

        public override void portA()
        {
        }

        public override void portB()
        {
            short lp = (short)((regs[PRB] | (short)(~regs[DDRB] & 0xff)) & 0x10);
            if (lp != this.lp)
            {
                m_player.lightpen();
            }
            this.lp = lp;
        }

        public C64cia1(Player player)
            : base(player.m_scheduler)
        {
            m_player = (player);
        }
        // only used for deserializing
        public C64cia1(Player player, BinaryReader reader, EventList events)
            : base(player.m_scheduler, reader, events)
        {
            m_player = player;
        }

        public override void reset()
        {
            lp = 0x10;
            base.reset();
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);
            writer.Write(lp);
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);
            lp = reader.ReadInt16();
        }
    }
}