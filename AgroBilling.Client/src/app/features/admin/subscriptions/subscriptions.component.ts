import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/api.services';
import { ShopSummary } from '../../../core/models/models';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './subscriptions.component.html',
  styleUrls: ['./subscriptions.component.scss']
})
export class SubscriptionsComponent implements OnInit {
  shops: ShopSummary[] = [];
  loading = true;
  filterStatus = 'all';

  private readonly cdr = inject(ChangeDetectorRef);

  constructor(private adminService: AdminService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.adminService.getShops().subscribe({
      next: res => {
        this.shops = res.items as any;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filtered(): ShopSummary[] {
    if (this.filterStatus === 'expired')  return this.shops.filter(s => s.daysLeft < 0);
    if (this.filterStatus === 'expiring') return this.shops.filter(s => s.daysLeft >= 0 && s.daysLeft <= 7);
    if (this.filterStatus === 'active')   return this.shops.filter(s => s.daysLeft > 7);
    return this.shops;
  }

  alertClass(days: number): string {
    if (days < 0)  return 'badge-danger';
    if (days <= 7) return 'badge-warning';
    return 'badge-success';
  }

  alertLabel(days: number): string {
    if (days < 0)  return `Expired ${Math.abs(days)}d ago`;
    if (days === 0) return 'Expires Today';
    return `${days} days left`;
  }
}