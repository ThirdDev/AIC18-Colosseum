using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Colosseum.Services.Server
{
    internal class ServerConfig
    {
        public string Map { get; set; }
        public string Deploy { get; } = "false";
        public string ClientsPort { get; set; }
        public string ClientsConnectionTimeout { get; } = "2147483647";
        public string UIEnable { get; } = "false";
        public string UIToken { get; } = "00000000000000000000000000000000";
        public string UIPort { get; set; }
        public string UIConnectionTimeout { get; } = "2147483647";
        public string OCSendToUI { get; } = "false";
        public string OCSendToFile { get; } = "false";
        public string OCFilePath { get; } = "./game.log";

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
