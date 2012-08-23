using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Martin.GCode
{
    class GCode
    {
        internal GCodeCommand Command { get; set; }
        internal List<GCodeParameter> Parameters { get; set; }

        internal GCode()
        {
            Parameters = new List<GCodeParameter>();
        }
    }
}
