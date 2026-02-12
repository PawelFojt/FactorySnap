using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactorySnap.Shared.Entities;

public class OpcServerConfig
{
    public int Id { get; init; }
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class OpcNodeConfig
{
    public int Id { get; init; }
    public string NodeId { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public class OpcServerConfigConfiguration : IEntityTypeConfiguration<OpcServerConfig>
{
    public void Configure(EntityTypeBuilder<OpcServerConfig> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}

public class OpcNodeConfigConfiguration : IEntityTypeConfiguration<OpcNodeConfig>
{
    public void Configure(EntityTypeBuilder<OpcNodeConfig> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NodeId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TagName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.HasIndex(x => x.SortOrder);
    }
}