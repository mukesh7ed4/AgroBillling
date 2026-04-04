// ================================================
//  src/app/features/subscription/subscription.component.ts
//  REPLACE existing file completely
// ================================================

import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './subscription.component.html',
  styleUrls: ['./subscription.component.scss']
})
export class SubscriptionComponent implements OnInit {
  private fb   = inject(FormBuilder);
  private http = inject(HttpClient);
  private cdr  = inject(ChangeDetectorRef);

  step: 'plans' | 'payment' | 'submitted' = 'plans';
  selectedPlan: 'monthly' | 'yearly' = 'monthly';
  submitting     = false;
  checkingStatus = true;
  error          = '';

  // ✅ Existing pending/rejected request info
  existingRequest: { transactionId: string; requestedAt: string; status: string; adminNotes?: string } | null = null;

  // TODO: Apna UPI ID aur QR image path daalo
  readonly upiId = 'yourname@upi';
  readonly upiQr = 'assets/images/upi-qr.png';

  readonly plans = {
    monthly: { label: 'Monthly', price: 299,  duration: '1 Month',   planType: 'monthly' },
    yearly:  { label: 'Yearly',  price: 2999, duration: '12 Months', planType: 'yearly'  }
  };

  form = this.fb.group({
    transactionId: ['', [Validators.required, Validators.minLength(6)]],
    payerName:     ['', Validators.required],
    payerMobile:   ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]]
  });

  constructor(public auth: AuthService) {}

  ngOnInit(): void {
    this.checkPendingRequest();
  }

  checkPendingRequest(): void {
    this.checkingStatus = true;

    this.http.get<any>(`${environment.apiUrl}/payments/shop-status`).subscribe({
      next: res => {
        const d = res?.data;

        if (d?.status === 'PENDING') {
          this.existingRequest = {
            transactionId: d.transactionId,
            requestedAt:   d.submittedAt,
            status:        'PENDING'
          };
          this.step = 'submitted';
        } else if (d?.status === 'REJECTED') {
          // Rejected — plans page pe jaao, error show karo
          this.existingRequest = null;
          this.error = `Aapki last request reject hui thi. Reason: ${d.adminNotes || 'Invalid transaction'}. Dobara submit karo.`;
          this.step = 'plans';
        }
        // APPROVED ya kuch nahi — normal plans page

        this.checkingStatus = false;
        this.cdr.detectChanges();
      },
      error: () => {
        // API fail — silently ignore, plans page dikhao
        this.checkingStatus = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectPlan(plan: 'monthly' | 'yearly'): void {
    this.selectedPlan = plan;
    this.error = '';
    this.step  = 'payment';
  }

  // ✅ HTML mein goBackToPlans() use ho raha hai
  goBackToPlans(): void {
    this.step  = 'plans';
    this.error = '';
  }

  get currentPlan() { return this.plans[this.selectedPlan]; }

  copyUpi(): void {
    navigator.clipboard.writeText(this.upiId).catch(() => {});
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.error      = '';

    const shopId  = this.auth.getShopId();
    const payload = {
      shopId,
      planType:      this.currentPlan.planType,
      amount:        this.currentPlan.price,
      transactionId: this.form.value.transactionId,
      payerName:     this.form.value.payerName,
      payerMobile:   this.form.value.payerMobile
    };

    this.http.post<any>(`${environment.apiUrl}/payments/request`, payload).subscribe({
      next: () => {
        this.auth.setPendingPaymentStatus();
        this.existingRequest = null; // fresh submission
        this.step       = 'submitted';
        this.submitting = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.error      = err.error?.message || 'Failed. Please try again.';
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  logout(): void { this.auth.logout(); }
}