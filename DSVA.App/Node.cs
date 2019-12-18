using Microsoft.Extensions.Logging;

namespace DSVA.App
{
    public class Node
    {
        //TODO leader, next node, next next node

        private readonly ILogger _log;
        public Node(ILogger<Node> log) => _log = log;

        public void Act() => _log.LogInformation("Message");
    }
}
