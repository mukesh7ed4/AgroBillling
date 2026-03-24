import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { BillingService } from '../../../core/services/api.services';
import { Bill, BillItem } from '../../../core/models/models';

@Component({
  selector: 'app-bill-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './bill-detail.component.html',
  styleUrls: ['./bill-detail.component.scss']
})
export class BillDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly billingService = inject(BillingService);
  private readonly cdr = inject(ChangeDetectorRef);

  bill: Bill | null = null;
  loading = true;
  showPaymentModal = false;
  showReturnModal = false;
  paymentLoading = false;
  returnItems: { item: BillItem; returnQty: number; selected: boolean }[] = [];

  paymentForm = this.fb.nonNullable.group({
    amount:      ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash', Validators.required],
    reference:   [''],
    notes:       [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    this.billingService.getBillById(id).subscribe({
      next: res => {
        this.bill = res.data;
        this.returnItems = (res.data.items || []).map(i => ({ item: i, returnQty: 0, selected: false }));
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  addPayment(): void {
    if (this.paymentForm.invalid || !this.bill) return;
    this.paymentLoading = true;
    const v = this.paymentForm.getRawValue();
    this.billingService
      .addPayment({
        billId: this.bill.billId ?? this.bill.id!,
        amount: Number(v.amount),
        paymentMode: v.paymentMode ?? undefined,
        reference: v.reference ?? undefined,
        notes: v.notes ?? undefined,
        paymentDate: v.paymentDate ?? undefined
      })
      .subscribe({
      next: () => {
        this.paymentLoading = false;
        this.showPaymentModal = false;
        const id = +this.route.snapshot.params['id'];
        this.billingService.invalidateBillDetailCache(id);
        this.ngOnInit();
        this.cdr.detectChanges();
      },
      error: () => {
        this.paymentLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  submitReturn(): void {
    if (!this.bill) return;
    const selectedItems = this.returnItems
      .filter(r => r.selected && r.returnQty > 0)
      .map(r => ({
        productId: r.item.productId,
        productName: r.item.productName,
        quantity: r.returnQty,
        unitPrice: r.item.unitPrice,
        totalAmount: +(r.returnQty * (r.item.unitPrice ?? 0)).toFixed(2)
      }));
    if (!selectedItems.length) return;
    this.billingService.processReturn(this.bill.shopId!, {
      customerId: this.bill.customerId,
      originalBillId: this.bill.billId ?? this.bill.id,
      returnDate: new Date().toISOString().substring(0, 10),
      items: selectedItems
    }).subscribe({
      next: () => {
        this.showReturnModal = false;
        const id = +this.route.snapshot.params['id'];
        this.billingService.invalidateBillDetailCache(id);
        this.ngOnInit();
        this.cdr.detectChanges();
      },
      error: () => {
        this.cdr.detectChanges();
      }
    });
  }

  get canAddPayment(): boolean { return !!this.bill && this.bill.paymentStatus !== 'PAID'; }
  get maxPayable(): number { return this.bill ? (this.bill.amountPending ?? 0) : 0; }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 }).format(
      v ?? 0
    );
  }

  statusClass(s: string | undefined): string {
    return s === 'PAID' ? 'badge-success' : s === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }

  printBill(): void { window.print(); }
}