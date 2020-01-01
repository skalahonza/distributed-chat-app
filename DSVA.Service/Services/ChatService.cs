using System.Threading.Tasks;
using DSVA.Lib.Models;
using Grpc.Core;

namespace DSVA.Service
{
    public class ChatService : Chat.ChatBase
    {
        private readonly Node _node;
        public ChatService(Node node) => _node = node;

        public override Task<Status> HeartBeatRequest(Empty request, ServerCallContext context)
        {
            _node.HeartBeat();
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> HeartBeat(Beat request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> SendMessageClient(ChatMessageClient request, ServerCallContext context)
        {
            _node.SendMessage(request.From,request.To,request.To);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> SendMessage(ChatMessage request, ServerCallContext context)
        {
            _node.Act(request);
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

        public override Task<Status> Drop(Dropped request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }

        public override Task<Status> SignOut(Disconnect request, ServerCallContext context)
        {
            _node.Act(request);
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }
    }
}
