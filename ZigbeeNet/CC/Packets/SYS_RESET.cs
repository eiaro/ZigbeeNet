namespace ZigbeeNet.CC
{
    /// <summary>
    /// This command is sent by the tester to reset the target device
    /// </summary>
    public class SYS_RESET : SerialPacket
    {
        public SYS_RESET(byte resetType) : base(CommandType.SYS_RESET, new byte[] { resetType })
        {
            ResetType = resetType;
        }

        /// <summary>
        /// Gets or sets the type of the reset.
        /// This command will reset the device by using a hardware reset (i.e.
        /// watchdog reset) if ‘Type’ is zero. Otherwise a soft reset (i.e. a jump to the
        /// reset vector) is done.This is especially useful in the CC2531, for
        /// instance, so that the USB host does not have to contend with the USB
        /// H/W resetting(and thus causing the USB host to re-enumerate the device
        /// which can cause an open virtual serial port to hang.)
        /// </summary>
        public byte ResetType { get; set; }
    }
}