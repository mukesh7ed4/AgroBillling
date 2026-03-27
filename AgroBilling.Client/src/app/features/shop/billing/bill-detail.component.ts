import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { BillingService } from '../../../core/services/api.services';

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
  private readonly router = inject(Router);
  private readonly billingService = inject(BillingService);
  private readonly cdr = inject(ChangeDetectorRef);

  bill: any = null;
  loading = true;

  showPaymentModal = false;
  showReturnModal = false;
  paymentLoading = false;

  returnItems: any[] = [];

  paymentForm = this.fb.nonNullable.group({
    amount: ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash', Validators.required],
    reference: [''],
    notes: [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  // ✅ Normalize BillId (VERY IMPORTANT)
  get billId(): number {
    return this.bill?.billId ?? this.bill?.id ?? 0;
  }

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];

    if (!id) {
      this.router.navigate(['/']); // prevent crash
      return;
    }

    this.loadBill(id);
  }

  // ✅ LOAD BILL (MAIN FIX HERE)
  loadBill(id: number) {
    this.loading = true;

    this.billingService.getBillById(id).subscribe({
      next: (res: any) => {

        console.log('🔥 FULL API RESPONSE:', res);

        const data = res.data;

        // ✅ NORMALIZE RESPONSE
        this.bill = {
          ...data,

          // 🔥 FIX: map backend → frontend
          items: data.billItems ?? [],
          payments: data.billPayments ?? [],

          // optional safety
          customer: data.customer ?? null
        };

        console.log('✅ NORMALIZED BILL:', this.bill);

        // ✅ Prepare return items
        this.returnItems = (this.bill.items || []).map((i: any) => ({
          item: i,
          returnQty: 0,
          selected: false
        }));

        this.loading = false;
        this.cdr.detectChanges();
      },

      error: (err) => {
        console.error('❌ ERROR LOADING BILL:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ✅ ADD PAYMENT
  addPayment(): void {
    if (this.paymentForm.invalid || !this.bill) return;

    this.paymentLoading = true;
    const v = this.paymentForm.getRawValue();

    this.billingService.addPayment({
      billId: this.billId,
      amount: Number(v.amount),
      paymentMode: v.paymentMode,
      reference: v.reference,
      notes: v.notes,
      paymentDate: v.paymentDate
    }).subscribe({
      next: () => {
        this.paymentLoading = false;
        this.showPaymentModal = false;

        this.loadBill(this.billId); // 🔥 reload properly
      },
      error: () => {
        this.paymentLoading = false;
      }
    });
  }

  // ✅ RETURN PROCESS
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

    this.billingService.processReturn(this.bill.shopId, {
      customerId: this.bill.customerId,
      originalBillId: this.billId,
      returnDate: new Date().toISOString().substring(0, 10),
      items: selectedItems
    }).subscribe({
      next: () => {
        this.showReturnModal = false;
        this.loadBill(this.billId);
      }
    });
  }

  // ✅ HELPERS
  get canAddPayment(): boolean {
    return this.bill && this.bill.paymentStatus !== 'PAID';
  }

  get maxPayable(): number {
    return this.bill ? (this.bill.amountPending ?? 0) : 0;
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(v ?? 0);
  }

  statusClass(s: string): string {
    return s === 'PAID'
      ? 'badge-success'
      : s === 'PARTIAL'
        ? 'badge-warning'
        : 'badge-danger';
  }

  printBill(): void {
    window.print();
  }
}