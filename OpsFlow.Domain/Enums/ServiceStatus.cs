using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpsFlow.Domain.Enums
{
    public enum ServiceStatus
    {
        Operational,
        Degraded,
        PartialOutage,
        MajorOutage,
        Maintenance
    }
}
