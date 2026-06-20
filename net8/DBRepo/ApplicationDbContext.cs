using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shagun.Models;

namespace Shagun.DBRepo;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Contribution> Contributions { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<GiftItem> GiftItems { get; set; }

    public virtual DbSet<Invitee> Invitees { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("contributions");

            entity.HasIndex(e => e.GiftItemId, "gift_item_id");

            entity.HasIndex(e => e.InviteeId, "invitee_id");

            entity.HasIndex(e => e.Id, "ix_contributions_id");

            entity.HasIndex(e => e.PaymentId, "payment_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Anonymous).HasColumnName("anonymous");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.GiftItemId).HasColumnName("gift_item_id");
            entity.Property(e => e.InviteeId).HasColumnName("invitee_id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.GiftItem).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.GiftItemId)
                .HasConstraintName("contributions_ibfk_1");

            entity.HasOne(d => d.Invitee).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.InviteeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contributions_ibfk_2");

            entity.HasOne(d => d.Payment).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contributions_ibfk_3");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("events");

            entity.HasIndex(e => e.HostId, "host_id");

            entity.HasIndex(e => e.Id, "ix_events_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BrideName)
                .HasMaxLength(100)
                .HasColumnName("bride_name");
            entity.Property(e => e.CoverPhotoUrl)
                .HasMaxLength(255)
                .HasColumnName("cover_photo_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EventDate).HasColumnName("event_date");
            entity.Property(e => e.EventName)
                .HasMaxLength(150)
                .HasColumnName("event_name");
            entity.Property(e => e.GroomName)
                .HasMaxLength(100)
                .HasColumnName("groom_name");
            entity.Property(e => e.HostId).HasColumnName("host_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Venue)
                .HasMaxLength(255)
                .HasColumnName("venue");

            entity.HasOne(d => d.Host).WithMany(p => p.Events)
                .HasForeignKey(d => d.HostId)
                .HasConstraintName("events_ibfk_1");
        });

        modelBuilder.Entity<GiftItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("gift_items");

            entity.HasIndex(e => e.EventId, "event_id");

            entity.HasIndex(e => e.Id, "ix_gift_items_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContributedAmount)
                .HasPrecision(12, 2)
                .HasColumnName("contributed_amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EstimatedCost)
                .HasPrecision(12, 2)
                .HasColumnName("estimated_cost");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Event).WithMany(p => p.GiftItems)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("gift_items_ibfk_1");
        });

        modelBuilder.Entity<Invitee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("invitees");

            entity.HasIndex(e => e.EventId, "event_id");

            entity.HasIndex(e => e.Id, "ix_invitees_id");

            entity.HasIndex(e => e.InviteToken, "ix_invitees_invite_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.InviteToken)
                .HasMaxLength(100)
                .HasColumnName("invite_token");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Relation)
                .HasMaxLength(50)
                .HasColumnName("relation");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Event).WithMany(p => p.Invitees)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("invitees_ibfk_1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("payments");

            entity.HasIndex(e => e.GatewayOrderId, "ix_payments_gateway_order_id").IsUnique();

            entity.HasIndex(e => e.GatewayPaymentId, "ix_payments_gateway_payment_id").IsUnique();

            entity.HasIndex(e => e.Id, "ix_payments_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasColumnName("currency");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.GatewayOrderId)
                .HasMaxLength(100)
                .HasColumnName("gateway_order_id");
            entity.Property(e => e.GatewayPaymentId)
                .HasMaxLength(100)
                .HasColumnName("gateway_payment_id");
            entity.Property(e => e.GatewaySignature)
                .HasMaxLength(255)
                .HasColumnName("gateway_signature");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "ix_users_email").IsUnique();

            entity.HasIndex(e => e.Id, "ix_users_id");

            entity.HasIndex(e => e.Phone, "ix_users_phone").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
        });

        

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
