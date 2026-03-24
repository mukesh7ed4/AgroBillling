import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SupplierService, PurchaseService } from '../../../core/services/api.services';
import { SupplierLedger } from '../../../core/models/models';

@Component({
  selector: 'app-supplier-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './supplier-detail.component.html',
  styleUrls: ['./supplier-detail.component.scss']
})
export class SupplierDetailComponent implements OnInit {
  private readonly fb             = inject(FormBuilder);
  private readonly route          = inject(ActivatedRoute);
  private readonly supplierService = inject(SupplierService);
  private readonly purchaseService = inject(PurchaseService);
  private readonly cdr            = inject(ChangeDetectorRef);

  ledger: SupplierLedger | null = null;
  loading         = true;
  showPaymentModal = false;
  paymentLoading  = false;
  activeTab: 'purchases' | 'payments' = 'purchases';
  supplierId      = 0;

  paymentForm = this.fb.nonNullable.group({
    amount:      ['', [Validators.required, Validators.min(0.01)]],
    paymentMode: ['Cash', Validators.required],
    reference:   [''],
    paymentDate: [new Date().toISOString().substring(0, 10)]
  });

  ngOnInit(): void {
    this.supplierId = +this.route.snapshot.params['id'];
    this.load();
  }

  load(): void {
    this.loading = true;
    this.supplierService.getSupplierLedger(this.supplierId).subscribe({
      next: res => {
        this.ledger  = res.data;
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
    if (this.paymentForm.invalid || !this.ledger) return;
    this.paymentLoading = true;
    this.purchaseService
      .addSupplierPayment(
        this.ledger.supplier.shopId ?? 0,
        this.supplierId,
        this.paymentForm.value
      )
      .subscribe({
        next: () => {
          this.paymentLoading  = false;
          this.showPaymentModal = false;
          this.paymentForm.reset({
            paymentMode: 'Cash',
            paymentDate: new Date().toISOString().substring(0, 10)
          });
          this.load();
        },
        error: () => { this.paymentLoading = false; }
      });
  }

  /* ─── FALLBACK CALCULATORS ───────────────────────────────────
     Backend sometimes returns null/undefined for aggregated fields.
     These methods calculate totals from the purchases/payments arrays
     so stat cards always show correct values.
  ──────────────────────────────────────────────────────────── */

  /** Sum of netPayable across all purchase records */
  calcTotalPurchased(): number {
    return (this.ledger?.purchases ?? []).reduce(
      (sum, p) => sum + (p.netPayable ?? 0), 0
    );
  }

  /** Sum of amountPaid across all purchase records */
  calcTotalPaid(): number {
    return (this.ledger?.purchases ?? []).reduce(
      (sum, p) => sum + (p.amountPaid ?? 0), 0
    );
  }

  /** Total purchased minus total paid */
  calcOutstanding(): number {
    return this.calcTotalPurchased() - this.calcTotalPaid();
  }

  /* ─── HELPERS ─── */

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(v ?? 0);
  }

  statusClass(s: string | undefined): string {
    if (s === 'PAID')    return 'badge-success';
    if (s === 'PARTIAL') return 'badge-warning';
    return 'badge-danger';
  }
}