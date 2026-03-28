import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent {
  private fb   = inject(FormBuilder);
  private cdr  = inject(ChangeDetectorRef);

  submitting = false;
  error      = '';
  success    = false;
  year       = new Date().getFullYear();

  form = this.fb.group({
    shopName:     ['', Validators.required],
    ownerName:    ['', Validators.required],
    mobileNumber: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    email:        ['', [Validators.required, Validators.email]],
    city:         ['', Validators.required],
    state:        ['Haryana'],
    password:     ['', [Validators.required, Validators.minLength(6)]],
    confirmPass:  ['', Validators.required]
  }, { validators: this.passwordMatch });

  constructor(
    public theme: ThemeService,
    private auth: AuthService,
    private router: Router
  ) {}

  private passwordMatch(g: any) {
    const p  = g.get('password')?.value;
    const cp = g.get('confirmPass')?.value;
    return p === cp ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    const { confirmPass, ...payload } = this.form.value;
    this.submitting = true;
    this.error      = '';

    // ✅ Use auth.signup() directly — handles token + shopId from response
    this.auth.signup(payload).subscribe({
      next: (signupRes: any) => {
        this.submitting = false;

        // ✅ Signup response se subscriptionStatus save karo
        const signupData = signupRes?.data;
        if (signupData?.subscriptionStatus) {
          this.auth.setSubscriptionStatus(
            signupData.subscriptionStatus,
            signupData.subscriptionExpiry
          );
        }

        // ✅ Ab auto-login karo to get JWT token
        this.auth.login({ email: payload.email!, password: payload.password! })
          .subscribe({
            next: (loginRes: any) => {
              // ✅ Login se bhi status check karo (already set hoga, but double ensure)
              const loginData = loginRes?.data;
              if (loginData?.subscriptionStatus) {
                this.auth.setSubscriptionStatus(
                  loginData.subscriptionStatus,
                  loginData.subscriptionExpiry
                );
              } else if (!this.auth.getSubscriptionStatus()) {
                // ✅ Fallback — agar kuch nahi aaya toh TRIAL set karo
                this.auth.setSubscriptionStatus('TRIAL');
              }
              this.router.navigate(['/shop/dashboard']);
            },
            error: () => this.router.navigate(['/auth/login'])
          });

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.submitting = false;
        this.error = err.error?.message || 'Signup failed. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  get f() { return this.form.controls; }
}