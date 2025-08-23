using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenConquer.Domain.Enums
{
    public enum NobilityAction
    {
        None = 0,
        Donate,
        List,
        Info,
        QueryRemainingSilver
    }

    public enum NobilityType : uint
    {
        Serf = 0,
        Knight = 1,
        Baron = 3,
        Earl = 5,
        Duke = 7,
        Prince = 9,
        King = 12
    }
}
