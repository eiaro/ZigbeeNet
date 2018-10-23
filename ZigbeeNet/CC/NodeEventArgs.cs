using System;

namespace ZigbeeNet.CC
{
    public class NodeEventArgs: EventArgs
    {
        private readonly int nodeId;

        public NodeEventArgs(int nodeId)
        {
            this.nodeId = nodeId;
        }
    }
}