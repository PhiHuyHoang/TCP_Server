using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPElement.Packets
{
    public class PersonalPacketEvent : EventArgs
    {
        public ClientBase Sender { get; private set; }
        public ClientBase Receiver { get; private set; }
        public PersonalPacket Packet { get; private set; }

        public PersonalPacketEvent(ClientBase sender, ClientBase receiver, PersonalPacket packet)
        {
            Sender = sender;
            Receiver = receiver;
            Packet = packet;
        }
    }
}
