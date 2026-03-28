// src/app/features/shop/purchases/purchase-detail.component.ts

import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { PurchaseService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { PurchaseOrder, PurchaseOrderItem, SupplierPayment } from '../../../core/models/models';

@Component({
  selector: 'app-purchase-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './purchase-detail.component.html',
  styleUrls: ['./purchase-detail.component.scss']
})
export class PurchaseDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);

  purchase: any = null;
  loading = true;
  showPayModal = false;
  payLoading = false;
  activeTab: 'details' | 'items' | 'payments' = 'details'; // Tab state

  payForm = this.fb.group({
    amount: ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash', Validators.required],
    reference: [''],
    notes: [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  constructor(
    private purchaseService: PurchaseService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) {
      this.router.navigate(['/shop/purchases']);
      return;
    }
    this.load(id);
  }

  load(id: number): void {
    this.loading = true;
    this.cdr.detectChanges();
    
    this.purchaseService.getPurchaseById(id).subscribe({
      next: (response: any) => {
        console.log('Purchase detail response:', response);
        
        const d = response?.data ?? response;
        
        if (!d) {
          console.error('No data in response');
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }
        
        this.purchase = {
          ...d,
          purchaseOrderItems: d.purchaseOrderItems ?? [],
          items: d.purchaseOrderItems ?? d.items ?? [],
          supplierPayments: d.supplierPayments ?? [],
          payments: d.supplierPayments ?? d.payments ?? [],
          gstAmount: d.gstAmount ?? d.gstamount ?? 0,
          gstamount: d.gstamount ?? d.gstAmount ?? 0
        };
        
        console.log('Processed purchase:', this.purchase);
        
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load purchase:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get purchaseId(): number {
    return this.purchase?.purchaseId ?? this.purchase?.id ?? 0;
  }

  get totalGst(): number {
    const items = this.purchase?.items ?? this.purchase?.purchaseOrderItems ?? [];
    return items.reduce((s: number, i: any) => {
      const gstAmount = i.gstamount ?? i.gstAmount ?? 0;
      return s + (+gstAmount);
    }, 0);
  }

  get subTotal(): number {
    const items = this.purchase?.items ?? this.purchase?.purchaseOrderItems ?? [];
    return items.reduce((s: number, i: any) => {
      const qty = +(i.quantity ?? 0);
      const price = +(i.unitPrice ?? 0);
      return s + (qty * price);
    }, 0);
  }

  get amountDue(): number {
    const netPayable = +(this.purchase?.netPayable ?? 0);
    const amountPaid = +(this.purchase?.amountPaid ?? 0);
    return netPayable - amountPaid;
  }

  submitPayment(): void {
    if (this.payForm.invalid) return;
    this.payLoading = true;
    const v = this.payForm.getRawValue();
    const shopId = this.auth.getShopId();

    if (!shopId) {
      console.error('No shop ID found');
      this.payLoading = false;
      return;
    }

    const paymentData = {
      purchaseId: this.purchaseId,
      amount: +(v.amount ?? 0),
      paymentMode: v.paymentMode ?? 'Cash',
      reference: v.reference ?? '',
      notes: v.notes ?? '',
      paymentDate: v.paymentDate ?? new Date().toISOString().substring(0, 10)
    };

    this.purchaseService.addSupplierPayment(shopId, this.purchase?.supplierId, paymentData).subscribe({
      next: () => {
        this.payLoading = false;
        this.showPayModal = false;
        this.load(this.purchaseId);
      },
      error: (err) => {
        console.error('Payment failed:', err);
        this.payLoading = false;
      }
    });
  }

  fmt(v: number | undefined | null): string {
    const amount = v ?? 0;
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(amount);
  }

  statusClass(s: string | undefined): string {
    if (!s) return 'badge-danger';
    const upperStatus = s.toUpperCase();
    return upperStatus === 'PAID' ? 'badge-success' : 
           upperStatus === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }
}