import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../core/services/api.services';
import { AdminDashboard, ShopAlert } from '../../../core/models/models';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './subscriptions.component.html',
  styleUrls: ['./subscriptions.component.scss']
})
export class SubscriptionsComponent implements OnInit {
  // ✅ Use allShops from AdminDashboard — has planName, startDate, endDate, daysLeft
  shops: ShopAlert[] = [];
  loading = true;
  filterStatus = 'all';

  private readonly cdr = inject(ChangeDetectorRef);

  constructor(private reportService: ReportService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    // ✅ Dashboard endpoint se data lo — wahan sab fields hain
    this.reportService.getAdminDashboard().subscribe({
      next: res => {
        const d = res?.data;
        // allShops mein TRIAL + ACTIVE + expired sab hain
        this.shops = d?.allShops ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filtered(): ShopAlert[] {
    if (this.filterStatus === 'expired')  return this.shops.filter(s => s.daysLeft < 0);
    if (this.filterStatus === 'expiring') return this.shops.filter(s => s.daysLeft >= 0 && s.daysLeft <= 7);
    if (this.filterStatus === 'active')   return this.shops.filter(s => s.daysLeft > 7);
    return this.shops;
  }

  get expiredCount():  number { return this.shops.filter(s => s.daysLeft < 0).length; }
  get expiringCount(): number { return this.shops.filter(s => s.daysLeft >= 0 && s.daysLeft <= 7).length; }
  get activeCount():   number { return this.shops.filter(s => s.daysLeft > 7).length; }

  alertClass(days: number): string {
    if (days < 0)  return 'badge-danger';
    if (days <= 7) return 'badge-warning';
    return 'badge-success';
  }

  alertLabel(days: number): string {
    if (days < 0)   return `Expired ${Math.abs(days)}d ago`;
    if (days === 0) return 'Expires Today';
    return `${days} days left`;
  }
}