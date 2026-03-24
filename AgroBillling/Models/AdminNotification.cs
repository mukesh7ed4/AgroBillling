using System;
using System.Collections.Generic;

namespace AgroBillling.DAL.Models;

public partial class AdminNotification
{
    public int NotificationId { get; set; }

    public int ShopId { get; set; }

    public string NotificationType { get; set; }

    public string Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shop Shop { get; set; }
}
