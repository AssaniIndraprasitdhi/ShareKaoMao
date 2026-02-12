using Microsoft.EntityFrameworkCore;
using ShareKaoMao.Models;

namespace ShareKaoMao.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Bill> Bills { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemShare> ItemShares { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bill: กำหนด precision สำหรับค่าเงิน
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.Property(b => b.VatPercent).HasColumnType("numeric(18,4)");
                entity.Property(b => b.ServicePercent).HasColumnType("numeric(18,4)");
                entity.Property(b => b.TipAmount).HasColumnType("numeric(18,4)");
            });

            // Person: cascade delete เมื่อลบ Bill
            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasOne(p => p.Bill)
                    .WithMany(b => b.People)
                    .HasForeignKey(p => p.BillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Item: กำหนด precision สำหรับราคา + cascade delete เมื่อลบ Bill
            modelBuilder.Entity<Item>(entity =>
            {
                entity.Property(i => i.Price).HasColumnType("numeric(18,4)");
                entity.HasOne(i => i.Bill)
                    .WithMany(b => b.Items)
                    .HasForeignKey(i => i.BillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ItemShare: join table สำหรับ many-to-many (Item <-> Person)
            // cascade delete ทั้งสองทาง เมื่อลบ Item หรือ Person
            modelBuilder.Entity<ItemShare>(entity =>
            {
                entity.HasOne(s => s.Item)
                    .WithMany(i => i.ItemShares)
                    .HasForeignKey(s => s.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Person)
                    .WithMany(p => p.ItemShares)
                    .HasForeignKey(s => s.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
