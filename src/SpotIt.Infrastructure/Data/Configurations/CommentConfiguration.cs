using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations
{
    public class CommentConfiguration :IEntityTypeConfiguration<Comment>
    {
        public void Configure (EntityTypeBuilder<Comment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            

            builder.HasOne(x => x.Post)
                .WithMany(c=>c.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Author)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PostId);
        }
    }
}
