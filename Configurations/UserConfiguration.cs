using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoviesTelegramBot.Models;

namespace MoviesTelegramBot.Configurations;

public class UserConfiguration: IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        builder.HasKey(u=>u.UserId);
    }
}