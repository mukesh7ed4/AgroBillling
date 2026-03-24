// ================================================
//  AgroBilling.DAL / Repositories / CreditNoteRepository.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBillling.DAL.Repositories
{
    public class CreditNoteRepository : GenericRepository<CreditNote>, ICreditNoteRepository
    {
        public CreditNoteRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<CreditNote>> GetByCustomerIdAsync(int customerId) =>
            await _context.CreditNotes
                .AsNoTracking()
                .Include(cn => cn.CreditNoteItems)
                .Where(cn => cn.CustomerId == customerId)
                .OrderByDescending(cn => cn.CreditNoteDate)
                .ToListAsync();
    }
}
