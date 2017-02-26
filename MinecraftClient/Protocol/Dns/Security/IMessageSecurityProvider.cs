using System;
using System.Collections.Generic;
using System.Text;
using DnDns.Query;

namespace DnDns.Security
{
    public interface IMessageSecurityProvider
    {
        DnsQueryRequest SecureMessage(DnsQueryRequest dnsQueryRequest);
    }
}
