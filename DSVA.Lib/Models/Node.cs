using DSVA.Service;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DSVA.Service.Chat;

namespace DSVA.Lib.Models
{
    public class Node
    {
        //RpcException
        private readonly int _id;
        private string _nextAddr;
        private ChatClient _next;
        private ChatClient _nextNext;
        private string _nextNextAddr;
        private readonly ConcurrentDictionary<int, long> _clock;

        public Node(IOptions<NodeOptions> options)
            :this(options.Value.Next,options.Value.NextNext,options.Value.NeighboursCount, options.Value.Id)
        {
        }

        private Node(string next, string nextNext, int neighbours, int id)
        {
            _id = id;
            _clock = new ConcurrentDictionary<int, long>(Enumerable.Range(0, neighbours).ToDictionary(x => x, _ => (long)0));
            (_nextAddr, _nextNextAddr) = (next, nextNext);
            _next = new ChatClient(GrpcChannel.ForAddress(next));
            _nextNext = new ChatClient(GrpcChannel.ForAddress(nextNext));
        }

        public void UpdateClock(IDictionary<int, long> clock)
        {
            foreach (var (k, v) in clock.Select(x => (x.Key, x.Value)))
            {
                if (_clock[k] < v)
                    _clock[k] = v;
            }
        }

        public void Act(ChatMessage message)
        {
            // update clock - common message body
            // leader - journal, send everyone, record message
        }
    }
}
