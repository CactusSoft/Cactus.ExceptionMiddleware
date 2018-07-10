using Newtonsoft.Json.Serialization;

namespace Cactus.Aspnetcore.ExceptionMiddleware
{
    public class ExceptionMiddlewareConfig
    {
        public bool EnableDetails { get; set; }
        /// <summary>
        /// ContractResolver for JSON serializer. If null, default contract resolver is used
        /// </summary>
        public IContractResolver ContractResolver { get; set; }
    }
}
