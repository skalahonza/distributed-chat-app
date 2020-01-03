using DSVA.Client.Models;
using DSVA.Lib.Extensions;
using DSVA.Service;
using fm.Mvvm;
using Grpc.Core;
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
        private ChatClient _client => string.IsNullOrEmpty(Service) ? null : new ChatClient(GrpcChannel.ForAddress(Service));
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        public string Service { get; set; } = "https://localhost:5001";
        public string Message { get; set; }
        public string To { get; set; }

        public RelayCommand SendMessageCommand => new RelayCommand(_ =>
       {
           try
           {
               var response = _client.SendMessageClient(new ChatMessageClient
               {
                   Content = Message,
                   From = Service,
                   To = To
               });               
               Message = "";
           }
           catch (System.Exception ex)
           {
               MessageBox.Show(ex.Message);
           }           
       });

        /// <summary>
        /// Request node connection
        /// </summary>
        public RelayCommand ConnectCommand => new RelayCommand(_ =>
        {
            
        });

        /// <summary>
        /// Gracefully signout
        /// </summary>
        public RelayCommand SignOutCommnad => new RelayCommand(_ =>
        {
            
        });

        public RelayCommand HeartBeatCommnad => new RelayCommand(_ =>
        {
            _client?.HeartBeatRequest(new Empty());
        });

        public RelayCommand GetJournal => new RelayCommand(async _ =>
        {
            if (_client == null) return;
            Messages.Clear();
            var replies = _client.GetJournal(new Empty());
            foreach (var reply in replies.Data)
                Messages.Add(new Message(Service, reply.From, reply.To, reply.Content,string.Join(',',reply.Jclock.ToOrderedValues())));
        });

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
