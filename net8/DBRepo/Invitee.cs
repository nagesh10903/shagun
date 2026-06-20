using System;
using System.Collections.Generic;

namespace Shagun.Models;

public partial class Invitee
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Relation { get; set; }

    public string InviteToken { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public virtual Event Event { get; set; } = null!;
}
