using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TCPGameLogic;
using ViewModelBase;

namespace TCPGameServer
{
    class ServerViewModel: Bindable
    {
        private ServerLogic serverLogic;

        private string _externalAddress;
        private string _port = "8000";
        private string _status;
        private int _clientsConnected;
        private ServerBase _server;
        private bool _isRunning;

        public string ExternalAddress
        {
            get { return _externalAddress; }
            set { SetField(ref _externalAddress, value); }
        }
        public string Port
        {
            get { return _port; }
            set { SetField(ref _port, value); }
        }
        public string Status
        {
            get { return _status; }
            set { SetField(ref _status, value); }
        }
        public int ClientsConnected
        {
            get { return _clientsConnected; }
            set { SetField(ref _clientsConnected, value); }
        }

        public ObservableCollection<string> Outputs { get; set; }
        public Dictionary<string, string> Usernames = new Dictionary<string, string>();

        public RelayCommand RunCommand { get; set; }
        public AsyncCommand StopCommand { get; set; }

        public ServerViewModel()
        {
            Outputs = new ObservableCollection<string>();
            serverLogic = new ServerLogic(ExternalAddress, Port, "Rice", ClientsConnected, _server, _isRunning, Outputs, Usernames);
            RunCommand = new RelayCommand((obj) => { serverLogic.Run(_status); });
            StopCommand = new AsyncCommand(serverLogic.Stop);
        }
    }
}
