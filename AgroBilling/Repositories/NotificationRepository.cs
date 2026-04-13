// ================================================
//  AgroBilling.DAL / Repositories / NotificationRepository.cs
// ================================================

using AgroBilling.DAL.Context;
using AgroBilling.DAL.Models;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroBilling.DAL.Repositories
{
    public class NotificationRepository : GenericRepository<AdminNotification>, INotificationRepository
    {
        public NotificationRepository(AgroBillingDbContext context) : base(context) { }

        public async Task<IEnumerable<AdminNotification>> GetUnreadAsync() =>
            await _context.AdminNotifications
                .AsNoTracking()
                .Where(n => n.IsRead == false)
                .Include(n => n.Shop)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task MarkReadAsync(int notificationId)
        {
            var notif = await _context.AdminNotifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadAsync()
        {
            var unread = await _context.AdminNotifications
                .Where(n => n.IsRead == false)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }
}
