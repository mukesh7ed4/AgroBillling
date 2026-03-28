// src/app/features/shop/purchases/purchases.component.ts

import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PurchaseService } from '../../../core/services/api.services';
import { PurchaseOrder } from '../../../core/models/models';

@Component({
  selector: 'app-purchases',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './purchases.component.html',
  styleUrls: ['./purchases.component.scss']
})
export class PurchasesComponent implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  
  purchases: PurchaseOrder[] = [];
  loading = true;
  totalCount = 0;
  pageNumber = 1;
  pageSize = 20;
  totalPages = 1;

  constructor(private purchaseService: PurchaseService) {}

  ngOnInit(): void {
    this.loadPurchases();
  }

  loadPurchases(): void {
    this.loading = true;
    this.cdr.detectChanges(); // Force detect changes to show loading state
    
    this.purchaseService.getPurchases(this.pageNumber).subscribe({
      next: (response: any) => {
        console.log('Purchase response:', response); // Debug log
        
        // Handle the response based on your API structure
        if (response?.data) {
          // If response has data property
          if (Array.isArray(response.data)) {
            this.purchases = response.data;
            this.totalCount = response.data.length;
          } else if (response.data.items) {
            this.purchases = response.data.items;
            this.totalCount = response.data.totalCount || response.data.items.length;
            this.totalPages = response.data.totalPages || Math.ceil(this.totalCount / this.pageSize);
          } else {
            this.purchases = [];
            this.totalCount = 0;
          }
        } else if (Array.isArray(response)) {
          // If response is directly an array
          this.purchases = response;
          this.totalCount = response.length;
          this.totalPages = 1;
        } else if (response?.items && Array.isArray(response.items)) {
          // If response has items property (paged response)
          this.purchases = response.items;
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = response.totalPages ?? Math.ceil(this.totalCount / this.pageSize);
        } else {
          this.purchases = [];
          this.totalCount = 0;
          this.totalPages = 1;
        }
        
        // Ensure totalPages is at least 1
        this.totalPages = Math.max(1, this.totalPages);
        
        this.loading = false;
        this.cdr.detectChanges(); // Force detect changes after data update
      },
      error: (err) => {
        console.error('Failed to load purchases:', err);
        this.loading = false;
        this.purchases = [];
        this.totalCount = 0;
        this.cdr.detectChanges(); // Force detect changes on error
      }
    });
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

  fmt(value: number | undefined | null): string {
    const amount = value ?? 0;
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(amount);
  }

  statusClass(status: string | undefined): string {
    if (!status) return 'badge-danger';
    const upperStatus = status.toUpperCase();
    return upperStatus === 'PAID' ? 'badge-success' : 
           upperStatus === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadPurchases();
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.loadPurchases();
    }
  }
}