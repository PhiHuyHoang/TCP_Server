using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPElement.Packets
{
    /// <summary>
    /// real packet
    /// </summary>
    [Serializable]
    public class PersonalPacket
    {
        public string GuidId { get; set; }
        public object Package { get; set; }
    }
}
