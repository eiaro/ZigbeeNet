using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

// TODO
// Files regaring UNPI is just cramped in here. Should not be tied too strict to
// the project as UNPI is intended to be used on other systems as well.
//
// SerialPacket could be abstracted to a general serialpacket and overloaded here to add the stuff we need
// to handle our MT CMD and queue stuff.... 

namespace ZigbeeNet.CC
{
    static partial class StreamExtensions
    {
        public static async Task ReadAsyncExact(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Buffer length should not be 0");

            var read = 0;
            while (read < count)
            {
                read += await stream.ReadAsync(buffer, offset + read, count - read);
            }
        }
    }



    public class UnifiedNetworkProcessorInterface 
    {
        public SerialPort Port { get; set; }
        public int LenBytes;

        public Stream InputStream
        {
            get { return Port.BaseStream; }
        }

        public Stream OutputStream
        {
            get { return Port.BaseStream; }
        }

        /// <summary>
        /// Create a new instance of the unpi class.
        /// </summary>
        /// <param name="lenBytes">1 or 2 to indicate the width of length field. Default is 2.</param>
        /// <param name="stream">The transceiver instance, i.e. serial port, spi. It should be a duplex stream.</param>
        public UnifiedNetworkProcessorInterface(string port, int baudrate = 115200, int lenBytes = 2)
        {
            Port = new SerialPort(port, baudrate);

            LenBytes = lenBytes;
        }

        public void Open()
        {
            Port.Open();

            Port.DiscardInBuffer();
            Port.DiscardOutBuffer();
        }

        public void Close()
        {
            Port.Close();
        }


        /// <summary>
        /// Sending isn't really implmented, but the correct method is to put it on the transmit queue on CCZnp
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public byte[] Send(int areq, int zpiObjectSubSystem, byte zpiObjectCommandId, byte[] zpiObjectFrame)
        {
            throw new NotImplementedException();
        }
    }
}
