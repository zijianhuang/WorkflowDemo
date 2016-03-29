using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Fonlow.Utilities
{
    public class TraceWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public override void WriteLine()
        {
            if (Trace.Listeners.Count==0)
            {
                base.WriteLine();
            }
            else
            {
                Trace.WriteLine(String.Empty);
            }
        }

        public override void WriteLine(string value)
        {
            if (Trace.Listeners.Count == 0)
            {
                base.WriteLine(value);
            }
            else
            {
                Trace.WriteLine(value);
            }
        }
    }
}
