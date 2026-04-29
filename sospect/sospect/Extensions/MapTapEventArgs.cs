using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sospect.Extensions
{
    public class MapTapEventArgs : EventArgs
    {
        public Location Position { get; set; }
    }
}
