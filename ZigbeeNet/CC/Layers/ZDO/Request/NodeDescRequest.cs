﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZigbeeNet.CC.ZDO
{
    public class NodeDescRequest : ZpiSREQ
    {
        public NodeDescRequest()
            : base(ZdoCommand.nodeDescReq)
        {

        }

        public ushort DestinationAddress
        {
            get
            {
                return (ushort)RequestArguments["dstaddr"];
            }
            set
            {
                RequestArguments["dstaddr"] = value;
            }
        }

        public ushort NetworkAddressOfInteresst
        {
            get
            {
                return (ushort)RequestArguments["nwkaddrofinterest"];
            }
            set
            {
                RequestArguments["nwkaddrofinterest"] = value;
            }
        }
    }
}
