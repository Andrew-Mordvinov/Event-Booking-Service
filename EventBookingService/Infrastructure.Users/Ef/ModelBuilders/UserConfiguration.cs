using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Infrastructure.Users.Ef;

namespace Infrastructure.Users.Ef.ModelBuilders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable(TableNames.Users)
            .HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.Login)
            .IsRequired();

        builder.HasIndex(b => b.Login)
            .HasDatabaseName(ConstraintNames.LoginUnique)
            .IsUnique();
    }
}
