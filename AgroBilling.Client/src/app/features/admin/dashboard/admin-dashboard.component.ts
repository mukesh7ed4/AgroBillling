import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize, forkJoin, catchError, of } from 'rxjs';
import { ReportService } from '../../../core/services/api.services';
import { AdminDashboard } from '../../../core/models/models';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';

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
  pendingPaymentsCount = 0;

  private readonly cdr  = inject(ChangeDetectorRef);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {

    // ✅ 🔥 MOST IMPORTANT FIX — non-admin ko API hit hi nahi karne dena
    if (!this.auth.isAdmin()) {
      this.loading = false;
      return;
    }

    forkJoin({
      dashboard: this.reportService
        .getAdminDashboard()
        .pipe(catchError(() => of(null))),

      payments: this.http
        .get<any>(`${environment.apiUrl}/payments/pending`)
        .pipe(catchError(() => of(null)))
    })
    .pipe(finalize(() => {
      this.loading = false;
      this.cdr.detectChanges();
    }))
    .subscribe({
      next: ({ dashboard, payments }) => {
        const d = dashboard?.data;

        if (d) {
          this.data = {
            totalShops:           d.totalShops          ?? 0,
            activeSubscriptions:  d.activeSubscriptions ?? 0,
            allShops:     [...(d.allShops     ?? [])],
            expired:      [...(d.expired      ?? [])],
            expiringSoon: [...(d.expiringSoon ?? [])]
          };
        }

        this.pendingPaymentsCount = payments?.data?.length ?? 0;
      },
      error: () => {
        this.data = null;
        this.cdr.detectChanges();
      }
    });
  }

  get totalShops(): number { return this.data?.totalShops ?? 0; }
  get activeSubscriptions(): number { return this.data?.activeSubscriptions ?? 0; }
  get expiringSoonCount(): number { return this.data?.expiringSoon?.length ?? 0; }
  get expiredCount(): number { return this.data?.expired?.length ?? 0; }

  alertClass(days: number): string {
    if (days < 0) return 'badge-danger';
    if (days <= 3) return 'badge-danger';
    if (days <= 7) return 'badge-warning';
    return 'badge-success';
  }

  alertLabel(days: number): string {
    if (days < 0) return `Expired ${Math.abs(days)}d ago`;
    if (days === 0) return 'Expires Today!';
    return `${days} days left`;
  }
}