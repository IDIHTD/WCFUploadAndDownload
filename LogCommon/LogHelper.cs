using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace LogCommon
{
   public class LogHelper
    {
        public static readonly ILog Log = LogManager.GetLogger("Log");
        public static readonly ILog Error = LogManager.GetLogger("Error");
        public static readonly ILog Api = LogManager.GetLogger("Api");
        public static readonly ILog Email = LogManager.GetLogger("logger");
        public static readonly ILog Search = LogManager.GetLogger("Search");
        public static readonly ILog UserFrom = LogManager.GetLogger("UserFrom");
    }
}
