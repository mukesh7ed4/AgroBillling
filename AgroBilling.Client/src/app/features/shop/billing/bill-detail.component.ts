import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { BillingService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-bill-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './bill-detail.component.html',
  styleUrls: ['./bill-detail.component.scss']
})
export class BillDetailComponent implements OnInit {
  private readonly fb             = inject(FormBuilder);
  private readonly route          = inject(ActivatedRoute);
  private readonly router         = inject(Router);
  private readonly billingService = inject(BillingService);
  readonly auth                   = inject(AuthService);  // public for template
  private readonly cdr            = inject(ChangeDetectorRef);

  bill: any = null;
  loading   = true;
  activeTab: 'details' | 'items' | 'payments' = 'details';

  showPaymentModal = false;
  showReturnModal  = false;
  paymentLoading   = false;
  returnItems: any[] = [];

  paymentForm = this.fb.group({
    amount:      ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash', Validators.required],
    reference:   [''],
    notes:       [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  get billId(): number { return this.bill?.billId ?? this.bill?.id ?? 0; }

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) { this.router.navigate(['/shop/billing']); return; }
    this.loadBill(id);
  }

  loadBill(id: number): void {
    this.loading = true;
    this.billingService.getBillById(id).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.bill = {
          ...data,
          items:    data.billItems    ?? data.items    ?? [],
          payments: data.billPayments ?? data.payments ?? [],
          customer: data.customer ?? null,
          shop:     data.shop     ?? null   // ✅ shop details from backend
        };
        this.returnItems = (this.bill.items || []).map((i: any) => ({
          item: i, returnQty: 0, selected: false
        }));
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  // ✅ Print — triggers browser print with styled layout
  printBill(): void { window.print(); }

  addPayment(): void {
    if (this.paymentForm.invalid || !this.bill) return;
    this.paymentLoading = true;
    const v = this.paymentForm.getRawValue();
    this.billingService.addPayment({
      billId:      this.billId,
      amount:      parseFloat(v.amount ?? '0'),
      paymentMode: v.paymentMode ?? 'Cash',
      reference:   v.reference   || undefined,
      notes:       v.notes       || undefined,
      paymentDate: v.paymentDate || new Date().toISOString().substring(0, 10)
    }).subscribe({
      next: () => { this.paymentLoading = false; this.showPaymentModal = false; this.loadBill(this.billId); },
      error: () => { this.paymentLoading = false; }
    });
  }

  submitReturn(): void {
    if (!this.bill) return;
    const selectedItems = this.returnItems
      .filter(r => r.selected && r.returnQty > 0)
      .map(r => ({
        productId:   r.item.productId,
        productName: r.item.productName,
        quantity:    r.returnQty,
        unitPrice:   r.item.unitPrice,
        totalAmount: +(r.returnQty * (r.item.unitPrice ?? 0)).toFixed(2)
      }));
    if (!selectedItems.length) return;
    this.billingService.processReturn(this.bill.shopId, {
      customerId:     this.bill.customerId,
      originalBillId: this.billId,
      returnDate:     new Date().toISOString().substring(0, 10),
      items:          selectedItems
    }).subscribe({
      next: () => { this.showReturnModal = false; this.loadBill(this.billId); }
    });
  }

  // Computed
  get subTotal(): number {
    return (this.bill?.items ?? []).reduce((s: number, i: any) => {
      return s + (+i.quantity || 0) * (+i.unitPrice || 0);
    }, 0);
  }
  get totalGst():     number { return +(this.bill?.gstamount ?? this.bill?.gstAmount ?? 0); }
  get totalDiscount():number { return +(this.bill?.discountAmount ?? 0); }
  get canAddPayment():boolean { return this.bill && this.bill.paymentStatus !== 'PAID'; }
  get maxPayable():   number  { return this.bill ? (this.bill.amountPending ?? 0) : 0; }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(v ?? 0);
  }
  statusClass(s: string): string {
    return s === 'PAID' ? 'badge-success' : s === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }
}