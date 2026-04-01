using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using StoreManagementSystem.Models; // Your namespace

namespace Store_Management_System.Models
{
    // CORRECT generic parameter order:
    // TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken
    public class InventoryDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        // Inventory DbSets
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<StockMovement> StockMovements { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map Identity tables to your existing database table names
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasMany(u => u.Orders)
                      .WithOne(o => o.CreatedByNavigation)
                      .HasForeignKey(o => o.CreatedBy);

                entity.HasMany(u => u.StockMovements)
                      .WithOne(s => s.PerformedByNavigation)
                      .HasForeignKey(s => s.PerformedBy);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            modelBuilder.Entity<UserClaim>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("RoleClaims");
            });

            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.ToTable("UserTokens");
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.ParentCategory)
                    .WithMany(p => p.InverseParentCategory)
                    .HasForeignKey(d => d.ParentId);
            });

            // Configure Supplier
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Sku).IsUnique(); // Note: Sku not SKU

                entity.Property(e => e.Sku) // Property name is "Sku" not "SKU"
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CostPrice)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryId);

                entity.HasOne(d => d.Supplier)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.SupplierId);
            });

            // Configure StockMovement
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("StockMovements");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.MovementType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.StockMovements)
                    .HasForeignKey(d => d.ProductId);

                entity.HasOne(d => d.PerformedByNavigation)
                    .WithMany(p => p.StockMovements)
                    .HasForeignKey(d => d.PerformedBy);
            });

            // Configure Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.OrderNumber).IsUnique();

                entity.Property(e => e.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.CreatedBy);
            });

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Total)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.ProductId);
            });
        }
    }
}