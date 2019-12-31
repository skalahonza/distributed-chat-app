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
        private int _neighbours;
        private ChatClient _next;
        private ChatClient _nextNext;
        private string _nextNextAddr;
        private readonly ConcurrentDictionary<int, long> _clock;
        private readonly ISet<string> _messages = new HashSet<string>();

        public Node(IOptions<NodeOptions> options, ILogger<Node> log)
            : this(options.Value.Next, options.Value.NextNext, options.Value.NeighboursCount, options.Value.Id) => _log = log;

        private Node(string next, string nextNext, int neighbours, int id)
        {
            _id = id;
            _neighbours = neighbours;
            _clock = new ConcurrentDictionary<int, long>(Enumerable.Range(0, neighbours).ToDictionary(x => x, _ => (long)0));
            (_nextAddr, _nextNextAddr) = (next, nextNext);
            _next = new ChatClient(GrpcChannel.ForAddress(next));
            _nextNext = new ChatClient(GrpcChannel.ForAddress(nextNext));
        }

        private bool IsLeader() => (leaderId ?? -1) == _id;

        public void UpdateClock(IDictionary<int, long> clock)
        {
            foreach (var (k, v) in clock.Select(x => (x.Key, x.Value)))
            {
                if (_clock[k] < v)
                    _clock[k] = v;
            }
        }

        private Header CreateHeader()
        {
            _clock[_id]++;
            return new Header()
            {
                Id = Guid.NewGuid().ToString(),
                Leader = -1,
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
            _log.LogMessage(_clock, $"Message {typeof(T).Name} with id: {header.Id} received.");
            if (!_messages.Add(header.Id))
            {
                _log.LogWarn(_clock, $"Message {typeof(T).Name} with id: {header.Id} is duplicate, skipping.");
                return true;
            }

            UpdateClock(Enumerable.Range(0, _neighbours).Zip(header.Clock).ToDictionary(x => x.First, x => x.Second));
            return false;
        }

        //TODO HANDLE DEAED NODES
        private void PassMessage(Action<ChatClient> pass, bool passThrough = true) =>
            PassMessage(pass, _ => { }, passThrough);

        private void PassMessage(Action<ChatClient> pass, Action<RpcException> OnFailure, bool passThrough = true)
        {
            //If I am alone
            if (_next == null)
            {
                //and not a leader
                if (!IsLeader())
                    InitElection();
                return;
            }
            // pass next
            // call on failure if fails
            try
            {
                pass(_next);
            }
            catch (RpcException e)
            {
                _log.LogException(_clock, e, $"Failed sending to {_nextAddr}.");
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
            if (_neighbours == 0)
            {
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
            ProcessHeader(message.Header, message);

            //If the UID in the election message is larger, the process unconditionally forwards the election message in a clockwise direction.
            if (message.Node > _id)
            {
                isParticipant = true;
                var e = new Election
                {
                    Node = message.Node,
                    Header = CreateHeader()
                };
                PassMessage(node => node.StartElection(e));
            }

            //If the UID in the election message is smaller
            if (message.Node < _id)
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
                //If the UID in the election message is smaller, and the process is already a participant (i.e., the process has already sent out an election message with a UID at least as large as its own UID), the process discards the election message.
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
            ProcessHeader(message.Header, message);
            //When a process receives an elected message, it marks itself as non-participant, records the elected UID, and forwards the elected message unchanged.
            if (message.Node != _id)
            {
                isParticipant = false;
                leaderId = message.Node;
            }
            //When the elected message reaches the newly elected leader, the leader discards that message, and the election is over.
            PassMessage(node => node.WonElection(new Elected
            {
                Header = CreateHeader(),
                Node = message.Node
            }));
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

        public void Act(RecordMessage message)
        {

        }

        public void Act(Disconnect message)
        {

        }

        public void Act(Dropped message)
        {

        }

        public void Act(Connect connect)
        {

        }
    }
}
