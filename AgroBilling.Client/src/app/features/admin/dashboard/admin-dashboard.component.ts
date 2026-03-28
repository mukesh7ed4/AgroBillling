import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ReportService } from '../../../core/services/api.services';
import { AdminDashboard, ShopAlert } from '../../../core/models/models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  readonly Math = Math;
  data: AdminDashboard | null = null;
  loading = true;

  private readonly cdr = inject(ChangeDetectorRef);

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.reportService
      .getAdminDashboard()
      .pipe(finalize(() => { this.loading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: res => {
          const d = res?.data;
          if (d == null) { this.data = null; return; }
          this.data = {
            totalShops:           d.totalShops          ?? 0,
            activeSubscriptions:  d.activeSubscriptions ?? 0,
            allShops:     [...(d.allShops     ?? [])],
            expired:      [...(d.expired      ?? [])],
            expiringSoon: [...(d.expiringSoon ?? [])]
          };
        },
        error: () => { this.data = null; this.cdr.detectChanges(); }
      });
  }

  // ✅ Computed from allShops (more accurate than what API returns)
  get totalShops():          number { return this.data?.totalShops ?? 0; }
  get activeSubscriptions(): number { return this.data?.activeSubscriptions ?? 0; }
  get expiringSoonCount():   number { return this.data?.expiringSoon?.length ?? 0; }
  get expiredCount():        number { return this.data?.expired?.length ?? 0; }

  alertClass(days: number): string {
    if (days < 0)  return 'badge-danger';
    if (days <= 3) return 'badge-danger';
    if (days <= 7) return 'badge-warning';
    return 'badge-success';
  }

  alertLabel(days: number): string {
    if (days < 0)   return `Expired ${Math.abs(days)}d ago`;
    if (days === 0) return 'Expires Today!';
    return `${days} days left`;
  }
}