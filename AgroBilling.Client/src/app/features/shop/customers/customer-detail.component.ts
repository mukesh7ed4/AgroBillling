import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { CustomerService } from '../../../core/services/api.services';
import { CustomerLedger } from '../../../core/models/models';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './customer-detail.component.html',
  styleUrls: ['./customer-detail.component.scss']
})
export class CustomerDetailComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  ledger: CustomerLedger | null = null;
  loading = true;
  activeTab: 'bills' | 'payments' | 'credits' = 'bills';

  constructor(private route: ActivatedRoute, private customerService: CustomerService) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    this.customerService
      .getCustomerLedger(id, { billsTake: 50, paymentsTake: 100, creditsTake: 50 })
      .subscribe({
        next: res => {
          this.ledger = res.data;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
  }

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

    // Prefer API-provided totalPending if present.
    const apiPending = l.customer?.totalPending;
    if (typeof apiPending === 'number' && Number.isFinite(apiPending)) return apiPending;

    // Fallback: pending = billed - paid (covers cases where backend doesn't populate totalPending).
    const pending = this.totalBilled - this.totalPaidOnBills;
    // Avoid showing tiny negative due to rounding.
    return pending > 0 ? pending : 0;
  }

  get billsTabLabel(): string {
    const l = this.ledger;
    if (!l) return 'Bills';
    const n = l.billsTotalCount ?? l.bills?.length ?? 0;
    const shown = l.bills?.length ?? 0;
    if (n > shown) return `Bills (${shown} of ${n})`;
    return `Bills (${n})`;
  }

  get paymentsTabLabel(): string {
    const l = this.ledger;
    if (!l) return 'Payments';
    const n = l.paymentsTotalCount ?? l.payments?.length ?? 0;
    const shown = l.payments?.length ?? 0;
    if (n > shown) return `Payments (${shown} of ${n})`;
    return `Payments (${n})`;
  }

  get creditsTabLabel(): string {
    const l = this.ledger;
    if (!l) return 'Credit notes';
    const n = l.creditNotesTotalCount ?? l.creditNotes?.length ?? 0;
    const shown = l.creditNotes?.length ?? 0;
    if (n > shown) return `Credit notes (${shown} of ${n})`;
    return `Credit notes (${n})`;
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
}