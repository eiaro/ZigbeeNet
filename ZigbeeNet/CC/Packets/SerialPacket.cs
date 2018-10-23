using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ZigbeeNet.CC
{
    public class SerialPacket
    {

        public static byte SOF = 0xfe;

        public SerialPacket()
        {

        }

        public byte Length => (byte)Payload.Length;

        //public SerialPacket(MessageType type, CommandSubsystem commandSubsystem, byte commandId, byte[] payload = null)
        //{
        //    //Type = type;
        //    //CommandSubsystem = commandSubsystem;
        //    Cmd1 = commandId;
        //    Payload = payload != null ? payload : new byte[0];
        //}

        protected SerialPacket(CommandType cmdType, byte[] payload)
        {
            var val = (ushort) cmdType;
            var b1 = (byte)(val >> 8 );
            var b2 = (byte) (val & 0xff);

            Cmd0 = b1;
            Cmd1 = b2;

            Payload = payload;
        }

        public async Task<byte[]> ToFrame()
        {
            using(MemoryStream stream = new MemoryStream())
            {
                await WriteAsync(stream).ContinueWith( (task) =>
                {
                    return stream.ToArray();
                }).ConfigureAwait(false);
            }

            return null;
        }

        public async Task WriteAsync(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var buffer = new List<byte>();
            buffer.Add(SOF);
            buffer.Add((byte)Payload.Length);
            buffer.Add(Cmd0);
            buffer.Add(Cmd1);
            buffer.AddRange(Payload);
            buffer.Add(buffer.Skip(1).Aggregate((byte)0x00, (total, next) => total ^= next));

            await stream.WriteAsync(buffer.ToArray(), 0, buffer.Count);
        }

        public static async Task<SerialPacket> ReadAsync(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var buffer = new byte[1024];
            await stream.ReadAsyncExact(buffer, 0, 1);

            if (buffer[0] == SOF)
            {
                await stream.ReadAsyncExact(buffer, 1, 1);
                var length = buffer[1];
                await stream.ReadAsyncExact(buffer, 2, length + 3);


                //var type = (MessageType)(buffer[2] >> 5 & 0x07);
                //var subsystem = (CommandSubsystem)(buffer[2] & 0x1f);
                var cmd0 = buffer[2];
                var cmd1 = buffer[3];
                var payload = buffer.Skip(4).Take(length).ToArray();
                var crc = buffer[4 + length];

                if (buffer.Skip(1).Take(length + 3).Aggregate((byte)0x00, (total, next) => (byte)(total ^ next)) != buffer[length + 4])
                    throw new InvalidDataException("checksum error");

                var cmdType = (CommandType)(buffer[2] << 8) + buffer[3];
                switch (cmdType)
                {
                    case CommandType.SYS_RESET_RESPONSE:
                        return new SYS_RESET_RESPONSE(payload);
                }

                throw new InvalidDataException($"unknown message type: {buffer}");
            }

            throw new InvalidDataException("unable to decode packet");
        }

        public MessageType Type => (MessageType)((Cmd0 & 0x60) >> 5);

        public CommandSubsystem CommandSubsystem => (CommandSubsystem) (Cmd0 & 0x1f);

        /// <summary>
        /// CMD0 is a 1 byte field that contains both message type and subsystem information 
        /// Bits[8-6]: Message type, see the message type section for more info.
        /// Bits[5-1]: Subsystem ID field, used to help NPI route the message to the appropriate place.
        /// 
        /// Source: http://processors.wiki.ti.com/index.php/NPI_Type_SubSystem
        /// </summary>
        public byte Cmd0 { get; set; }

        /// <summary>
        /// CMD1 is a 1 byte field that contains the opcode of the command being sent
        /// </summary>

        public byte Cmd1 { get; set; }

        /// <summary>
        /// Payload is a variable length field that contains the parameters defined by the 
        /// command that is selected by the CMD1 field. The length of the payload is defined by the length field.
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Frame Check Sequence (FCS) is calculated by doing a XOR on each bytes of the frame in the order they are 
        /// send/receive on the bus (the SOF byte is always excluded from the FCS calculation): 
        ///     FCS = LEN_LSB XOR LEN_MSB XOR D1 XOR D2...XOR Dlen
        /// </summary>
        public byte FrameCheckSequence { get; set; }


        /// <summary>
        /// Do not use! Should be handled internally
        /// </summary>
        /// <value>The checksum.</value>
        public byte Checksum { get; set; }
    }
}