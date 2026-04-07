// ================================================
// Add this as a new file: VwCustomerPendingSummary.cs
// ================================================

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroBillling.DAL.Models
{
    [Table("VwCustomerPendingSummary")]
    public class VwCustomerPendingSummary
    {
        public int ShopId { get; set; }
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string? Village { get; set; }
        public string? District { get; set; }
        public int TotalBills { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public DateTime? LastBillDate { get; set; }
    }
}