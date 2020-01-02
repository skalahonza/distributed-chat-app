using System;
using System.Collections.Generic;

namespace DSVA.Lib.Models
{
    public class JournalEntry
    {
        public IDictionary<int, long> Clock { get; }
        public string From { get; }
        public string To { get; }
        public string Content { get; }
        public bool IsConfirmed { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public JournalEntry(string jid, IDictionary<int, long> clock, string from, string to, string content)
        {
            Id = jid;
            Clock = new Dictionary<int, long>(clock);
            From = from;
            To = to;
            Content = content;
        }
    }
}
