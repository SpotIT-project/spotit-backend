using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations
{
    public class StatusHistoryConfiguration :IEntityTypeConfiguration<StatusHistory>
    {
        public void Configure (EntityTypeBuilder<StatusHistory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(s => s.OldStatus)
                 .HasColumnType("post_status")
                 .HasConversion<string>();

            builder.Property(s => s.NewStatus)
                   .HasColumnType("post_status")
                   .HasConversion<string>();

            builder.Property(s => s.Note)
                   .HasMaxLength(1000);

            builder.Property(s => s.ChangedAt)
                   .HasColumnType("timestamptz");

            builder.HasOne(s => s.Post)
                   .WithMany(p => p.StatusHistories)
                   .HasForeignKey(s => s.PostId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.ChangedBy)
                   .WithMany(u => u.StatusChanges)
                   .HasForeignKey(s => s.ChangedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
