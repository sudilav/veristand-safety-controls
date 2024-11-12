using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NationalInstruments.VeriStand.CustomControls
{
    public class HostnameIpValidator
    {
        public static bool IsValidHostnameOrIp(string input)
        {
            // Check if it's a valid IP address
            if (IPAddress.TryParse(input, out _))
            {
                return true;
            }

            // Check if it's a valid hostname
            UriHostNameType hostType = Uri.CheckHostName(input);
            return hostType == UriHostNameType.Dns; // Returns true if it's a valid hostname
        }
    }
}
