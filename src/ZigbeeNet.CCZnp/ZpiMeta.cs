﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace ZigbeeNet.CC
{
    public static class ZpiMeta
    {
        private static string zclMetaFile = LoadZpiMetaFile();

        private static string LoadZpiMetaFile()
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"{Assembly.GetCallingAssembly().GetName().Name}.Defs.zpi_meta.json"))
            {
                StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        private static Dictionary<SubSystem, List<ZpiObject>> _zpiObjects;

        public static Dictionary<SubSystem, List<ZpiObject>> ZpiObjects
        {
            get
            {
                if(_zpiObjects == null)
                {
                    _zpiObjects = new Dictionary<SubSystem, List<ZpiObject>>();

                    JObject jSubSys = JsonConvert.DeserializeObject<JObject>(zclMetaFile);

                    LoadSubSys(jSubSys);
                }
                return _zpiObjects;
            }
            set { _zpiObjects = value; }
        }

        private static void LoadSubSys(JObject root)
        {
            foreach (JObject subSys in root.Values())
            {
                string subSysName = subSys.Path;
                SubSystem subSystem = (SubSystem)subSys["id"].Value<int>();

                if(_zpiObjects.ContainsKey(subSystem) == false)
                {
                    _zpiObjects.Add(subSystem, new List<ZpiObject>());
                }

                foreach (var attr in subSys["cmds"].Values())
                {
                    ZpiObject zpiObject = new ZpiObject();
                    zpiObject.Name = attr.Path.Substring(attr.Path.LastIndexOf(".") + 1);
                    zpiObject.CommandId = (byte)attr["cmdId"].Value<byte>();
                    zpiObject.Type = (MessageType)attr["type"].Value<int>();
                    zpiObject.SubSystem = subSystem;

                    foreach (JObject item in attr["params"]["req"])
                    {
                        foreach (var itm in item)
                        {
                            ZpiArgument zpiArgument = new ZpiArgument()
                            {
                                Name = itm.Key,
                                ParamType = (ParamType)itm.Value.Value<int>()
                            };

                            zpiObject.RequestArguments.AddOrUpdate(zpiArgument);
                        }
                    }

                    if (attr["params"].Value<JArray>("rsp") != null)
                    {
                        foreach (JObject item in attr["params"]["rsp"])
                        {
                            foreach (var itm in item)
                            {
                                ZpiArgument zpiArgument = new ZpiArgument()
                                {
                                    Name = itm.Key,
                                    ParamType = (ParamType)itm.Value.Value<int>()
                                };

                                zpiObject.ResponseArguments.AddOrUpdate(zpiArgument);
                            }
                        }
                    }

                    _zpiObjects[subSystem].Add(zpiObject);
                }
            }
        }

        public static ZpiObject GetCommand(SubSystem subSystem, byte cmdId)
        {
            return ZpiObjects[subSystem].Single(cmd => cmd.CommandId == cmdId);
        }

        public static MessageType GetMessageType(SubSystem subSystem, byte cmdId)
        {
            return GetCommand(subSystem, cmdId).Type;
        }

        public static ArgumentCollection GetReqArguments(SubSystem subSystem, byte cmdId)
        {
            return GetCommand(subSystem, cmdId).RequestArguments;
        }

        public static ArgumentCollection GetRspArguments(SubSystem subSystem, byte cmdId)
        {
            return GetCommand(subSystem, cmdId).ResponseArguments;
        }
    }
}
