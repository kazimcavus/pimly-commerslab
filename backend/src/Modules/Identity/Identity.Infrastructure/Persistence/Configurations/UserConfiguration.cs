using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>User varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Ignore(u => u.DomainEvents);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
