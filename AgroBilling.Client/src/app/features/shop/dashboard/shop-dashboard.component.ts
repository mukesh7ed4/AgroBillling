import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize, switchMap, take } from 'rxjs/operators';
import { of } from 'rxjs';
import { ReportService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { MonthlyDashboard, SalesSummary } from '../../../core/models/models';

@Component({
  selector: 'app-shop-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './shop-dashboard.component.html',
  styleUrls: ['./shop-dashboard.component.scss']
})
export class ShopDashboardComponent implements OnInit {

  private auth          = inject(AuthService);
  private reportService = inject(ReportService);
  private cdr           = inject(ChangeDetectorRef);

  dashboard: MonthlyDashboard | null = null;
  loading = false;

  currentMonth = new Date().getMonth() + 1;
  currentYear  = new Date().getFullYear();
  monthName    = new Date().toLocaleString('en-IN', { month: 'long', year: 'numeric' });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;

    this.auth.ensureShopId$()
      .pipe(
        take(1),
        switchMap(shopId => {
          if (!shopId) {
            return of(null);
          }

          return this.reportService.getMonthlyDashboard(
            shopId,
            this.currentYear,
            this.currentMonth
          );
        }),
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: res => {
          if (!res) {
            this.dashboard = null;
            return;
          }

          const d = (res as any)?.data ?? res ?? {};

          this.dashboard = {
            totalPurchased:   d.totalPurchased   ?? 0,
            paidToSuppliers:  d.paidToSuppliers  ?? 0,
            totalExpenses:    d.totalExpenses    ?? 0,
            salesSummary:     d.salesSummary     ?? {},
            topProducts:      d.topProducts      ?? [],
            expenseBreakdown: d.expenseBreakdown ?? []
          };

          this.cdr.detectChanges();
        },
        error: () => {
          this.dashboard = null;
          this.cdr.detectChanges();
        }
      });
  }

  refresh(): void {
    this.loadDashboard();
  }

  formatCurrency(val: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(val ?? 0);
  }

  get sm(): SalesSummary {
    return this.dashboard?.salesSummary ?? {};
  }

  get supplierOutstanding(): number {
    return (this.dashboard?.totalPurchased ?? 0) - (this.dashboard?.paidToSuppliers ?? 0);
  }
}