using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BinarySerialization;
using ZigbeeNet.CC.SYS;
using ZigbeeNet.CC.ZDO;
using ZigbeeNet.Logging;

namespace ZigbeeNet.CC
{
    public class CCZnp : IHardwareChannel
    {
        private readonly ILog _logger = LogProvider.For<CCZnp>();
        private readonly SemaphoreSlim semaphore;

        private UnifiedNetworkProcessorInterface unpi { get; set; }
        
        
        private Task _readTask;
        private Task _transmitTask;
        private Task _areqTask;

        public TimeSpan ResponseTimeout { get; } = TimeSpan.FromSeconds(5);

        private BlockingCollection<SerialPacket> _areqQueue;
        private BlockingCollection<SerialPacket> _transmitQueue;
        private BlockingCollection<SerialPacket> _responseQueue;

        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler Closed;
        public event EventHandler<NodeEventArgs> NodeEventReceived;

        public int MaxRetryCount => 3;
        public bool Enabled { get; set; }

        public CCZnp(string portName)
        {
            semaphore = new SemaphoreSlim(1, 1);

            unpi = new UnifiedNetworkProcessorInterface(portName, 115200, 1); 
        }

        public void Open()
        {
            _logger.Debug("Opening interface...");
            // TODO: port must be open!!
            unpi.Port.Open();

            // create or reset queues
            _transmitQueue = new BlockingCollection<SerialPacket>();
            _areqQueue = new BlockingCollection<SerialPacket>();
            _responseQueue = new BlockingCollection<SerialPacket>();

            // TODO
            // Create tasks
            _readTask = new Task(() => ReadSerialPort(unpi));
            _transmitTask = new Task(() => ProcessQueue(_transmitQueue, OnSend));
            _areqTask = new Task(() => ProcessQueue(_areqQueue, OnReceive));

            _readTask.Start();
            _transmitTask.Start();
            _areqTask.Start();

            _logger.Debug("Interface opened...");
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            _logger.Debug($"Exception: {e.Error}");
            Error?.Invoke(this, e);
        }

        protected virtual void OnClosed(EventArgs e)
        {
            _logger.Debug($"Connection closed");
            Closed?.Invoke(this, e);
        }
        
        private void OnSend(SerialPacket serialPacket)
        {
            Contract.Requires(serialPacket != null);

            serialPacket.WriteAsync(unpi.OutputStream).ConfigureAwait(false);
            _logger.Debug($"Transmitted: {serialPacket}");
        }

        private void OnReceive(SerialPacket asynchronousRequest)
        {
            _logger.Debug($"Received async message: {asynchronousRequest.GetType().Name}. ({asynchronousRequest})");
        }

        public void Close()
        {
            _logger.Debug("Closing interface...");
            
            _transmitQueue.CompleteAdding();
            _areqQueue.CompleteAdding();
            _responseQueue.CompleteAdding();

            _readTask.Wait();
            _areqTask.Wait();
            _transmitTask.Wait();

            _logger.Debug("Closed interface...");
        }

        private async void ReadSerialPort(UnifiedNetworkProcessorInterface port)
        {
            if (port == null) throw new ArgumentNullException(nameof(port));

            while (true)
            {
                try
                {
                    var serialPacket = await SerialPacket.ReadAsync(port.InputStream).ConfigureAwait(false);
                    
                    if (serialPacket.Type == MessageType.SRSP)
                    {
                        _responseQueue.Add(serialPacket);
                        continue;
                    }

                    if (serialPacket.Type == MessageType.AREQ)
                    {
                        _areqQueue.Add(serialPacket);
                    }
                }
                catch (Exception e)
                {
                    // TODO improve this by adding a good error handling
                    OnError(e);
                }
            }
        }

        protected virtual void OnError(Exception e)
        {
            _logger.Error($"Exception: {e}");
        }

        private void ProcessQueue<T>(BlockingCollection<T> queue, Action<T> action) where T : SerialPacket
        {
            Contract.Requires(queue != null);
            Contract.Requires(action != null);

            while (!queue.IsCompleted)
            {
                var packet = default(SerialPacket);
                try
                {
                    packet = queue.Take();
                }
                catch (InvalidOperationException ie)
                {
                    OnError(ie);
                }

                if (packet != null)
                    action((T) packet);
            }
        }

        private async Task<byte[]> RetryAsync(Func<Task<byte[]>> func, string message, CancellationToken cancellationToken)
        {
            Contract.Requires(func != null);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var attempt = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        return await func().ConfigureAwait(false);
                    }
                    // TODO
                    // Catch more specific exceptions
                    catch (Exception)
                    {
                        if (attempt++ >= MaxRetryCount)
                            throw;

                        _logger.Error($"Some error occured on: {message}. Retrying {attempt} of {MaxRetryCount}.");
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            throw new TaskCanceledException();
        }

        private async Task<SerialPacket> WaitForResponseAsync(Func<SerialPacket, bool> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await Task.Run((Func<SerialPacket>)(() =>
                {
                    var msg = default(SerialPacket);
                    _responseQueue.TryTake(out msg, (int) ResponseTimeout.TotalMilliseconds, cancellationToken);
                    return msg;
                })).ConfigureAwait(false);

                // TODO Sanity checks

                if (predicate(result))
                    return result;
            }

            throw new TaskCanceledException();
        }
        
        private async Task<byte[]> SendAsync(CommandSubsystem commandSubsystem, byte[] payload, Func<SerialPacket, bool> predicate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //return await RetryAsync(async () =>
            //{
            //    var request = new SynchronousRequest(commandSubsystem, 0, payload);
            //    _transmitQueue.Add(request);

            //    var response = await WaitForResponseAsync((msg) => predicate((SynchronousResponse) msg), cancellationToken).ConfigureAwait(false);
                
            //    return ((SynchronousResponse) response).Payload;
            //}, $"{commandSubsystem} {(payload != null ? BitConverter.ToString(payload) : string.Empty)}", cancellationToken);
        }

        public async Task<byte[]> SendAsync(SerialPacket payload, Func<SerialPacket, bool> predicate, CancellationToken cancellationToken)
        {
            return await RetryAsync(async () =>
            {
                _transmitQueue.Add(payload);

                var response = await WaitForResponseAsync((msg) => predicate(msg), cancellationToken).ConfigureAwait(false);

                return ((SerialPacket)response).Payload;
            }, $"{payload.Cmd0} {(payload != null ? BitConverter.ToString(payload.Payload) : string.Empty)}", cancellationToken);
        }
    }
}