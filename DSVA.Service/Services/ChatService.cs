using System.Threading.Tasks;
using DSVA.Lib.Models;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DSVA.Service
{
    public class ChatService : Chat.ChatBase
    {
        private readonly ILogger log;
        private readonly Node _node;
        public ChatService(Node node, ILogger<ChatService> log)
        {
            _node = node;
            this.log = log;
        }

        public override Task<Status> SendMessage(ChatMessage request, ServerCallContext context)
        {
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> StartElection(Election request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> WonElection(Elected request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> Connected(Connect request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }
    }
}
