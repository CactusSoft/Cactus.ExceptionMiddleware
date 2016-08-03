using Microsoft.Owin.Logging;

namespace Cactus.Owin
{
    public class ExceptionMiddlewareConfig
    {
        public bool EnableDetails { get; set; }
        public ILogger Log { get; set; }
    }
}
