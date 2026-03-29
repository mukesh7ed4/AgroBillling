// src/app/features/shop/purchases/purchases.component.ts

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
  searchText = '';
  filterStatus = '';
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;

  constructor(
    private purchaseService: PurchaseService,
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
    
    this.purchaseService.getPurchases(shopId, params).subscribe({
      next: (response: any) => {
        // Handle response structure
        if (response?.items && Array.isArray(response.items)) {
          this.purchases = response.items;
          this.totalCount = response.totalCount ?? 0;
        } else if (Array.isArray(response)) {
          this.purchases = response;
          this.totalCount = response.length;
        } else {
          this.purchases = [];
          this.totalCount = 0;
        }
        
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load purchases:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  getSubTotal(purchase: PurchaseOrder): number {
    const items = (purchase as any).purchaseOrderItems ?? (purchase as any).items ?? [];
    return items.reduce((sum: number, item: any) => {
      const qty = +(item.quantity ?? 0);
      const price = +(item.unitPrice ?? 0);
      return sum + (qty * price);
    }, 0);
  }

  getGSTAmount(purchase: PurchaseOrder): number {
    const gstAmount = (purchase as any).gstAmount ?? 
                      (purchase as any).gstamount ?? 
                      0;
    return gstAmount;
  }

  purchasePending(purchase: PurchaseOrder): number {
    const netPayable = +(purchase.netPayable ?? 0);
    const amountPaid = +(purchase.amountPaid ?? 0);
    return netPayable - amountPaid;
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

  fmt(v: number | undefined | null): string {
    const amount = v ?? 0;
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(amount);
  }

  statusClass(status: string | undefined): string {
    if (!status) return 'badge-danger';
    const upperStatus = status.toUpperCase();
    return upperStatus === 'PAID' ? 'badge-success' : 
           upperStatus === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }
}