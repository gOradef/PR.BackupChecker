using HostLibrary.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreateConfig
{
    internal record ResultItem(string Host, HostPath path);

    internal class ResultPathItemBuilder(string Host) // for paths resolver
    {
        private string Host = Host;

        internal ResultItem Create(HostPath path)
        {
            return new(Host, path);
        }
    }

}
