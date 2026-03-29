import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { MonthlyDashboard, SalesSummary } from '../../../core/models/models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss']
})
export class ReportsComponent implements OnInit {
  dashboard: MonthlyDashboard | null = null;
  loading   = false;
  exporting = false;

  selectedMonth = new Date().toISOString().substring(0, 7);

  private readonly cdr = inject(ChangeDetectorRef);

  constructor(
    private reportService: ReportService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => this.load());
  }

  load(): void {
    const [year, month] = this.selectedMonth.split('-').map(Number);
    const shopId = this.auth.getShopId();
    if (shopId == null) {
      this.loading   = false;
      this.dashboard = null;
      this.cdr.detectChanges();
      return;
    }
    this.loading = true;
    this.reportService.getMonthlyDashboard(shopId, year, month).subscribe({
      next: res => {
        const d = res.data;
        this.dashboard = d ? { ...d, salesSummary: { ...(d.salesSummary ?? {}) } } : null;
        this.loading   = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ✅ Export all data as ZIP
  exportData(): void {
    const shopId = this.auth.getShopId();
    if (!shopId) return;
    this.exporting = true;

    this.reportService.exportShopData(shopId).subscribe({
      next: (blob: Blob) => {
        const url  = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href  = url;
        link.download = `agrobilling-${new Date().toISOString().slice(0, 10)}.zip`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        this.exporting = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exporting = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Computed ─────────────────────────────────────────────
  get netProfit(): number {
    if (!this.dashboard) return 0;
    const s = this.dashboard.salesSummary ?? {};
    return (s.totalCollected ?? 0)
         - (this.dashboard.totalExpenses   ?? 0)
         - (this.dashboard.paidToSuppliers ?? 0);
  }

  get sm(): SalesSummary {
    return this.dashboard?.salesSummary ?? {};
  }

  get supplierOutstanding(): number {
    const d = this.dashboard;
    if (!d) return 0;
    return (d.totalPurchased ?? 0) - (d.paidToSuppliers ?? 0);
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(v ?? 0);
  }

  monthLabel(m: string): string {
    const [y, mo] = m.split('-');
    return new Date(+y, +mo - 1).toLocaleString('en-IN', {
      month: 'long', year: 'numeric'
    });
  }
}