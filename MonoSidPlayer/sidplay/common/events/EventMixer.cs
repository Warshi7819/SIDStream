using System;
using System.IO;

namespace sidplay
{
    public class EventMixer : Event
    {
        private Player m_player;

        public override void _event()
        {
            m_player.mixer();
        }

        public EventMixer(Player player)
            : base("Mixer")
        {
            m_player = player;
        }
        // only used for deserializing
        public EventMixer(Player player, EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
            m_player = player;
        }

        internal override EventType GetEventType()
        {
            return EventType.mixerEvt;
        }
    }
}