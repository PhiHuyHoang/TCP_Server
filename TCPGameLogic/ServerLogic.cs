using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TCPElement;
using TCPElement.Packets;

namespace TCPGameLogic
{
    public class ServerLogic
    {
        public string _externalAddress;
        public string _port;
        public string _status;
        public int _clientsConnected;
        private ServerBase _server;
        public bool _isRunning;
        private IList<string> _outputs;
        public Dictionary<string, string> _usernames;
        private Task _updateTask;
        private Task _listenTask;

        public ServerLogic(string externalAddress, string port, string status, int clientsConnected, ServerBase server, bool isRunning, IList<string> outputs, Dictionary<string, string> usernames)
        {
            _externalAddress = externalAddress;
            _port = port;
            _status = status;
            _clientsConnected = clientsConnected;
            _server = server;
            _isRunning = isRunning;
            _outputs = outputs;
            _usernames = usernames;
        }

        public void UpdateScreen<T>(ref T oldvalue, T newvalue )
        {
            oldvalue = newvalue;
        }

        private void WriteOutput(string message)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                _outputs.Add(message);
            });
        }

        public async Task Run(string status)
        {
            UpdateScreen(ref _status, "Connecting...");
            status = "Connecting...";
            // mean set status first but still need to WAIT FOR THE SETUP server
            await SetupServer();
            _server.Open();
            _listenTask = Task.Run(() => _server.Start());
            _updateTask = Task.Run(() => Update());
            _isRunning = true;
        }

        private async Task SetupServer()
        {
            UpdateScreen(ref _status, "Validating socket...");
            _status = "Validating socket...";
            int socketPort = 0;
            var isValidPort = int.TryParse(_port, out socketPort);

            if (!isValidPort)
            {
                DisplayError("Port value is not valid.");
                return;
            }

            _status = "Obtaining IP...";
            await Task.Run(() => GetExternalIp());
            _status = "Setting up server...";
            _server = new ServerBase(IPAddress.Any, socketPort);

            _status = "Setting up events...";

            // Subcribe event to thing
            _server.OnConnectionAccepted += Server_OnConnectionAccepted;
            _server.OnConnectionRemoved += Server_OnConnectionRemoved;
            _server.OnPacketSent += Server_OnPacketSent;
            _server.OnPersonalPacketSent += Server_OnPersonalPacketSent;
            _server.OnPersonalPacketReceived += Server_OnPersonalPacketReceived;
            _server.OnPacketReceived += Server_OnPacketReceived;
        }

        private void Update()
        {
            while (_isRunning)
            {
                // update server every 5 seconds
                Thread.Sleep(5);
                if (!_server.IsRunning)
                {
                    Task.Run(() => Stop());
                    return;
                }

                _clientsConnected = _server.Connections.Count;
                _status = "Running";
            }
        }

        public async Task Stop()
        {
            _externalAddress = string.Empty;
            _isRunning = false;
            _clientsConnected = 0;
            _server.Close();

            await _listenTask;
            await _updateTask;
            _status = "Stopped";
        }


        private void GetExternalIp()
        {
            try
            {
                string externalIP;
                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                _externalAddress = externalIP;
            }
            catch { _externalAddress = "Error receiving IP address."; }
        }

        private void DisplayError(string message)
        {
            MessageBox.Show(message, "Woah there!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Server_OnPacketSent(object sender, PingPacketEvent e)
        {
            WriteOutput("Ping OK!");
        }

        private void Server_OnPacketReceived(object sender, PingPacketEvent e)
        {
            WriteOutput("Connection OK!");
        }

        private void Server_OnPersonalPacketSent(object sender, PersonalPacketEvent e)
        {
            WriteOutput("Personal Packet Sent");
        }

        private void Server_OnConnectionAccepted(object sender, PingPacketEvent e)
        {
            WriteOutput("Client Connected: " + e.Sender.Socket.RemoteEndPoint.ToString());
        }

        private void Server_OnConnectionRemoved(object sender, PingPacketEvent e)
        {
            if (!_usernames.ContainsKey(e.Sender.ClientId.ToString()))
            {
                return;
            }

            var notification = new ChatPacket
            {
                Username = "Server",
                Message = "A user has left the chat",
                UserColor = Colors.Purple.ToString()
            };

            var userPacket = new UserConnectionPacket
            {
                UserGuid = e.Sender.ClientId.ToString(),
                Username = _usernames[e.Sender.ClientId.ToString()],
                IsJoining = false
            };

            if (_usernames.Keys.Contains(userPacket.UserGuid))
                _usernames.Remove(userPacket.UserGuid);

            userPacket.Users = _usernames.Values.ToArray();

            if (_server.Connections.Count != 0)
            {
                Task.Run(() => _server.SendObjectToClients(userPacket)).Wait();
                Task.Run(() => _server.SendObjectToClients(notification)).Wait();
            }
            WriteOutput("Client Disconnected: " + e.Sender.Socket.RemoteEndPoint.ToString());
        }

        private void Server_OnPersonalPacketReceived(object sender, PersonalPacketEvent e)
        {
            if (e.Packet.Package is UserConnectionPacket ucp)
            {
                var notification = new ChatPacket
                {
                    Username = "Server",
                    Message = "A new user has joined the chat",
                    UserColor = Colors.Purple.ToString()
                };

                if (_usernames.Keys.Contains(ucp.UserGuid))
                    _usernames.Remove(ucp.UserGuid);
                else
                    _usernames.Add(ucp.UserGuid, ucp.Username);

                ucp.Users = _usernames.Values.ToArray();

                Task.Run(() => _server.SendObjectToClients(ucp)).Wait();
                Thread.Sleep(500);
                Task.Run(() => _server.SendObjectToClients(notification)).Wait();
            }
            WriteOutput("Personal Packet Received");
        }
    }
}
