using DSVA.Client.Models;
using DSVA.Service;
using fm.Mvvm;
using Grpc.Net.Client;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using static DSVA.Service.Chat;

namespace DSVA.Client.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel : INotifyPropertyChanged
    {
        private ChatClient _client;
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        public string Service { get; set; }
        public string Message { get; set; }

        public RelayCommand SendMessageCommand => new RelayCommand(_ =>
       {
           try
           {
               var response = _client.SendMessage(new ChatMessage
               {
                   Content = Message,
               });
           }
           catch (System.Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           Messages.Add(new Message { Content = Message, IsFromMe = true });
           Message = "";
       });

        public RelayCommand ConnectCommand => new RelayCommand(_ =>
        {
            var channel = GrpcChannel.ForAddress(Service);
            _client = new ChatClient(channel);
        });

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
