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

        public Message(string me, string from, string to, string content, string clock)
        {
            _me = me;
            From = from;
            To = to;
            Content = content;
            Clock = clock;
        }
    }
}
