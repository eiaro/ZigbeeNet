using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZigbeeNet.CC
{
    /// <summary>
    /// Interface for a hardware specific Channel. This should reside in the ZCL part of the library
    /// and all hardware implementations should implement this interface
    /// TODO
    /// Add events for received events
    /// Some "short" commands like get nodes or stuff... You know, to keep life simple.
    /// </summary>
    public interface IHardwareChannel
    {
        void Open();
        void Close();
        event EventHandler<NodeEventArgs> NodeEventReceived;
        event EventHandler<ErrorEventArgs> Error;
        event EventHandler Closed;

        Task<byte[]> SendAsync(SerialPacket payload, Func<SerialPacket, bool> predicate,
            CancellationToken cancellationToken);
    }
}