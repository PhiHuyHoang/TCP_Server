using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPElement.Packets
{
    /// <summary>
    /// Packet class to test connection
    /// </summary>
    [Serializable]
    public class PingPacket
    {
        public string GuidId { get; set; }
    }
}
