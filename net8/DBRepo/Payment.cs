using System;
using System.Collections.Generic;

namespace Shagun.Models;

public partial class Payment
{
    public int Id { get; set; }

    public string Gateway { get; set; } = null!;

    public string GatewayOrderId { get; set; } = null!;

    public string? GatewayPaymentId { get; set; }

    public string? GatewaySignature { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
