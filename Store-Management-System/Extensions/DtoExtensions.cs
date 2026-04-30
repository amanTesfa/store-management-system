using Store_Management_System.DTOs;
using Store_Management_System.Models;

namespace Store_Management_System.Extensions
{
    public static class DtoExtensions
    {
        public static ArticleDto ToDto(this Article article)
        {
            return new ArticleDto
            {
                Id = article.Id,
                ArticleCode = article.ArticleCode,
                ArticleName = article.ArticleName,
                ArticleType = article.ArticleType,
                ArticleCategory = article.ArticleCategory,
                CategoryName = article.ArticleCategoryNavigation?.Name,
                ArticleGroup = article.ArticleGroup,
                Description = article.Description,
                StandardCost = article.StandardCost,
                StandardPrice = article.StandardPrice,
                ReorderLevel = article.ReorderLevel,
                IsActive = article.IsActive,
                IsStockable = article.IsStockable,
                IsPurchasable = article.IsPurchasable,
                IsSellable = article.IsSellable,
                TaxRate = article.TaxRate,
                CreatedAt = article.CreatedAt,
                CreatedBy = article.CreatedBy?.ToString()
            };
        }

        public static BeginningBalanceDto ToDto(this BeginningBalance balance)
        {
            return new BeginningBalanceDto
            {
                Id = balance.Id,
                BalanceNumber = balance.BalanceNumber,
                FiscalPeriodId = balance.FiscalPeriodId,
                PeriodName = balance.FiscalPeriod?.PeriodName,
                WarehouseId = balance.WarehouseId,
                WarehouseName = balance.Warehouse?.WarehouseName,
                Description = balance.Description,
                Status = balance.Status,
                LineCount = balance.BeginningBalanceLines?.Count ?? 0,
                TotalQuantity = balance.BeginningBalanceLines?.Sum(l => l.Quantity) ?? 0,
                TotalValue = balance.BeginningBalanceLines?.Sum(l => l.Quantity * l.UnitCost) ?? 0,
                CreatedAt = balance.CreatedAt,
                CreatedBy = balance.CreatedBy.ToString()
            };
        }

        // RENAMED: For Purchase Order
        public static PurchaseOrderDto ToPurchaseOrderDto(this Voucher po)
        {
            return new PurchaseOrderDto
            {
                Id = po.Id,
                VoucherNumber = po.VoucherNumber,
                SupplierId = po.SupplierId ?? 0,
                SupplierName = po.Supplier?.Name,
                WarehouseId = po.WarehouseId,
                WarehouseName = po.Warehouse?.WarehouseName,
                VoucherDate = po.VoucherDate,
                ExpectedDate = po.ExpectedDate,
                SubTotal = po.SubTotal,
                TotalAmount = po.TotalAmount,
                Status = po.Status,
                Remarks = po.Remarks,
                LineCount = po.VoucherLines?.Count ?? 0,
                TotalQuantity = po.VoucherLines?.Sum(l => l.Quantity) ?? 0,
                CreatedAt = po.CreatedAt,
                CreatedBy = po.CreatedBy.ToString()
            };
        }

        // RENAMED: For Sales Order
        public static SalesOrderDto ToSalesOrderDto(this Voucher so)
        {
            return new SalesOrderDto
            {
                Id = so.Id,
                VoucherNumber = so.VoucherNumber,
                CustomerId = so.ConsigneeId ?? 0,
                CustomerName = so.Consignee?.ConsigneeName,
                WarehouseId = so.WarehouseId,
                WarehouseName = so.Warehouse?.WarehouseName,
                VoucherDate = so.VoucherDate,
                DeliveryDate = so.DeliveryDate,
                SubTotal = so.SubTotal,
                TotalAmount = so.TotalAmount,
                Status = so.Status,
                Remarks = so.Remarks,
                LineCount = so.VoucherLines?.Count ?? 0,
                TotalQuantity = so.VoucherLines?.Sum(l => l.Quantity) ?? 0,
                CreatedAt = so.CreatedAt,
                CreatedBy = so.CreatedBy.ToString()
            };
        }

        // RENAMED: For Invoice
        public static InvoiceDto ToInvoiceDto(this Voucher invoice)
        {
            return new InvoiceDto
            {
                Id = invoice.Id,
                VoucherNumber = invoice.VoucherNumber,
                CustomerId = invoice.ConsigneeId ?? 0,
                CustomerName = invoice.Consignee?.ConsigneeName,
                SalesOrderId = invoice.OriginalVoucherId,
                SalesOrderNumber = invoice.OriginalVoucher?.VoucherNumber,
                InvoiceDate = invoice.VoucherDate,
                DueDate = invoice.DueDate,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                LineCount = invoice.VoucherLines?.Count ?? 0,
                CreatedAt = invoice.CreatedAt,
                CreatedBy = invoice.CreatedBy.ToString()
            };
        }

        // For Goods Receipt
        public static GoodsReceiptDto ToGoodsReceiptDto(this Voucher grn)
        {
            return new GoodsReceiptDto
            {
                Id = grn.Id,
                VoucherNumber = grn.VoucherNumber,
                PurchaseOrderId = grn.OriginalVoucherId ?? 0,
                PurchaseOrderNumber = grn.OriginalVoucher?.VoucherNumber,
                SupplierId = grn.SupplierId ?? 0,
                SupplierName = grn.Supplier?.Name,
                ReceiptDate = grn.VoucherDate,
                TotalAmount = grn.TotalAmount,
                Status = grn.Status,
                LineCount = grn.VoucherLines?.Count ?? 0,
                TotalQuantity = grn.VoucherLines?.Sum(l => l.Quantity) ?? 0,
                CreatedAt = grn.CreatedAt,
                CreatedBy = grn.CreatedBy.ToString()
            };
        }

        // For Return to Supplier
        public static ReturnToSupplierDto ToReturnDto(this Voucher returnVoucher)
        {
            return new ReturnToSupplierDto
            {
                Id = returnVoucher.Id,
                VoucherNumber = returnVoucher.VoucherNumber,
                OriginalReceiptId = returnVoucher.OriginalVoucherId ?? 0,
                OriginalReceiptNumber = returnVoucher.OriginalVoucher?.VoucherNumber,
                SupplierId = returnVoucher.SupplierId ?? 0,
                SupplierName = returnVoucher.Supplier?.Name,
                ReturnDate = returnVoucher.VoucherDate,
                ReturnReason = returnVoucher.ReturnReason,
                TotalAmount = returnVoucher.TotalAmount,
                Status = returnVoucher.Status,
                LineCount = returnVoucher.VoucherLines?.Count ?? 0,
                TotalQuantity = returnVoucher.VoucherLines?.Sum(l => l.Quantity) ?? 0,
                CreatedAt = returnVoucher.CreatedAt,
                CreatedBy = returnVoucher.CreatedBy.ToString()
            };
        }
    }
}