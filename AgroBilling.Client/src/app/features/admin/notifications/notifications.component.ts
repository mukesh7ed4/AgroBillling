import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/api.services';
import { Notification } from '../../../core/models/models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit {
  notifications: Notification[] = [];
  loading = true;

  private readonly cdr = inject(ChangeDetectorRef);

  constructor(private adminService: AdminService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.adminService.getNotifications().subscribe({
      next: res => {
        this.notifications = res.data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  markRead(id: number): void {
    this.adminService.markNotificationRead(id).subscribe(() => {
      const n = this.notifications.find(x => (x.notificationId ?? x.id) === id);
      if (n) {
        n.isRead = true;
        n.read = true;
      }
    });
  }

  notifId(n: Notification): number {
    return n.notificationId ?? n.id;
  }

  isUnread(n: Notification): boolean {
    return !(n.isRead ?? n.read);
  }

  markAllRead(): void {
    this.notifications.filter(n => this.isUnread(n)).forEach(n => this.markRead(this.notifId(n)));
  }

  typeIcon(type: string): string {
    if (type === 'EXPIRY_WARNING') return '⚠️';
    if (type === 'EXPIRED')        return '❌';
    if (type === 'NEW_SIGNUP')     return '🎉';
    return '🔔';
  }

  typeClass(type: string): string {
    if (type === 'EXPIRY_WARNING') return 'badge-warning';
    if (type === 'EXPIRED')        return 'badge-danger';
    if (type === 'NEW_SIGNUP')     return 'badge-success';
    return 'badge-muted';
  }

  get unreadCount(): number {
    return this.notifications.filter(n => this.isUnread(n)).length;
  }
}