//using DevExpress.XtraReports.UI;
using Store_Management_System.Models;
using System.IO;

namespace Store_Management_System.Services
{
    public class PdfInvoiceService
    {
        public byte[] GenerateInvoice(Voucher invoice, Voucher salesOrder, Consignee customer, List<VoucherLine> lines)
        {
            // Create a simple HTML report (since DevExpress might need license)
            var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Invoice {invoice.VoucherNumber}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 40px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .company {{ font-size: 24px; font-weight: bold; }}
                    .invoice-title {{ font-size: 20px; margin-top: 20px; }}
                    .info-table {{ width: 100%; margin-bottom: 30px; }}
                    .info-table td {{ padding: 5px; }}
                    .items-table {{ width: 100%; border-collapse: collapse; margin-bottom: 30px; }}
                    .items-table th, .items-table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    .items-table th {{ background-color: #f2f2f2; }}
                    .total {{ text-align: right; font-size: 18px; font-weight: bold; }}
                    .footer {{ text-align: center; margin-top: 50px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='company'>Inventory Management System</div>
                    <div class='invoice-title'>TAX INVOICE</div>
                </div>
                
                <table class='info-table'>
                    <tr>
                        <td width='50%'>
                            <strong>Bill To:</strong><br />
                            {customer.ConsigneeName}<br />
                            {customer.BillingAddress}<br />
                            {customer.City}, {customer.State} {customer.PostalCode}<br />
                            Tax ID: {customer.TaxNumber}
                        </td>
                        <td width='50%'>
                            <strong>Invoice Details:</strong><br />
                            Invoice #: {invoice.VoucherNumber}<br />
                            Date: {invoice.VoucherDate:yyyy-MM-dd}<br />
                            Due Date: {(invoice.DueDate?.ToString("yyyy-MM-dd") ?? "N/A")}<br />
                            PO #: {salesOrder?.VoucherNumber}
                        </td>
                    </tr>
                </table>

                <table class='items-table'>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Description</th>
                            <th>Quantity</th>
                            <th>Unit Price</th>
                            <th>Total</th>
                        </tr>
                    </thead>
                    <tbody>";

            int i = 1;
            foreach (var line in lines)
            {
                html += $@"
                    <tr>
                        <td>{i++}</td>
                        <td>{line.Article?.ArticleName}</td>
                        <td>{line.Quantity:N2}</td>
                        <td>{line.UnitPrice:C}</td>
                        <td>{line.LineTotal:C}</td>
                    </tr>";
            }

            html += $@"
                    </tbody>
                </table>

                <div class='total'>
                    Subtotal: {invoice.SubTotal:C}<br />
                    Tax: {invoice.TaxAmount:C}<br />
                    <strong>Total: {invoice.TotalAmount:C}</strong>
                </div>

                <div class='footer'>
                    Thank you for your business!<br />
                    Payment is due within 30 days.
                </div>
            </body>
            </html>";

            // Convert HTML to PDF using a library or return HTML for now
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return bytes;
        }
    }
}