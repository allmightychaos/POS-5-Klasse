using System.IO;
using Microsoft.EntityFrameworkCore;
using OsterhasenProgramm.Models;

namespace OsterhasenProgramm.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "osterhase.db");
            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
