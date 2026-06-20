using System;
using System.Collections.Generic;

namespace Shagun.Models;

public partial class GiftItem
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public decimal EstimatedCost { get; set; }

    public decimal ContributedAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public virtual Event Event { get; set; } = null!;
}
