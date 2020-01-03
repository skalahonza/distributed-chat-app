using DSVA.Lib.Comparers;
using DSVA.Lib.Extensions;
using DSVA.Service;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DSVA.Service.Chat;

namespace DSVA.Lib.Models
{
    public class Node
    {
        private ILogger _log;
        private readonly int _id;
        private string leaderId = "";

        //Initially each process in the ring is marked as non-participant.
        private bool isParticipant;

        private string NextAddr
        {
            get { return nextAddr; }
            set
            {
                nextAddr = value;
                (_next, nextAddr) = string.IsNullOrEmpty(value) || value == _options.Address ? (null, "") : (new ChatClient(GrpcChannel.ForAddress(value)), value);
            }
        }

        private string NextNextAddr
        {
            get { return nextNextAddr; }
            set
            {
                nextNextAddr = value;
                (_nextNext, nextNextAddr) = string.IsNullOrEmpty(value) || value == _options.Address ? (null, "") : (new ChatClient(GrpcChannel.ForAddress(value)), value);
            }
        }

        private ChatClient _next;
        private ChatClient _nextNext;
        private readonly ConcurrentDictionary<int, long> _clock;
        private readonly ISet<string> _messages = new HashSet<string>();

        private readonly ConcurrentBag<JournalEntry> _journal = new ConcurrentBag<JournalEntry>();
        private readonly NodeOptions _options;
        private string nextAddr;
        private string nextNextAddr;

        public Node(IOptions<NodeOptions> options, ILogger<Node> log)
            : this(options.Value, log)
        {
            log.LogInformation("Initializing node with: {@options}", options.Value);
        }

        private Node(NodeOptions options, ILogger log)
        {
            _options = options;
            _log = log;
            _id = options.Id;
            _clock = new ConcurrentDictionary<int, long>(Enumerable.Range(0, _id + 1).ToDictionary(x => x, _ => (long)0));
            (NextAddr, NextNextAddr) = (options.Next, "");
        }

        private IEnumerable<JournalEntry> ConfirmedOrderedJournal() => _journal
                .Where(x => x.IsConfirmed)
                .OrderBy(x => x.Clock, new VectorClockComparer());

        private bool IsLeader() => leaderId == _options.Address;

        public void SendConnected(string next = null)
        {
            if (_next != null)
            {
                _log.LogWarn(_clock, _id, "Sending connected message.");
                PassMessage((x) => x.Connected(new Connect
                {
                    Node = _id,
                    Addr = _options.Address,
                    Header = CreateHeader(),
                    NextAddr = _options.Next ?? next ?? "",
                    NextNextAddr = "",
                }));
            }
            InitElection();
        }

        private void UpdateClock(IDictionary<int, long> clock)
        {
            foreach (var (k, v) in clock.Select(x => (x.Key, x.Value)))
            {
                if (_clock.GetOrAdd(k, 0) < v)
                    _clock[k] = v;
            }
        }

        private Header CreateHeader() => CreateHeader(Guid.NewGuid().ToString());

        private Header CreateHeader(string id)
        {
            _clock[_id]++;
            _messages.Add(id);
            return new Header()
            {
                Id = id,
                Leader = leaderId,
                Clock = { _clock.ToOrderedValues() }
            };
        }

        /// <summary>
        /// Process message header, return true if duplicate message detected
        /// </summary>
        /// <param name="header">Message header</param>
        /// <returns>true if duplicate message detected</returns>
        private bool ProcessHeader<T>(Header header, T message)
        {
            _log.LogMessage(_clock, _id, message);
            if (!_messages.Add(header.Id))
            {
                _log.LogWarn(_clock, _id, $"Message {typeof(T).Name} with id: {header.Id} is duplicate, skipping.");
                return true;
            }

            UpdateClock(Enumerable.Range(0, header.Clock.Count).Zip(header.Clock).ToDictionary(x => x.First, x => x.Second));
            return false;
        }

        // TODO HANDLE DEAED NODES
        // TODO Update clock  
        // TOOD HADNEL UNDELIVERABLE CHAT MASSAGES
        private void PassMessage(Action<ChatClient> pass, bool passThrough = false) =>
            PassMessage(pass, _ =>
            {
                //Inform about dropped node
                _log.LogError(_clock, _id, $"Node {NextAddr} detected as dropped.");
                var dropped = NextAddr;
                if (string.IsNullOrEmpty(NextNextAddr))
                {
                    _log.LogWarn(_clock, _id, "No fallback node.");
                    NextAddr = "";
                }
                else
                {
                    _nextNext?.Drop(new Dropped
                    {
                        Header = CreateHeader(),
                        Addr = NextAddr,
                        NextAddr = NextNextAddr,
                        NextNextAddr = "",
                    });
                }

                // Leader is dead
                if (dropped == leaderId)
                {
                    _log.LogError(_clock, _id, $"Dropped node {NextAddr} was a leader, initializing election.");
                    InitElection();
                }
            }, passThrough);

        private void PassMessage(Action<ChatClient> pass, Action<RpcException> OnFailure, bool passThrough = false)
        {
            // pass next
            // call on failure if fails
            try
            {
                if (_next != null)
                    pass(_next);
            }
            catch (RpcException e)
            {
                _log.LogException(_clock, _id, e, $"Failed sending to {NextAddr}.");
                OnFailure(e);
                if (passThrough)
                    if (_nextNext != null)
                        pass(_nextNext);
            }
        }

        /// <summary>
        /// A process that notices a lack of leader starts an election. It creates an election message containing its UID. It then sends this message clockwise to its neighbour.
        /// </summary>
        private void InitElection()
        {
            _log.LogMessage(_clock, _id, "Initializing election.");
            if (_next == null)
            {
                _log.LogWarn(_clock, _id, "I am alone, winning election instantly. I am the senate.");
                leaderId = _options.Address;
                return;
            }

            isParticipant = true;
            var message = new Election
            {
                Node = _options.Address,
                Header = CreateHeader()
            };
            PassMessage(node => node.StartElection(message));
        }

        public void Act(Election message)
        {
            // Every time a process sends or forwards an election message, the process also marks itself as a participant.
            if (ProcessHeader(message.Header, message)) return;

            //If the UID in the election message is smaller, the process unconditionally forwards the election message in a clockwise direction.
            if (string.Compare(message.Node, _options.Address) < 0)
            {
                isParticipant = true;
                var e = new Election
                {
                    Node = message.Node,
                    Header = CreateHeader()
                };
                PassMessage(node => node.StartElection(e));
            }

            //If the UID in the election message is larger
            else if (string.Compare(message.Node, _options.Address) > 0)
            {
                //and the process is not yet a participant, the process replaces the UID in the message with its own UID, sends the updated election message in a clockwise direction.
                if (!isParticipant)
                {
                    isParticipant = true;
                    var e = new Election
                    {
                        Node = _options.Address,
                        Header = CreateHeader()
                    };
                    PassMessage(node => node.StartElection(e));
                }
                //If the UID in the election message is larger, and the process is already a participant 
                //(i.e., the process has already sent out an election message with a UID at least as large as its own UID), 
                //the process discards the election message.
            }

            else
            {
                //If the UID in the incoming election message is the same as the UID of the process, that process starts acting as the leader.
                //The leader process marks itself as non-participant and sends an elected message to its neighbour announcing its election and UID.
                isParticipant = false;
                leaderId = _options.Address;
                var e = new Elected
                {
                    Header = CreateHeader(),
                    Node = _options.Address
                };
                PassMessage(node => node.WonElection(e));
            }
        }

        public void Act(Elected message)
        {
            if (ProcessHeader(message.Header, message)) return;
            //When a process receives an elected message, it marks itself as non-participant, records the elected UID, and forwards the elected message unchanged.
            if (message.Node != _options.Address)
            {
                isParticipant = false;
                leaderId = message.Node;
                //When the elected message reaches the newly elected leader, the leader discards that message, and the election is over.
                PassMessage(node => node.WonElection(new Elected
                {
                    Header = CreateHeader(),
                    Node = message.Node
                }));
            }
        }

        public void HeartBeat()
        {
            PassMessage(node => node.HeartBeat(new Beat
            {
                Header = CreateHeader(),
                Node = _id
            }));
        }

        public void Act(Beat message)
        {
            if (ProcessHeader(message.Header, message)) return;
            PassMessage(node => node.HeartBeat(message));
        }

        public void SendMessage(string from, string to, string content)
        {
            Act(new ChatMessage
            {
                Header = CreateHeader(),
                From = from,
                To = to,
                Content = content
            },false);
        }

        public void Act(ChatMessage message, bool validate = true)
        {
            if (validate && ProcessHeader(message.Header, message)) return;
            if (IsLeader() && message.To == _options.Address)
            {
                var entry = new JournalEntry(message.Header.Id, _clock, message.From, message.To, message.Content)
                {
                    IsConfirmed = true
                };
                _journal.Add(entry);
                PassMessage(node => node.ConfirmJournal(new JournalMessageConfirm
                {
                    Header = CreateHeader(),
                    Content = message.Content,
                    Jid = entry.Id,
                    From = message.From,
                    To = message.To,
                    Jclock = { _clock }
                }));
            }
            else if (IsLeader())
            {
                var entry = new JournalEntry(message.Header.Id, _clock, message.From, message.To, message.Content);
                _journal.Add(entry);
                PassMessage(node => node.SendMessage(message));
            }
            // recipient 
            else if (message.To == _options.Address)
            {
                PassMessage(node => node.SendMessage(message));
                PassMessage(node => node.MessageReceived(new ReceivedMessage
                {
                    Header = CreateHeader(),
                    Jid = message.Header.Id
                }));
            }
            // other nodes - forward
            else
            {
                PassMessage(node => node.SendMessage(message));
            }
        }

        public void Act(ReceivedMessage message)
        {
            if (ProcessHeader(message.Header, message)) return;
            if (IsLeader())
            {
                var entry = _journal.FirstOrDefault(x => x.Id == message.Jid);
                if (entry == null)
                    _log.LogError(_clock, _id, $"Journal with id {message.Jid} not found.");
                else
                {
                    _log.LogWarn(_clock, _id, $"Journal with id {message.Jid} confirmed.");
                    entry.IsConfirmed = true;
                    PassMessage(node => node.ConfirmJournal(new JournalMessageConfirm
                    {
                        Header = CreateHeader(),
                        Jid = entry.Id,
                        Content = entry.Content,
                        From = entry.From,
                        To = entry.To,
                        Jclock = { _clock }
                    }));
                }
            }
            else
            {
                message.Header = CreateHeader(message.Header.Id);
                PassMessage(node => node.MessageReceived(message));
            }
        }

        public void Act(JournalMessageConfirm message)
        {
            if (ProcessHeader(message.Header, message)) return;
            _journal.Add(new JournalEntry(message.Jid, _clock, message.From, message.To, message.Content));
            message.Header = CreateHeader(message.Header.Id);
            PassMessage(node => node.ConfirmJournal(message));
        }

        // TODO what if leader disconnected
        public void Act(Disconnect message)
        {
            if (ProcessHeader(message.Header, message)) return;
            // My next node disconnected
            // Me --> dead -->
            if (message.Addr == NextAddr)
            {
                _log.LogWarn(_clock, _id, "My Next node disconnected.");
                NextAddr = NextNextAddr;

                NextNextAddr = message.NextNextAddr != _options.Address ? message.NextNextAddr : "";
                _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
            }
            // My next node disconnected
            // Me --> node --> dead
            else if (message.Addr == NextNextAddr)
            {
                _log.LogWarn(_clock, _id, "My Next Next node disconnected.");
                NextNextAddr = message.NextAddr;
                message.Header = CreateHeader();
                PassMessage(node => node.SignOut(message));
                _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
            }
            else
            {
                message.Header = CreateHeader();
                PassMessage(node => node.SignOut(message));
            }
        }

        public void Act(Dropped message)
        {
            if (ProcessHeader(message.Header, message)) return;
            // My next node disconnected
            // Me --> dead -->
            if (message.Addr == NextAddr)
            {
                _log.LogWarn(_clock, _id, "My Next node dropped.");
                NextAddr = NextNextAddr;

                NextNextAddr = message.NextNextAddr != _options.Address ? message.NextNextAddr : "";
                _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
            }
            // My next next node disconnected
            // Me --> node --> dead
            else if (message.Addr == NextNextAddr)
            {
                _log.LogWarn(_clock, _id, "My Next Next node dropped.");
                NextNextAddr = message.NextAddr;
                message.Header = CreateHeader();
                PassMessage(node => node.Drop(message));
                _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
            }
            // My previous node disconnected
            else if (message.NextAddr == _options.Address)
            {
                _log.LogWarn(_clock, _id, "My previous node disconnected.");
                message.NextNextAddr = NextAddr;
                message.Header = CreateHeader();
                PassMessage(node => node.Drop(message));
                _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
            }
            else
            {
                message.Header = CreateHeader();
                PassMessage(node => node.Drop(message));
            }
        }

        public void Act(Connect node)
        {
            if (ProcessHeader(node.Header, node)) return;

            // I am the new node and other nodes already filled my nextnext
            if (node.Addr == _options.Address)
            {
                // TODO sync messages
                // TODO sync leader?
                NextNextAddr = node.NextNextAddr;
                _log.LogWarn(_clock, _id, $"Connected Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
                return;
            }

            // Second node added to the circle
            if (_next == null)
            {
                NextAddr = node.Addr;
            }
            // me new x
            else if (node.NextAddr == NextAddr)
            {
                // treat next as nextnext
                NextNextAddr = NextAddr;
                // newcomer is my new next
                NextAddr = node.Addr;
            }
            // me x new
            else if (node.NextAddr == NextNextAddr && node.Addr != NextAddr)
            {
                NextNextAddr = node.Addr;
            }
            // new me x
            else if (node.NextAddr == _options.Address)
            {
                // Third node added to the circle
                if (_nextNext == null)
                    NextNextAddr = node.Addr;
                //TODE  Resumption - node dropped, noone detected and recovered                
                node.NextNextAddr = NextAddr;
            }

            node.Header = CreateHeader();
            PassMessage(x => x.Connected(node));
            _log.LogWarn(_clock, _id, $"Next: {NextAddr}, NextNext: {NextNextAddr}, Leader: {leaderId}");
        }
    }
}
