using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations
{
    public class LikeConfiguration : IEntityTypeConfiguration<Like>
    {
        public void Configure (EntityTypeBuilder<Like> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.CreatedAt)
                   .HasColumnType("timestamptz");

            builder.HasOne(l => l.User)
                   .WithMany(u => u.Likes)
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.Post)
                   .WithMany(p => p.Likes)
                   .HasForeignKey(l => l.PostId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(l => new { l.UserId, l.PostId }).IsUnique();
        }
    }
}
