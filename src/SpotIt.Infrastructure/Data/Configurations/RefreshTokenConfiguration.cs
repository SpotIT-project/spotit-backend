using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x=>x.Id);

        builder.Property(x=>x.Token).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");

        builder.HasOne(x=>x.User)
            .WithMany(x=>x.RefreshTokens)
            .HasForeignKey(x=>x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);


    }

}
