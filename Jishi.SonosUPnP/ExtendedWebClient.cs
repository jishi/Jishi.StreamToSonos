using System.Diagnostics;
using System.Net;

namespace Jishi.SonosUPnP
{
    public class ExtendedWebClient : WebClient
    {
        [DebuggerNonUserCode]
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            // we need a lower timeout
            request.Timeout = 500;
			
			
            try
            {
                return base.GetWebResponse(request);
            }
            catch
            {
                throw;
            }
        }
    }
}