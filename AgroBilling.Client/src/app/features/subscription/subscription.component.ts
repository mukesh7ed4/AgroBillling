import { Component, inject, ChangeDetectorRef } from '@angular/core';
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
export class SubscriptionComponent {
  private fb   = inject(FormBuilder);
  private http = inject(HttpClient);
  private cdr  = inject(ChangeDetectorRef);

  step: 'plans' | 'payment' | 'submitted' = 'plans';
  selectedPlan: 'monthly' | 'yearly' = 'monthly';
  submitting = false;
  error      = '';

  readonly plans = {
    monthly: { label: 'Monthly',  price: 599,  duration: '1 Month',   saving: '' },
    yearly:  { label: 'Yearly',   price: 5999, duration: '12 Months', saving: 'Save ₹1,189 (2 months free)' }
  };

  readonly upiId  = 'yourname@upi'; // ← apna UPI ID daalo
  readonly upiQr  = 'assets/images/upi-qr.png'; // ← QR image path

  form = this.fb.group({
    transactionId: ['', [Validators.required, Validators.minLength(6)]],
    payerName:     ['', Validators.required],
    payerMobile:   ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]]
  });

  constructor(public auth: AuthService) {}

  selectPlan(plan: 'monthly' | 'yearly'): void {
    this.selectedPlan = plan;
    this.step = 'payment';
  }

  get currentPlan() { return this.plans[this.selectedPlan]; }

  copyUpi(): void {
    navigator.clipboard.writeText(this.upiId);
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.error      = '';

    const shopId = this.auth.getShopId();
    const payload = {
      shopId,
      planType:      this.selectedPlan,
      amount:        this.currentPlan.price,
      transactionId: this.form.value.transactionId,
      payerName:     this.form.value.payerName,
      payerMobile:   this.form.value.payerMobile
    };

    this.http.post<any>(`${environment.apiUrl}/payments/request`, payload).subscribe({
      next: () => {
        this.step      = 'submitted';
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