import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { CustomerService, BillingService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { CustomerLedger } from '../../../core/models/models';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './customer-detail.component.html',
  styleUrls: ['./customer-detail.component.scss']
})
export class CustomerDetailComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb  = inject(FormBuilder);

  ledger:    CustomerLedger | null = null;
  loading    = true;
  customerId = 0;
  activeTab: 'bills' | 'payments' | 'credits' = 'bills';

  // ── Bulk Payment ──────────────────────────────────────────
  showBulkPayModal = false;
  bulkPayLoading   = false;
  bulkPayResult:   any = null;

  bulkPayForm = this.fb.group({
    amount:      ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash'],
    reference:   [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  constructor(
    private route:           ActivatedRoute,
    private customerService: CustomerService,
    private billingService:  BillingService,
    private auth:            AuthService
  ) {}

  ngOnInit(): void {
    this.customerId = +this.route.snapshot.params['id'];
    this.loadLedger();
  }

  loadLedger(): void {
    this.loading = true;
    this.customerService
      .getCustomerLedger(this.customerId, { billsTake: 50, paymentsTake: 100, creditsTake: 50 })
      .subscribe({
        next: res => {
          this.ledger  = res.data;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => { this.loading = false; this.cdr.detectChanges(); }
      });
  }

  // ── Bulk Payment ──────────────────────────────────────────
  openBulkPay(): void {
    this.bulkPayResult = null;
    this.bulkPayForm.reset({
      paymentMode: 'Cash',
      paymentDate: new Date().toISOString().substring(0, 10)
    });
    this.showBulkPayModal = true;
  }

  submitBulkPayment(): void {
    if (this.bulkPayForm.invalid) { this.bulkPayForm.markAllAsTouched(); return; }
    this.bulkPayLoading = true;
    const v = this.bulkPayForm.value;

    this.billingService.bulkPayment(this.auth.getShopId()!, {
      customerId:  this.customerId,
      amount:      +v.amount!,
      paymentMode: v.paymentMode ?? 'Cash',
      reference:   v.reference   || undefined,
      paymentDate: v.paymentDate
    }).subscribe({
      next: res => {
        this.bulkPayResult  = res.data;
        this.bulkPayLoading = false;
        this.loadLedger();   // refresh ledger after payment
        this.cdr.detectChanges();
      },
      error: () => { this.bulkPayLoading = false; this.cdr.detectChanges(); }
    });
  }

  closeBulkPay(): void {
    this.showBulkPayModal = false;
    this.bulkPayResult    = null;
  }

  // ── Computed ─────────────────────────────────────────────
  get totalBilled(): number {
    const l = this.ledger;
    if (l?.ledgerTotalBilled != null) return l.ledgerTotalBilled;
    return (l?.bills ?? []).reduce((s, b) => s + (b.totalAmount ?? 0), 0);
  }

  get totalPaidOnBills(): number {
    const l = this.ledger;
    if (l?.ledgerTotalPaidOnBills != null) return l.ledgerTotalPaidOnBills;
    return (l?.bills ?? []).reduce((s, b) => s + (b.amountPaid ?? 0), 0);
  }

  get totalPending(): number {
    const l = this.ledger;
    if (!l) return 0;
    const apiPending = l.customer?.totalPending;
    if (typeof apiPending === 'number' && Number.isFinite(apiPending)) return apiPending;
    const pending = this.totalBilled - this.totalPaidOnBills;
    return pending > 0 ? pending : 0;
  }

  get hasPendingBills(): boolean {
    return this.totalPending > 0;
  }

  get billsTabLabel(): string {
    const l = this.ledger; if (!l) return 'Bills';
    const n = l.billsTotalCount ?? l.bills?.length ?? 0;
    const shown = l.bills?.length ?? 0;
    return n > shown ? `Bills (${shown} of ${n})` : `Bills (${n})`;
  }

  get paymentsTabLabel(): string {
    const l = this.ledger; if (!l) return 'Payments';
    const n = l.paymentsTotalCount ?? l.payments?.length ?? 0;
    const shown = l.payments?.length ?? 0;
    return n > shown ? `Payments (${shown} of ${n})` : `Payments (${n})`;
  }

  get creditsTabLabel(): string {
    const l = this.ledger; if (!l) return 'Credit notes';
    const n = l.creditNotesTotalCount ?? l.creditNotes?.length ?? 0;
    const shown = l.creditNotes?.length ?? 0;
    return n > shown ? `Credit notes (${shown} of ${n})` : `Credit notes (${n})`;
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: 'INR', maximumFractionDigits: 0
    }).format(v ?? 0);
  }

  statusClass(s: string | undefined): string {
    return s === 'PAID' ? 'badge-success' : s === 'PARTIAL' ? 'badge-warning' : 'badge-danger';
  }
}