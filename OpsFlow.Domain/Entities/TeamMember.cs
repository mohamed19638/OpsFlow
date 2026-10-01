using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class TeamMember
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }

        public Team Team { get; set; } = null!;
        public User User { get; set; } = null!;
    }

