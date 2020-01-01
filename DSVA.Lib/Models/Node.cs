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
        private int? leaderId;

        //Initially each process in the ring is marked as non-participant.
        private bool isParticipant;

        private string _nextAddr;
        private ChatClient _next;
        private ChatClient _nextNext;
        private string _nextNextAddr;
        private readonly ConcurrentDictionary<int, long> _clock;
        private readonly ISet<string> _messages = new HashSet<string>();
        private readonly NodeOptions _options;

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
            (_nextAddr, _nextNextAddr) = (options.Next, options.NextNext);
            if (!string.IsNullOrEmpty(options.Next))
            {
                _next = new ChatClient(GrpcChannel.ForAddress(options.Next));
                if (!string.IsNullOrEmpty(options.NextNext))
                {
                    _nextNext = new ChatClient(GrpcChannel.ForAddress(options.NextNext));
                }
            }
        }

        private bool IsLeader() => (leaderId ?? -1) == _id;

        public void SendConnected()
        {
            if (_next != null)
            {
                _log.LogWarn(_clock, _id, "Sending connected message.");
                PassMessage((x) => x.Connected(new Connect
                {
                    Node = _id,
                    Addr = _options.Address,
                    Header = CreateHeader(),
                    NextAddr = _options.Next,
                    NextId = _options.NextId,
                    NextNextAddr = _options.NextNext,
                    NextNextId = _options.NextNextId
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

        private Header CreateHeader()
        {
            _clock[_id]++;
            var id = Guid.NewGuid().ToString();
            _messages.Add(id);
            return new Header()
            {
                Id = id,
                Leader = leaderId.GetValueOrDefault(-1),
                Clock = { _clock.OrderBy(x => x.Key).Select(x => x.Value) }
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

        //TODO HANDLE DEAED NODES
        private void PassMessage(Action<ChatClient> pass, bool passThrough = true) =>
            PassMessage(pass, _ => { }, passThrough);

        private void PassMessage(Action<ChatClient> pass, Action<RpcException> OnFailure, bool passThrough = true)
        {
            // pass next
            // call on failure if fails
            try
            {
                pass(_next);
            }
            catch (RpcException e)
            {
                _log.LogException(_clock, _id, e, $"Failed sending to {_nextAddr}.");
                OnFailure(e);
                if (passThrough)
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
                leaderId = _id;
                return;
            }

            isParticipant = true;
            var message = new Election
            {
                Node = _id,
                Header = CreateHeader()
            };
            PassMessage(node => node.StartElection(message));
        }

        public void Act(Election message)
        {
            // Every time a process sends or forwards an election message, the process also marks itself as a participant.
            if (ProcessHeader(message.Header, message)) return;

            //If the UID in the election message is smaller, the process unconditionally forwards the election message in a clockwise direction.
            if (message.Node < _id)
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
            if (message.Node > _id)
            {
                //and the process is not yet a participant, the process replaces the UID in the message with its own UID, sends the updated election message in a clockwise direction.
                if (!isParticipant)
                {
                    isParticipant = true;
                    var e = new Election
                    {
                        Node = _id,
                        Header = CreateHeader()
                    };
                    PassMessage(node => node.StartElection(e));
                }
                //If the UID in the election message is larger, and the process is already a participant 
                //(i.e., the process has already sent out an election message with a UID at least as large as its own UID), 
                //the process discards the election message.
            }

            if (message.Node == _id)
            {
                //If the UID in the incoming election message is the same as the UID of the process, that process starts acting as the leader.
                //The leader process marks itself as non-participant and sends an elected message to its neighbour announcing its election and UID.
                isParticipant = false;
                leaderId = _id;
                var e = new Elected
                {
                    Header = CreateHeader(),
                    Node = _id
                };
                PassMessage(node => node.WonElection(e));
            }
        }

        public void Act(Elected message)
        {
            if (ProcessHeader(message.Header, message)) return;
            //When a process receives an elected message, it marks itself as non-participant, records the elected UID, and forwards the elected message unchanged.
            if (message.Node != _id)
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

        public void Act(ChatMessage message)
        {
            // update clock - common message body
            // leader - journal, send everyone, record message
            // follower - send to next - leader died?, 
            try
            {

            }
            catch (RpcException e)
            {

            }
        }

        private bool HandleDisonnected(string addr, string next)
        {
            // Node behind me disconnected
            if (addr == _nextAddr)
            {
                _nextAddr = _nextNextAddr;
                _next = _nextNext;

                _nextNextAddr = next;
                _nextNext = string.IsNullOrEmpty(next) ? null : new ChatClient(GrpcChannel.ForAddress(next));
                return true;
            }
            else return false;
        }

        public void Act(RecordMessage message)
        {

        }

        public void Act(Disconnect message)
        {
            if (ProcessHeader(message.Header, message)) return;
            if (HandleDisonnected(message.Addr, message.NextAddr)) return;
            PassMessage(node => node.SignOut(message));
        }

        public void Act(Dropped message)
        {
            if (ProcessHeader(message.Header, message)) return;
            if (HandleDisonnected(message.Addr, message.NextAddr)) return;
            PassMessage(node => node.Drop(message));
        }

        public void Act(Connect node)
        {
            if (ProcessHeader(node.Header, node) || node.Node == _id) return;
            // Second node added to the circle
            if (_next == null)
            {
                _nextAddr = node.Addr;
                _next = new ChatClient(GrpcChannel.ForAddress(node.Addr));
            }
            // Third node added to the circle
            else if (_nextNext == null && node.NextId == _id)
            {
                // I am in fron of the node: node-->me-->x
                if (node.NextId == _id)
                {
                    _nextNextAddr = node.Addr;
                    _nextNext = _nextNext = new ChatClient(GrpcChannel.ForAddress(_nextNextAddr));
                }
            }
            // I am behind the new node
            else if (node.NextAddr == _nextAddr)
            {
                // treat next as nextnext
                _nextNextAddr = _nextAddr;
                _nextNext = new ChatClient(GrpcChannel.ForAddress(_nextNextAddr));

                // newcomer is my new next
                _nextAddr = node.Addr;
                _next = new ChatClient(GrpcChannel.ForAddress(_nextAddr));
            }
            PassMessage(x => x.Connected(node));
            _log.LogWarn(_clock, _id, $"Next: {_nextAddr}, NextNext: {_nextNextAddr}, Leader: {leaderId}");
        }
    }
}
