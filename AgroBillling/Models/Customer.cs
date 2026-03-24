using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int ShopId { get; set; }

    public string FullName { get; set; }

    public string FatherName { get; set; }

    public string MobileNumber { get; set; }

    public string AlternateMobile { get; set; }

    public string Village { get; set; }

    public string Tehsil { get; set; }

    public string District { get; set; }

    public string State { get; set; }

    public decimal? LandAcres { get; set; }

    public string AadhaarLast4 { get; set; }

    public decimal OpeningBalance { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual Shop Shop { get; set; }
}
