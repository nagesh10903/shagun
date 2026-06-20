using System;
using System.Collections.Generic;

namespace Shagun.Models;

public partial class Contribution
{
    public int Id { get; set; }

    public int GiftItemId { get; set; }

    public int? InviteeId { get; set; }

    public decimal Amount { get; set; }

    public int? PaymentId { get; set; }

    public bool Anonymous { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual GiftItem GiftItem { get; set; } = null!;

    public virtual Invitee? Invitee { get; set; }

    public virtual Payment? Payment { get; set; }
}
