// ================================================
//  AgroBilling.API / Controllers / ReportsController.cs
//  ✅ FIXED: Authorize(Roles="SHOP") → [Authorize]
//  Role claim mismatch se 403 aa raha tha
// ================================================

using AgroBilling.DAL.Context;
using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace AgroBilling.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]  // ✅ FIXED — "SHOP" hataya, role claim case mismatch tha
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _repo;
        private readonly AgroBillingDbContext _context;

        public ReportsController(IReportRepository repo, AgroBillingDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        [HttpGet("{shopId:int}/monthly")]
        public async Task<IActionResult> GetMonthly(
            int shopId,
            [FromQuery] int year = 0,
            [FromQuery] int month = 0)
        {
            // ✅ Manual role + shopId check
            var shopIdClaim = User.FindFirst("shopId")?.Value;
            var roleClaim = User.FindFirst("role")?.Value?.ToUpper();
            var isAdmin = roleClaim == "ADMIN";

            if (!isAdmin)
            {
                if (!int.TryParse(shopIdClaim, out var sid) || sid != shopId)
                    return Forbid();
            }

            if (year == 0) year = DateTime.Now.Year;
            if (month == 0) month = DateTime.Now.Month;

            var data = await _repo.GetMonthlyDashboardAsync(shopId, year, month);
            return Ok(ApiResponse<MonthlyDashboardDto>.Ok(data));
        }

        [HttpGet("{shopId:int}/export")]
        public async Task<IActionResult> ExportShopData(int shopId)
        {
            var shopIdClaim = User.FindFirst("shopId")?.Value;
            var roleClaim = User.FindFirst("role")?.Value?.ToUpper();
            var isAdmin = roleClaim == "ADMIN";

            if (!isAdmin)
            {
                if (!int.TryParse(shopIdClaim, out var sid) || sid != shopId)
                    return Forbid();
            }

            using var memStream = new MemoryStream();
            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.ShopId == shopId && c.IsActive)
                    .ToListAsync();

                AddCsvEntry(zip, "customers.csv",
                    "Name,Father Name,Mobile,Village,District,Opening Balance,Created",
                    customers.Select(c =>
                        $"{Csv(c.FullName)},{Csv(c.FatherName)},{Csv(c.MobileNumber)}," +
                        $"{Csv(c.Village)},{Csv(c.District)},{c.OpeningBalance}," +
                        $"{c.CreatedAt:dd-MM-yyyy}"));

                var bills = await _context.Bills
                    .AsNoTracking()
                    .Include(b => b.Customer)
                    .Where(b => b.ShopId == shopId && !b.IsReturn)
                    .OrderByDescending(b => b.BillDate)
                    .ToListAsync();

                AddCsvEntry(zip, "bills.csv",
                    "Bill No,Customer,Date,Total,Paid,Pending,Status",
                    bills.Select(b =>
                        $"{Csv(b.BillNumber)},{Csv(b.Customer?.FullName)}," +
                        $"{b.BillDate:dd-MM-yyyy},{b.TotalAmount},{b.AmountPaid}," +
                        $"{b.AmountPending ?? 0},{Csv(b.PaymentStatus)}"));

                var billItems = await _context.BillItems
                    .AsNoTracking()
                    .Include(i => i.Bill)
                    .Include(i => i.Product)
                    .Where(i => i.Bill.ShopId == shopId)
                    .ToListAsync();

                AddCsvEntry(zip, "bill_items.csv",
                    "Bill No,Product,Qty,Unit Price,GST%,Total",
                    billItems.Select(i =>
                        $"{Csv(i.Bill?.BillNumber)},{Csv(i.Product?.ProductName)}," +
                        $"{i.Quantity},{i.UnitPrice},{i.Gstpercent},{i.TotalAmount}"));

                var payments = await _context.BillPayments
                    .AsNoTracking()
                    .Include(p => p.Bill)
                    .Include(p => p.Customer)
                    .Where(p => p.ShopId == shopId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                AddCsvEntry(zip, "payments.csv",
                    "Bill No,Customer,Date,Amount,Mode,Reference",
                    payments.Select(p =>
                        $"{Csv(p.Bill?.BillNumber)},{Csv(p.Customer?.FullName)}," +
                        $"{p.PaymentDate:dd-MM-yyyy},{p.Amount}," +
                        $"{Csv(p.PaymentMode)},{Csv(p.Reference)}"));

                var products = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Unit)
                    .Where(p => p.ShopId == shopId && p.IsActive)
                    .ToListAsync();

                AddCsvEntry(zip, "products.csv",
                    "Product,Category,Company,Unit,Purchase Price,Selling Price,GST%,Stock",
                    products.Select(p =>
                        $"{Csv(p.ProductName)},{Csv(p.Category?.CategoryName)}," +
                        $"{Csv(p.CompanyName)},{Csv(p.Unit?.ShortName)}," +
                        $"{p.PurchasePrice},{p.SellingPrice},{p.Gstpercent},{p.CurrentStock}"));

                var purchases = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Include(p => p.Supplier)
                    .Where(p => p.ShopId == shopId)
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync();

                AddCsvEntry(zip, "purchases.csv",
                    "Invoice No,Supplier,Date,Total,Paid,Status",
                    purchases.Select(p =>
                        $"{Csv(p.InvoiceNumber)},{Csv(p.Supplier?.CompanyName)}," +
                        $"{p.PurchaseDate:dd-MM-yyyy},{p.NetPayable}," +
                        $"{p.AmountPaid},{Csv(p.PaymentStatus)}"));

                var summaryLines = new[]
                {
                    "AgroBilling — Data Export",
                    $"Export Date: {DateTime.Now:dd MMM yyyy HH:mm}",
                    "",
                    $"Total Customers : {customers.Count}",
                    $"Total Bills     : {bills.Count}",
                    $"Total Revenue   : {bills.Sum(b => b.TotalAmount):F2}",
                    $"Total Collected : {bills.Sum(b => b.AmountPaid):F2}",
                    $"Total Pending   : {bills.Sum(b => b.AmountPending ?? 0):F2}",
                    $"Total Products  : {products.Count}",
                    $"Total Purchases : {purchases.Count}",
                };

                var summaryEntry = zip.CreateEntry("summary.txt");
                using var sw = new StreamWriter(
                    summaryEntry.Open(), System.Text.Encoding.UTF8);
                foreach (var line in summaryLines) sw.WriteLine(line);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            var fileName = $"agrobilling-export-{DateTime.Now:yyyyMMdd}.zip";
            return File(memStream.ToArray(), "application/zip", fileName);
        }

        private static void AddCsvEntry(ZipArchive zip, string name,
            string header, IEnumerable<string> rows)
        {
            var entry = zip.CreateEntry(name);
            using var writer = new StreamWriter(
                entry.Open(), System.Text.Encoding.UTF8);
            writer.WriteLine(header);
            foreach (var row in rows) writer.WriteLine(row);
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\"", "\"\"");
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value}\"" : value;
        }
    }
}