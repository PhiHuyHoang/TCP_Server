using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TCPElement.Packets;

namespace TCPElement
{
    public delegate void PingPacketEventHandler(object sender, PingPacketEvent e);
    public delegate void PersonalPacketEventHandler(object sender, PersonalPacketEvent e);

    public class ServerBase
    {
        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public bool IsRunning { get; private set; }

        public IPEndPoint EndPoint { get; private set; }
        public Socket Socket { get; private set; }
        public List<ClientBase> Connections { get; private set; }

        private Task _receivingTask;

        public event PingPacketEventHandler OnConnectionAccepted;
        public event PingPacketEventHandler OnConnectionRemoved;
        public event PingPacketEventHandler OnPacketReceived;
        public event PingPacketEventHandler OnPacketSent;

        public event PersonalPacketEventHandler OnPersonalPacketSent;
        public event PersonalPacketEventHandler OnPersonalPacketReceived;

        public ServerBase(IPAddress address, int port)
        {
            Address = address;
            Port = port;

            EndPoint = new IPEndPoint(address, port);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.ReceiveTimeout = 5000;
            Connections = new List<ClientBase>();
        }

        public bool Open()
        {
            Socket.Bind(EndPoint);
            Socket.Listen(10);
            return true;
        }

        public bool Close()
        {
            IsRunning = false;
            Connections.Clear();
            return true;
        }

        private object ReadObject(Socket clientSocket)
        {
            byte[] data = new byte[clientSocket.ReceiveBufferSize];

            using (Stream s = new NetworkStream(clientSocket))
            {
                s.Read(data, 0, data.Length);
                var memory = new MemoryStream(data);
                memory.Position = 0;

                var formatter = new BinaryFormatter();
                var obj = formatter.Deserialize(memory);

                return obj;
            }
        }

        public void SendObjectToClients(object package)
        {
            foreach (var c in Connections.ToList())
            {
                c.SendObject(package).Wait();
                var testPing = new PingPacketEvent(c, c, package);
                OnPacketSent?.Invoke(this, testPing);
            }
        }

        private void MonitorStreams()
        {
            while (IsRunning)
            {
                foreach (var client in Connections.ToList())
                {
                    if (!client.IsSocketConnected())
                    {
                        var removeClientEvent = new PingPacketEvent(client, null, string.Empty);
                        Connections.Remove(client);
                        OnConnectionRemoved?.Invoke(this, removeClientEvent);
                        continue;
                    }

                    if (client.Socket.Available != 0)
                    {
                        var readObject = ReadObject(client.Socket);
                        var testConnection = new PingPacketEvent(client, null, readObject);
                        OnPacketReceived?.Invoke(this, testConnection);

                        if (readObject is PingPacket ping)
                        {
                            client.SendObject(ping).Wait();
                            continue;
                        }

                        if (readObject is PersonalPacket pp)
                        {
                            var destination = Connections.FirstOrDefault(c => c.ClientId.ToString() == pp.GuidId);
                            var e4 = new PersonalPacketEvent(client, destination, pp);
                            OnPersonalPacketReceived?.Invoke(this, e4);

                            if (destination != null)
                            {
                                destination.SendObject(pp).Wait();
                                var personPaketEventSuccess = new PersonalPacketEvent(client, destination, pp);
                                OnPersonalPacketSent?.Invoke(this, personPaketEventSuccess);
                            }
                        }
                        else
                        {
                            foreach (var c in Connections.ToList())
                            {
                                c.SendObject(readObject).Wait();
                                var waitPacketEvent = new PingPacketEvent(client, c, readObject);
                                OnPacketSent?.Invoke(this, waitPacketEvent);
                            }
                        }
                    }
                }
            }
        }

        public async Task<bool> Listen()
        {
            while (IsRunning)
            {
                if (Socket.Poll(100000, SelectMode.SelectRead))
                {
                    var newConnection = Socket.Accept();
                    if (newConnection != null)
                    {
                        var client = new ClientBase();
                        var newGuid = await client.CreateGuid(newConnection);
                        await client.SendMessage(newGuid);
                        Connections.Add(client);
                        var emptyPingClientSuccess = new PingPacketEvent(client, null, String.Empty);
                        OnConnectionAccepted?.Invoke(this, emptyPingClientSuccess);
                    }
                }
            }
            return true;
        }

        public async Task<bool> Start()
        {
            _receivingTask = Task.Run(() => MonitorStreams());
            IsRunning = true;
            await Listen();
            await _receivingTask;
            Socket.Close();
            return true;
        }
    }
}
