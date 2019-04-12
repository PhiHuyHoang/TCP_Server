using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPElement.Packets
{
    public class PingPacketEvent : EventArgs
    {
        public ClientBase Sender { get; private set; }
        public ClientBase Receiver { get; private set; }
        public object Packet { get; private set; }

        public PingPacketEvent(ClientBase sender, ClientBase receiver, object packet)
        {
            Sender = sender;
            Receiver = receiver;
            Packet = packet;
        }
    }
}
