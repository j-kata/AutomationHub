using System.Text.Json;
using AutomationHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomationHub.Infrastructure.Data.Configurations;

public class RuleActionConfiguration : IEntityTypeConfiguration<RuleAction>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();

    public void Configure(EntityTypeBuilder<RuleAction> builder)
    {
        builder.ToTable("RuleActions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActionType)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(a => a.Parameters)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions) ?? new()
            );
    }
}