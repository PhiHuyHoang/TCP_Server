using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ViewModelBase;

namespace TCPGameClient
{
    class LoginViewModel: Bindable
    {
        private string _username;
        private string _address;
        private string _port = "8000";
        private string _message;
        private string _colorCode;

        private GameViewModel _chatRoom;

        public string Username
        {
            get { return _username; }
            set { SetField(ref _username, value); }
        }     
        public string Address
        {
            get { return _address; }
            set { SetField(ref _address, value); }
        }      
        public string Port
        {
            get { return _port; }
            set { SetField(ref _port, value); }
        }      
        public string Message
        {
            get { return _message; }
            set { SetField(ref _message, value); }
        }
        public string ColorCode
        {
            get { return _colorCode; }
            set { SetField(ref _colorCode, value); }
        }


        public GameViewModel ChatRoom
        {
            get { return _chatRoom; }
            set { SetField(ref _chatRoom, value); }
        }

        public AsyncCommand ConnectCommand { get; set; }
        public AsyncCommand DisconnectCommand { get; set; }
        public AsyncCommand SendCommand { get; set; }

        public LoginViewModel()
        {
            ChatRoom = new GameViewModel();

            ConnectCommand = new AsyncCommand(Connect, CanConnect);
            DisconnectCommand = new AsyncCommand(Disconnect, CanDisconnect);
            SendCommand = new AsyncCommand(Send, CanSend);
        }

        private async Task Connect()
        {
            ChatRoom = new GameViewModel();
            int socketPort = 0;
            var validPort = int.TryParse(Port, out socketPort);

            if (!validPort)
            {
                DisplayError("Please provide a valid port.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Address))
            {
                DisplayError("Please provide a valid address.");
                return;
            }

            if (String.IsNullOrWhiteSpace(Username))
            {
                DisplayError("Please provide a username.");
                return;
            }

            ChatRoom.Clear();
            await Task.Run(() => ChatRoom.Connect(Username, Address, socketPort));
        }

        private async Task Disconnect()
        {
            if (ChatRoom == null)
                DisplayError("You are not connected to a server.");

            await ChatRoom.Disconnect();
        }

        private async Task Send()
        {
            if (ChatRoom == null)
                DisplayError("You are not connected to a server.");

            await ChatRoom.Send(Username, Message, ColorCode);
            Message = string.Empty;
        }

        private bool CanConnect() => !ChatRoom.IsRunning;
        private bool CanDisconnect() => ChatRoom.IsRunning;
        private bool CanSend() => !String.IsNullOrWhiteSpace(Message) && ChatRoom.IsRunning;

        private void DisplayError(string message) =>
            MessageBox.Show(message, "Woah there!", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

