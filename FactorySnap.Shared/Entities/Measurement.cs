using FactorySnap.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactorySnap.Shared.Entities;

public class Measurement
{
    public DateTime Timestamp { get; init; }
    public string MachineId { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public double ValNum { get; set; }
    public string? ValText { get; set; }
    public DataType OriginalType { get; init; }
    public uint Quality { get; init; }
}

public class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.HasKey(m => new { m.MachineId, m.TagName, m.Timestamp });
        builder.Property(m => m.MachineId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.TagName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.ValText).HasMaxLength(100);
        builder.Property(m => m.OriginalType).IsRequired();
        builder.HasIndex(m => new { m.MachineId, m.TagName, m.Timestamp })
            .IncludeProperties(m => m.ValNum);
    }
}

