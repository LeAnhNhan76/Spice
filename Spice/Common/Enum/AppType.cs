using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Common.Enum
{
    public sealed class AppType
    {
        public enum ESpicy : byte
        {
            NotSpicy = 0,
            Spicy = 1,
            VerySpicy = 2
        }
    }
}
