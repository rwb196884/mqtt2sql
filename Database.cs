using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Mqtt2Sql
{
    public class MqttMessage
    {
        public virtual string Topic { get; set; }

        public virtual DateTime Timestamp { get; set; }

        public virtual string Payload { get; set; }
    }

    /// <summary>
    /// This is NOT how to do databases!
    /// </summary>
    public class Database : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfiguration cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddJsonFile("appsettings.json", false)
            .Build();

            optionsBuilder.UseSqlServer(cfg.GetConnectionString("Mqtt"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MqttMessage>().HasKey(z => new { z.Topic, z.Timestamp });
        }

        public virtual DbSet<MqttMessage> Messages { get; set; }
    }
}
