using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class VoltaXApiDbContext : DbContext
    {
        private IConfiguration _configuration;


        public VoltaXApiDbContext(DbContextOptions<VoltaXApiDbContext> options) : base(options)
        {

        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .OnDelete(DeleteBehavior.NoAction);
        }

            public DbSet<User> Users { get; set; }
            public DbSet<Administrator> Administrators { get; set; }
            public DbSet<Customer> Customers { get; set; }
            public DbSet<Card> Cards { get; set; }
            public DbSet<ChargingStation> ChargingStations { get; set; }
            public DbSet<ChargePoint> ChargePoints { get; set; }
            public DbSet<Connector> Connectors { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<Transaction> Transactions { get; set; }
            public DbSet<Comment> Comments { get; set; }
            public DbSet<MessageLog> MessageLogs { get; set; }
            public DbSet<Country> Countries { get; set; }
            public DbSet<State> States { get; set; }
            public DbSet<City> Cities { get; set; }
            // Add any Dbset configurations here
    }
}