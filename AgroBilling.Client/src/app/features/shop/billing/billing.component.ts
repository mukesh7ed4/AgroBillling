import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BillingService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Bill } from '../../../core/models/models';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './billing.component.html',
  styleUrl: './billing.component.scss'
})
export class BillingComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  bills: Bill[] = [];
  loading = true;
  searchText = '';
  filterStatus = '';
  pageNumber = 1;
  readonly pageSize = 10;
  totalCount = 0;

  constructor(
    private billingService: BillingService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => this.load());
  }

  load(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) {
      this.loading = false;
      this.cdr.detectChanges();
      return;
    }
    this.loading = true;
    const params: Record<string, unknown> = {
      page: this.pageNumber,
      pageSize: this.pageSize
    };
    if (this.searchText) params['search'] = this.searchText;
    if (this.filterStatus) params['status'] = this.filterStatus;
    this.billingService.getBills(shopId, params).subscribe({
      next: res => {
        this.bills = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.load();
  }

  onFilter(): void {
    this.pageNumber = 1;
    this.load();
  }

  clearSearch(): void {
    this.searchText = '';
    this.onSearch();
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.load();
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.load();
    }
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(v ?? 0);
  }

  statusClass(s: string | undefined): string {
    if (s === 'PAID') return 'badge-success';
    if (s === 'PARTIAL') return 'badge-warning';
    return 'badge-danger';
  }

  statusLabel(s: string | undefined): string {
    return s ?? '';
  }
}
