using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Data.Configurations
{
    public class CategoryConfiguration :IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x=> x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x=> x.Description).IsRequired().HasMaxLength(200);

            builder.HasOne(x=>x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(c=>c.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
