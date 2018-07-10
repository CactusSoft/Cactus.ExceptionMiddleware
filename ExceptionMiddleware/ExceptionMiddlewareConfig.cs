using Microsoft.Owin.Logging;
using Newtonsoft.Json.Serialization;

namespace Cactus.Owin
{
    public class ExceptionMiddlewareConfig
    {
        public bool EnableDetails { get; set; }
        /// <summary>
        /// ContractResolver for JSON serializer. If null, default contract resolver is used
        /// </summary>
        public IContractResolver ContractResolver { get; set; }
        public ILogger Log { get; set; }
    }
}
