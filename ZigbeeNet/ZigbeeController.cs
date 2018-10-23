using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ZigbeeNet.CC;
using ZigbeeNet.ZCL;
using ZigbeeNet.CC.ZDO;
using ZigbeeNet.CC.SAPI;
using System.Threading;

namespace ZigbeeNet
{
    public class ErrorEventArgs : EventArgs
    {
        public readonly Exception Error;

        public ErrorEventArgs(Exception error)
        {
            Error = error;
        }
    }

    public class ZigbeeController
    {
        public IHardwareChannel Channel;
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler ChannelClosed;

        private ZigbeeController(IHardwareChannel channel)
        {
            this.Channel = channel;
        }

        public ZigbeeController(string portName)
        :this(new CCZnp(portName))
        {
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this,e);
        }

        protected virtual void OnChannelClose(EventArgs e)
        {
            ChannelClosed?.Invoke(this, e);
        }

        public void Open()
        {
            Channel.NodeEventReceived += Channel_NodeEventReceived;
            Channel.Error += ChannelOnError;
            Channel.Closed += Channel_Closed;
            Channel.Open();
        }

        public void Close()
        {
            Channel.NodeEventReceived -= Channel_NodeEventReceived;
            Channel.Error -= ChannelOnError;
            Channel.Closed -= Channel_Closed;
            Channel.Close();
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ChannelOnError(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Channel_NodeEventReceived(object sender, NodeEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
