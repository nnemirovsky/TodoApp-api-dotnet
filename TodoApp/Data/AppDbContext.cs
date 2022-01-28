using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Item> Items { get; set; } = default!;
    public DbSet<ItemList> ItemLists { get; set; } = default!;
    public DbSet<User> Users { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
}
