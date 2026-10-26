using System.Collections.Generic;
using System.Reflection.Emit;
using ABCRetailWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailWebApp.Data
{
    public class ABCRetailDbContext : DbContext
    {
        public ABCRetailDbContext(DbContextOptions<ABCRetailDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<ShoppingCartEntity> ShoppingCart { get; set; }
        public DbSet<CustomerOrderEntity> CustomerOrders { get; set; }
        public DbSet<OrderItemEntity> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Customer");

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.LastLoginDate)
                    .IsRequired(false);
            });

            modelBuilder.Entity<ShoppingCartEntity>(entity =>
            {
                entity.ToTable("ShoppingCart");
                entity.HasKey(e => e.CartId);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CartItems)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ProductRowKey)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.DateAdded)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.LastModified)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.UserId, e.ProductRowKey })
                    .HasDatabaseName("IX_ShoppingCart_UserId_ProductRowKey");
            });

            modelBuilder.Entity<CustomerOrderEntity>(entity =>
            {
                entity.ToTable("CustomerOrders");
                entity.HasKey(e => e.OrderId);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.OrderStatus)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.OrderDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.DeliveryAddress)
                    .HasMaxLength(500);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_CustomerOrders_UserId");

                entity.HasIndex(e => e.OrderStatus)
                    .HasDatabaseName("IX_CustomerOrders_OrderStatus");

                entity.HasIndex(e => e.OrderDate)
                    .HasDatabaseName("IX_CustomerOrders_OrderDate");
            });

            modelBuilder.Entity<OrderItemEntity>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(e => e.OrderItemId);

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ProductRowKey)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.Property(e => e.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_OrderItems_OrderId");
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ShoppingCartEntity && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                ((ShoppingCartEntity)entry.Entity).LastModified = DateTime.UtcNow;
            }
        }
    }
}
