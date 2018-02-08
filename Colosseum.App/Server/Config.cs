using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.App.Server
{
    class Config
    {
        public string Map { get; set; }
        public string Deploy { get; private set; } = "false";
        public string ClientsPort { get; set; }
        public string ClientsConnectionTimeout { get; private set; } = "2147483647";
        public string UIEnable { get; private set; } = "false";
        public string UIToken { get; private set; } = "00000000000000000000000000000000";
        public string UIPort { get; private set; } = "7000";
        public string UIConnectionTimeout { get; private set; } = "2147483647";
        public string OCSendToUI { get; private set; } = "false";
        public string OCSendToFile { get; private set; } = "false";
        public string OCFilePath { get; private set; } = "./game.log";

        public string Serialize()
        {
            var serializeSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };

            return JsonConvert.SerializeObject(this, serializeSettings);
        }
    }
}
