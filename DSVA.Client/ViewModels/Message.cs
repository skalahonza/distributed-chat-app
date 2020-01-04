namespace DSVA.Client.Models
{
    public class Message
    {
        private readonly string _me;
        public string From { get; }
        public string To { get; }
        public string Content { get; }
        public string Clock { get; }
        public bool IsForMe => To == _me;
        public bool IsFromMe => From == _me;
        public bool IsConfirmed { get; }

        public Message(string me, string from, string to, string content, string clock, bool isConfirmed)
        {
            _me = me;
            From = from;
            To = to;
            Content = content;
            Clock = clock;
            IsConfirmed = isConfirmed;
        }
    }
}
