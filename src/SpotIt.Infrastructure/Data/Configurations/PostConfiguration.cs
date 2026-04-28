using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations
{
    public class PostConfiguration: IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x=> x.Description).IsRequired().HasMaxLength(4000);
            builder.Property(x => x.Status).HasConversion<string>().HasColumnType("character varying(50)");
            builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");

            builder.HasOne(x=>x.Author).WithMany(u=>u.Posts).HasForeignKey(u => u.AuthorId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Category).WithMany(u => u.Posts).HasForeignKey(u => u.CategoryId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => x.Status);

        }
    }
}
