using System;

namespace ZigbeeNet.CC
{
    /// <summary>
    /// This callback is sent by the device to indicate that a reset has occurred.
    /// </summary>
    public class SYS_RESET_RESPONSE : SerialPacket
    {
        

        public SYS_RESET_RESPONSE(byte[] payload) : base(CommandType.SYS_RESET_RESPONSE, payload)
        {
            Reason =  payload[0];
            TransportRev = payload[1];
            ProductId = payload[2];
            MajorRel = payload[3];
            MinorRel = payload[4];
            HwRev = payload[5];
        }

        /// <summary>
        /// Gets or sets the hardware revision number
        /// </summary>
        public byte HwRev { get; protected set; }

        /// <summary>
        /// Gets or sets the minor release number.
        /// </summary>
        public byte MinorRel { get; protected set; }

        /// <summary>
        /// Gets or sets the major release number.
        /// </summary>
        public byte MajorRel { get; protected set; }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public byte ProductId { get; protected set; }

        /// <summary>
        /// Gets or sets the transport protocol revision.
        /// </summary>
        public byte TransportRev { get; protected set; }

        /// <summary>
        /// Gets or sets the reason for the reset.
        /// 0x00 Power-up
        /// 0x01 External
        /// 0x02 Watch-dog
        /// </summary>
        public byte Reason { get; protected set; }

        public override string ToString()
        {
            return
                $"Reason: {Reason}, TransportRev: {TransportRev}, ProductId: {ProductId}, Major Rel: {MajorRel}, Minor Rel: {MinorRel}, HwRev: {HwRev}";
        }
    }
}