using Microsoft.EntityFrameworkCore;

namespace WebApi;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Book> Books {get; set;}
}