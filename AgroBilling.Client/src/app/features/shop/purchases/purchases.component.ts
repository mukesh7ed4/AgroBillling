import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PurchaseService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { PurchaseOrder } from '../../../core/models/models';

@Component({
  selector: 'app-purchases',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './purchases.component.html',
  styleUrls: ['./purchases.component.scss']
})
export class PurchasesComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  purchases: PurchaseOrder[] = [];
  loading = true;
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;

  constructor(private purchaseService: PurchaseService, private auth: AuthService) {}
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
    this.purchaseService
      .getPurchases(shopId, { page: this.pageNumber, pageSize: this.pageSize })
      .subscribe({
        next: res => {
          this.purchases = res.items;
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
      maximumFractionDigits: 0
    }).format(v ?? 0);
  }
  statusClass(s: string | undefined): string {
    return s === 'PAID' ? 'badge-success' : s === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }

  purchasePending(p: PurchaseOrder): number {
    return (p.netPayable ?? 0) - (p.amountPaid ?? 0);
  }
}
