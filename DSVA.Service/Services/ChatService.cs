using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace DSVA.Service
{
    public class ChatService : Chat.ChatBase
    {
        private Guid guid = Guid.NewGuid();

        public override Task<Status> SendMessage(ChatMessage request, ServerCallContext context)
        {           
            return Task.FromResult(new Status
            {
                Ok = true
            });
        }
    }
}
