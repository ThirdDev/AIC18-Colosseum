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
        public string Deploy { get; set; } = "false";
        public string ClientsPort { get; set; }
        public string ClientsConnectionTimeout { get; set; }
        public string UIEnable { get; set; }
        public string UIToken { get; set; }
        public string UIPort { get; set; }
        public string UIConnectionTimeout { get; set; }
        public string OCSendToUI { get; set; }
        public string OCSendToFile { get; set; }
        public string OCFilePath { get; set; }

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
