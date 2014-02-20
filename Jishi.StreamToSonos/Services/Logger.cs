using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Jishi.StreamToSonos.Services
{
    public static class Logger
    {
        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(type);
        }
    }
}