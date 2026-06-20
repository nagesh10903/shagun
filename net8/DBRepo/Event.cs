using System;
using System.Collections.Generic;

namespace Shagun.Models;

public partial class Event
{
    public int Id { get; set; }

    public int HostId { get; set; }

    public string EventName { get; set; } = null!;

    public string GroomName { get; set; } = null!;

    public string BrideName { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    public string Venue { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverPhotoUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GiftItem> GiftItems { get; set; } = new List<GiftItem>();

    public virtual User Host { get; set; } = null!;

    public virtual ICollection<Invitee> Invitees { get; set; } = new List<Invitee>();
}
