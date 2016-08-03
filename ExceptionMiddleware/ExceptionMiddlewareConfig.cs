using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Logging;

namespace Cactus.Owin
{
    public class ExceptionMiddlewareConfig
    {
        public bool EnableDetails { get; set; }
        public ILogger Log { get; set; }
    }
}
