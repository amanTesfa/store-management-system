using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Store_Management_System.Models;

public partial class InventoryDbContext : DbContext
{
    public InventoryDbContext()
    {
    }

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountLedger> AccountLedgers { get; set; }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<ArticleBarcode> ArticleBarcodes { get; set; }

    public virtual DbSet<ArticlePrice> ArticlePrices { get; set; }

    public virtual DbSet<ArticleTaxMapping> ArticleTaxMappings { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BeginningBalance> BeginningBalances { get; set; }

    public virtual DbSet<BeginningBalanceLine> BeginningBalanceLines { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ChargeAllocation> ChargeAllocations { get; set; }

    public virtual DbSet<ChargeType> ChargeTypes { get; set; }

    public virtual DbSet<ClosedRelation> ClosedRelations { get; set; }

    public virtual DbSet<ClosingPeriod> ClosingPeriods { get; set; }

    public virtual DbSet<Consignee> Consignees { get; set; }

    public virtual DbSet<Consignor> Consignors { get; set; }

    public virtual DbSet<CurrentStock> CurrentStocks { get; set; }

    public virtual DbSet<CustomerTaxExemption> CustomerTaxExemptions { get; set; }

    public virtual DbSet<FiscalPeriod> FiscalPeriods { get; set; }

    public virtual DbSet<NumberingSequence> NumberingSequences { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Preference> Preferences { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleClaim> RoleClaims { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<StorageLocation> StorageLocations { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierWithholdingMapping> SupplierWithholdingMappings { get; set; }

    public virtual DbSet<SystemConstant> SystemConstants { get; set; }

    public virtual DbSet<TaxAuthority> TaxAuthorities { get; set; }

    public virtual DbSet<TaxGroup> TaxGroups { get; set; }

    public virtual DbSet<TaxGroupRule> TaxGroupRules { get; set; }

    public virtual DbSet<TaxRule> TaxRules { get; set; }

    public virtual DbSet<TransactionReference> TransactionReferences { get; set; }

    public virtual DbSet<TransferOrder> TransferOrders { get; set; }

    public virtual DbSet<TransferOrderLine> TransferOrderLines { get; set; }

    public virtual DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserClaim> UserClaims { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherCharge> VoucherCharges { get; set; }

    public virtual DbSet<VoucherLine> VoucherLines { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WithholdingTaxRule> WithholdingTaxRules { get; set; }

    public virtual DbSet<WithholdingTransaction> WithholdingTransactions { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=THANOS;Database=InventoryDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountLedger>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AccountL__3214EC0752954F71");

            entity.ToTable("AccountLedger");

            entity.Property(e => e.AccountCode).HasMaxLength(20);
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.CreditAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.DebitAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Reference).HasMaxLength(255);

            entity.HasOne(d => d.FiscalPeriod).WithMany(p => p.AccountLedgers)
                .HasForeignKey(d => d.FiscalPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ledger_Period");

            entity.HasOne(d => d.Voucher).WithMany(p => p.AccountLedgers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ledger_Voucher");
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Activiti__3214EC07312D36C5");

            entity.HasIndex(e => e.ActivityCode, "UQ__Activiti__2D7E17A7EB64520A").IsUnique();

            entity.Property(e => e.ActivityCode).HasMaxLength(20);
            entity.Property(e => e.ActivityName).HasMaxLength(100);
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.AffectsAccounting).HasDefaultValue(true);
            entity.Property(e => e.AffectsStock).HasDefaultValue(true);
            entity.Property(e => e.Direction).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Articles__3214EC075732881A");

            entity.HasIndex(e => e.ArticleCode, "IX_Articles_ArticleCode");

            entity.HasIndex(e => e.ArticleName, "IX_Articles_ArticleName");

            entity.HasIndex(e => e.ArticleCode, "UQ__Articles__3B99B1DE17FFB4E9").IsUnique();

            entity.Property(e => e.ArticleCategory).HasMaxLength(100);
            entity.Property(e => e.ArticleCode).HasMaxLength(50);
            entity.Property(e => e.ArticleGroup).HasMaxLength(100);
            entity.Property(e => e.ArticleName).HasMaxLength(200);
            entity.Property(e => e.ArticleType)
                .HasMaxLength(20)
                .HasDefaultValue("Product");
            entity.Property(e => e.AverageCost).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Height).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPurchasable).HasDefaultValue(true);
            entity.Property(e => e.IsSellable).HasDefaultValue(true);
            entity.Property(e => e.IsStockable).HasDefaultValue(true);
            entity.Property(e => e.LastPurchasePrice).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Length).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.MaxStockLevel).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReorderQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.SafetyStock).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.StandardCost).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.StandardPrice).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TaxGroup).HasMaxLength(50);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Weight).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Width).HasColumnType("decimal(18, 6)");
            // Relationship to Category
            entity.HasOne(d => d.Category)
                .WithMany(p => p.Articles)
                .HasForeignKey(d => d.ArticleCategory)
                .HasConstraintName("FK_Articles_Category");
            entity.HasOne(d => d.BaseUnit).WithMany(p => p.ArticleBaseUnits)
                .HasForeignKey(d => d.BaseUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articles_BaseUnit");

            entity.HasOne(d => d.PurchaseUnit).WithMany(p => p.ArticlePurchaseUnits)
                .HasForeignKey(d => d.PurchaseUnitId)
                .HasConstraintName("FK_Articles_PurchaseUnit");

            entity.HasOne(d => d.SalesUnit).WithMany(p => p.ArticleSalesUnits)
                .HasForeignKey(d => d.SalesUnitId)
                .HasConstraintName("FK_Articles_SalesUnit");
        });

        modelBuilder.Entity<ArticleBarcode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ArticleB__3214EC07294926B6");

            entity.HasIndex(e => e.Barcode, "UQ_Barcode").IsUnique();

            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.BarcodeType)
                .HasMaxLength(20)
                .HasDefaultValue("CODE128");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Article).WithMany(p => p.ArticleBarcodes)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleBarcodes_Article");
        });

        modelBuilder.Entity<ArticlePrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ArticleP__3214EC07C8423F62");

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .HasDefaultValue("USD");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.PriceType).HasMaxLength(50);

            entity.HasOne(d => d.Article).WithMany(p => p.ArticlePrices)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticlePrices_Article");
        });

        modelBuilder.Entity<ArticleTaxMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ArticleT__3214EC07ACAD5D27");

            entity.HasOne(d => d.Article).WithMany(p => p.ArticleTaxMappings)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleTax_Article");

            entity.HasOne(d => d.TaxGroup).WithMany(p => p.ArticleTaxMappings)
                .HasForeignKey(d => d.TaxGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleTax_Group");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attachme__3214EC0742D35A6F");

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC076553CB04");

            entity.Property(e => e.Action).HasMaxLength(20);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<BeginningBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Beginnin__3214EC07754B364E");

            entity.HasIndex(e => e.BalanceNumber, "UQ__Beginnin__EB7FB1606BCEFD76").IsUnique();

            entity.Property(e => e.BalanceNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");

            entity.HasOne(d => d.FiscalPeriod).WithMany(p => p.BeginningBalances)
                .HasForeignKey(d => d.FiscalPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BeginningBalances_Period");
        });

        modelBuilder.Entity<BeginningBalanceLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Beginnin__3214EC078CD4A62E");

            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.TotalValue).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.Article).WithMany(p => p.BeginningBalanceLines)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BBLines_Article");

            entity.HasOne(d => d.BeginningBalance).WithMany(p => p.BeginningBalanceLines)
                .HasForeignKey(d => d.BeginningBalanceId)
                .HasConstraintName("FK_BBLines_Balance");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.BeginningBalanceLines)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BBLines_Warehouse");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Branches__3214EC079EF3BD99");

            entity.HasIndex(e => e.BranchCode, "UQ__Branches__1C61B88820C4E279").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BranchCode).HasMaxLength(20);
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.BranchType).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ManagerName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07B3ACD013");

            entity.HasIndex(e => e.IsDeleted, "IX_Categories_IsDeleted");

            entity.HasIndex(e => e.ParentId, "IX_Categories_ParentId");

            entity.HasIndex(e => e.Name, "UQ__Categori__737584F6C2137EB4").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Categories_ParentId");
        });

        modelBuilder.Entity<ChargeAllocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChargeAl__3214EC07A0A5816B");

            entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.AllocationMethod).HasMaxLength(50);

            entity.HasOne(d => d.VoucherCharge).WithMany(p => p.ChargeAllocations)
                .HasForeignKey(d => d.VoucherChargeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChargeAlloc_Charge");

            entity.HasOne(d => d.VoucherLine).WithMany(p => p.ChargeAllocations)
                .HasForeignKey(d => d.VoucherLineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChargeAlloc_Line");
        });

        modelBuilder.Entity<ChargeType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChargeTy__3214EC076FC9A404");

            entity.HasIndex(e => e.ChargeCode, "UQ__ChargeTy__3FFCA5B4809E7748").IsUnique();

            entity.Property(e => e.CalculationMethod).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ChargeCode).HasMaxLength(30);
            entity.Property(e => e.ChargeName).HasMaxLength(100);
            entity.Property(e => e.DefaultRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.GlaccountCode)
                .HasMaxLength(20)
                .HasColumnName("GLAccountCode");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsTaxable).HasDefaultValue(true);
        });

        modelBuilder.Entity<ClosedRelation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClosedRe__3214EC07014CE610");

            entity.Property(e => e.ClosedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RelationType).HasMaxLength(50);

            entity.HasOne(d => d.ClosingVoucher).WithMany(p => p.ClosedRelationClosingVouchers)
                .HasForeignKey(d => d.ClosingVoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClosedRel_Closing");

            entity.HasOne(d => d.OriginalVoucher).WithMany(p => p.ClosedRelationOriginalVouchers)
                .HasForeignKey(d => d.OriginalVoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClosedRel_Original");
        });

        modelBuilder.Entity<ClosingPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClosingP__3214EC071C6FC247");

            entity.Property(e => e.ClosedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ClosingType).HasMaxLength(20);
            entity.Property(e => e.StockClosingValue).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.FiscalPeriod).WithMany(p => p.ClosingPeriods)
                .HasForeignKey(d => d.FiscalPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClosingPeriods_Period");
        });

        modelBuilder.Entity<Consignee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Consigne__3214EC07193DAE67");

            entity.HasIndex(e => e.ConsigneeCode, "UQ__Consigne__6CDF753AA5B227A8").IsUnique();

            entity.Property(e => e.BillingAddress).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ConsigneeCode).HasMaxLength(50);
            entity.Property(e => e.ConsigneeName).HasMaxLength(200);
            entity.Property(e => e.ConsigneeType)
                .HasMaxLength(20)
                .HasDefaultValue("Customer");
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.CurrentBalance).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mobile).HasMaxLength(50);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.PriceLevel).HasMaxLength(50);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.TaxNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<Consignor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Consigno__3214EC072BF1B6A7");

            entity.HasIndex(e => e.ConsignorCode, "UQ__Consigno__56588144082DAE9E").IsUnique();

            entity.Property(e => e.AddressLine1).HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ConsignorCode).HasMaxLength(50);
            entity.Property(e => e.ConsignorName).HasMaxLength(200);
            entity.Property(e => e.ConsignorType)
                .HasMaxLength(20)
                .HasDefaultValue("Supplier");
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Fax).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mobile).HasMaxLength(50);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.TaxNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<CurrentStock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CurrentS__3214EC0795583E79");

            entity.ToTable("CurrentStock");

            entity.HasIndex(e => new { e.ArticleId, e.WarehouseId, e.StorageLocationId, e.BatchNumber, e.SerialNumber }, "UQ_StockItem").IsUnique();

            entity.Property(e => e.AvailableQuantity)
                .HasComputedColumnSql("([Quantity]-[ReservedQuantity])", false)
                .HasColumnType("decimal(19, 6)");
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);

            entity.HasOne(d => d.Article).WithMany(p => p.CurrentStocks)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrentStock_Article");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.CurrentStocks)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrentStock_Warehouse");
        });

        modelBuilder.Entity<CustomerTaxExemption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC0786327449");

            entity.Property(e => e.ExemptionCertificate).HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.Consignee).WithMany(p => p.CustomerTaxExemptions)
                .HasForeignKey(d => d.ConsigneeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerExempt_Consignee");

            entity.HasOne(d => d.TaxRule).WithMany(p => p.CustomerTaxExemptions)
                .HasForeignKey(d => d.TaxRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerExempt_Rule");
        });

        modelBuilder.Entity<FiscalPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FiscalPe__3214EC07FE431F5A");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.PeriodName).HasMaxLength(50);
            entity.Property(e => e.PeriodType).HasMaxLength(20);
        });

        modelBuilder.Entity<NumberingSequence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Numberin__3214EC07F8393BFB");

            entity.HasIndex(e => e.SequenceCode, "UQ__Numberin__83FAC6E08216CCB5").IsUnique();

            entity.Property(e => e.CurrentNumber).HasDefaultValue(1);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NumberLength).HasDefaultValue(6);
            entity.Property(e => e.Prefix).HasMaxLength(20);
            entity.Property(e => e.ResetPattern).HasMaxLength(20);
            entity.Property(e => e.ResetValue).HasMaxLength(20);
            entity.Property(e => e.SequenceCode).HasMaxLength(50);
            entity.Property(e => e.SequenceName).HasMaxLength(100);
            entity.Property(e => e.Suffix).HasMaxLength(20);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC075F88B5C2");

            entity.HasIndex(e => e.OrderNumber, "UQ__Orders__CAC5E7433F601CCA").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CustomerEmail).HasMaxLength(256);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_CreatedBy");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC077993D855");

            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_OrderId");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_ProductId");
        });

        modelBuilder.Entity<Preference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Preferen__3214EC0769784E0E");

            entity.HasIndex(e => e.PreferenceKey, "UQ__Preferen__224AFDE8269180AC").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PreferenceKey).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ValueType).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC07D447CDEF");

            entity.HasIndex(e => e.Sku, "UQ__Products__CA1ECF0DAEC7C803").IsUnique();

            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_CategoryId");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Products_SupplierId");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07A80B179D");

            entity.HasIndex(e => e.NormalizedName, "IX_Roles_NormalizedName");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F686B6AEE5").IsUnique();

            entity.HasIndex(e => e.NormalizedName, "UQ__Roles__A93C97B9A0C540B6").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoleClai__3214EC07366BD4A6");

            entity.HasIndex(e => e.RoleId, "IX_RoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_RoleClaims_RoleId");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StockMov__3214EC0702412068");

            entity.HasIndex(e => e.ArticleId, "IX_StockMovements_ArticleId");

            entity.HasIndex(e => e.MovementDate, "IX_StockMovements_MovementDate");

            entity.HasIndex(e => e.VoucherId, "IX_StockMovements_VoucherId");

            entity.HasIndex(e => e.MovementNumber, "UQ__StockMov__ED5E71C8F3D642A0").IsUnique();

            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.MovementDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.MovementNumber).HasMaxLength(50);
            entity.Property(e => e.MovementType).HasMaxLength(20);
            entity.Property(e => e.NewStock).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.PreviousStock).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.Article).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Article");

            entity.HasOne(d => d.Voucher).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Voucher");

            entity.HasOne(d => d.VoucherLine).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.VoucherLineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_VoucherLine");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Warehouse");
        });

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StorageL__3214EC07AD05DA3C");

            entity.HasIndex(e => new { e.WarehouseId, e.LocationCode }, "UQ_Location_Code").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LocationCode).HasMaxLength(50);
            entity.Property(e => e.LocationName).HasMaxLength(100);
            entity.Property(e => e.Zone).HasMaxLength(50);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StorageLocations)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StorageLocations_Warehouse");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Supplier__3214EC071448B639");

            entity.HasIndex(e => e.IsActive, "IX_Suppliers_IsActive");

            entity.HasIndex(e => e.IsDeleted, "IX_Suppliers_IsDeleted");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<SupplierWithholdingMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Supplier__3214EC0775FD0202");

            entity.Property(e => e.ExemptionCertificate).HasMaxLength(100);

            entity.HasOne(d => d.Consignor).WithMany(p => p.SupplierWithholdingMappings)
                .HasForeignKey(d => d.ConsignorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierWHT_Consignor");

            entity.HasOne(d => d.WithholdingRule).WithMany(p => p.SupplierWithholdingMappings)
                .HasForeignKey(d => d.WithholdingRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierWHT_Rule");
        });

        modelBuilder.Entity<SystemConstant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemCo__3214EC07AEE61A18");

            entity.HasIndex(e => new { e.ConstantGroup, e.ConstantCode }, "UQ_Constants_GroupCode").IsUnique();

            entity.Property(e => e.ConstantCode).HasMaxLength(50);
            entity.Property(e => e.ConstantGroup).HasMaxLength(50);
            entity.Property(e => e.ConstantName).HasMaxLength(100);
            entity.Property(e => e.ConstantValue).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<TaxAuthority>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaxAutho__3214EC071757A33A");

            entity.HasIndex(e => e.AuthorityCode, "UQ__TaxAutho__AB4110C4BEEDE2C2").IsUnique();

            entity.Property(e => e.AuthorityCode).HasMaxLength(20);
            entity.Property(e => e.AuthorityName).HasMaxLength(100);
            entity.Property(e => e.AuthorityType).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
        });

        modelBuilder.Entity<TaxGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaxGroup__3214EC07CF978E4B");

            entity.HasIndex(e => e.GroupCode, "UQ__TaxGroup__3B97438077E424B0").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GroupCode).HasMaxLength(20);
            entity.Property(e => e.GroupName).HasMaxLength(100);
        });

        modelBuilder.Entity<TaxGroupRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaxGroup__3214EC07D2014A70");

            entity.HasOne(d => d.TaxGroup).WithMany(p => p.TaxGroupRules)
                .HasForeignKey(d => d.TaxGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaxGroupRules_Group");

            entity.HasOne(d => d.TaxRule).WithMany(p => p.TaxGroupRules)
                .HasForeignKey(d => d.TaxRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaxGroupRules_Rule");
        });

        modelBuilder.Entity<TaxRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaxRules__3214EC0757F3D342");

            entity.HasIndex(e => e.TaxCode, "UQ__TaxRules__12945A2854A75B19").IsUnique();

            entity.Property(e => e.CalculationMethod).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TaxCode).HasMaxLength(20);
            entity.Property(e => e.TaxName).HasMaxLength(100);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TaxType).HasMaxLength(20);

            entity.HasOne(d => d.TaxAuthority).WithMany(p => p.TaxRules)
                .HasForeignKey(d => d.TaxAuthorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaxRules_Authority");
        });

        modelBuilder.Entity<TransactionReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transact__3214EC07632C7034");

            entity.HasIndex(e => new { e.SourceVoucherId, e.TargetVoucherId }, "UQ_TransRef").IsUnique();

            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReferenceDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ReferenceType).HasMaxLength(50);

            entity.HasOne(d => d.SourceVoucher).WithMany(p => p.TransactionReferenceSourceVouchers)
                .HasForeignKey(d => d.SourceVoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransRef_Source");

            entity.HasOne(d => d.TargetVoucher).WithMany(p => p.TransactionReferenceTargetVouchers)
                .HasForeignKey(d => d.TargetVoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransRef_Target");
        });

        modelBuilder.Entity<TransferOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transfer__3214EC07C4840FA6");

            entity.HasIndex(e => e.TransferNumber, "UQ__Transfer__02A4FA0DCC9D8E5B").IsUnique();

            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransferNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<TransferOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transfer__3214EC073266AF75");

            entity.Property(e => e.QuantityReceived).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.QuantityRequested).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.QuantityShipped).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 6)");
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UnitOfMe__3214EC07095722D6");

            entity.HasIndex(e => e.UnitCode, "UQ__UnitOfMe__0665E6D9C69EA96A").IsUnique();

            entity.Property(e => e.ConversionFactor)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UnitCode).HasMaxLength(20);
            entity.Property(e => e.UnitName).HasMaxLength(50);
            entity.Property(e => e.UnitType).HasMaxLength(20);

            entity.HasOne(d => d.BaseUnit).WithMany(p => p.InverseBaseUnit)
                .HasForeignKey(d => d.BaseUnitId)
                .HasConstraintName("FK_UOM_BaseUnit");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC078B2A349D");

            entity.HasIndex(e => e.NormalizedEmail, "IX_Users_NormalizedEmail");

            entity.HasIndex(e => e.NormalizedUserName, "IX_Users_NormalizedUserName");

            entity.HasIndex(e => e.NormalizedUserName, "UQ__Users__54E8BE227E29C84D").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ__Users__C9F284560967A0B2").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.LockoutEnabled).HasDefaultValue(true);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_UserRoles_RoleId"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_UserRoles_UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserClai__3214EC072DCD78B0");

            entity.HasIndex(e => e.UserId, "IX_UserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserClaims)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserClaims_UserId");
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_UserLogins_UserId");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserLogins_UserId");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserTokens_UserId");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vouchers__3214EC07BC4E32A3");

            entity.HasIndex(e => e.ConsigneeId, "IX_Vouchers_ConsigneeId");

            entity.HasIndex(e => e.ConsignorId, "IX_Vouchers_ConsignorId");

            entity.HasIndex(e => e.Status, "IX_Vouchers_Status");

            entity.HasIndex(e => e.VoucherDate, "IX_Vouchers_VoucherDate");

            entity.HasIndex(e => e.VoucherNumber, "UQ__Vouchers__56C64C171F506F50").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .HasDefaultValue("USD");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ExchangeRate)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.VoucherNumber).HasMaxLength(50);
            entity.Property(e => e.VoucherType).HasMaxLength(50);

            entity.HasOne(d => d.Activity).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.ActivityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vouchers_Activity");

            entity.HasOne(d => d.Consignee).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.ConsigneeId)
                .HasConstraintName("FK_Vouchers_Consignee");

            entity.HasOne(d => d.Consignor).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.ConsignorId)
                .HasConstraintName("FK_Vouchers_Consignor");
        });

        modelBuilder.Entity<VoucherCharge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VoucherC__3214EC073C6A03B6");

            entity.Property(e => e.CalculationBasis).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ChargeAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ChargeRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.ChargeType).WithMany(p => p.VoucherCharges)
                .HasForeignKey(d => d.ChargeTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherCharges_ChargeType");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherCharges)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherCharges_Voucher");
        });

        modelBuilder.Entity<VoucherLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VoucherL__3214EC0713852835");

            entity.HasIndex(e => e.ArticleId, "IX_VoucherLines_ArticleId");

            entity.HasIndex(e => e.VoucherId, "IX_VoucherLines_VoucherId");

            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.ConversionFactor)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.FulfilledQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Reference).HasMaxLength(255);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.ShippedQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TaxRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.WithholdingAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.WithholdingTaxCode).HasMaxLength(20);

            entity.HasOne(d => d.Article).WithMany(p => p.VoucherLines)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherLines_Article");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.VoucherLines)
                .HasForeignKey(d => d.StorageLocationId)
                .HasConstraintName("FK_VoucherLines_Location");

            entity.HasOne(d => d.Unit).WithMany(p => p.VoucherLines)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherLines_Unit");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherLines)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_VoucherLines_Voucher");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.VoucherLines)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherLines_Warehouse");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Warehous__3214EC0703741051");

            entity.HasIndex(e => e.WarehouseCode, "UQ__Warehous__1686A056B748EFAE").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.WarehouseCode).HasMaxLength(20);
            entity.Property(e => e.WarehouseName).HasMaxLength(100);
        });

        modelBuilder.Entity<WithholdingTaxRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Withhold__3214EC070D8D8732");

            entity.HasIndex(e => e.RuleCode, "UQ__Withhold__D618C1EE61CE6379").IsUnique();

            entity.Property(e => e.CalculationBase).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RuleCode).HasMaxLength(20);
            entity.Property(e => e.RuleName).HasMaxLength(100);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.ThresholdAmount).HasColumnType("decimal(18, 6)");
        });

        modelBuilder.Entity<WithholdingTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Withhold__3214EC0721D6083A");

            entity.Property(e => e.BaseAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.CertificateNumber).HasMaxLength(100);
            entity.Property(e => e.RemittanceReference).HasMaxLength(100);
            entity.Property(e => e.WithholdingAmount).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.WithholdingRate).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.TaxAuthority).WithMany(p => p.WithholdingTransactions)
                .HasForeignKey(d => d.TaxAuthorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WHT_Authority");

            entity.HasOne(d => d.Voucher).WithMany(p => p.WithholdingTransactions)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WHT_Voucher");

            entity.HasOne(d => d.WithholdingRule).WithMany(p => p.WithholdingTransactions)
                .HasForeignKey(d => d.WithholdingRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WHT_Rule");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
