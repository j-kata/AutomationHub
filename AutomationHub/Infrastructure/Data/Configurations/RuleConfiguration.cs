using System.Text.Json;
using AutomationHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomationHub.Infrastructure.Data.Configurations;


public class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.EventType)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(r => r.Source).HasMaxLength(200);
        builder.Property(r => r.Condition).HasMaxLength(500);
        builder.Property(r => r.Priority)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();
        builder.HasMany(r => r.Actions)
            .WithOne()
            .HasForeignKey(a => a.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => new { r.EventType, r.Source });
    }
}