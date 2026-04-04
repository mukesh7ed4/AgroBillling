import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { map, catchError, of, debounceTime, switchMap } from 'rxjs';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FormsModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent {
  private fb   = inject(FormBuilder);
  private cdr  = inject(ChangeDetectorRef);
  private http = inject(HttpClient);

  // ── State ───────────────────────────────────────────────────
  step: 'form' | 'otp' = 'form';

  // Form step
  submitting = false;
  error      = '';
  year       = new Date().getFullYear();

  // OTP step
  signupEmail = '';
  signupShopId = 0;
  otpValue    = '';
  otpError    = '';
  verifying   = false;
  resending   = false;
  resendMsg   = '';

  // Async validator for email (real-time)
  emailValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const email = control.value;
      // Skip if basic validation fails
      if (!email || !email.includes('@') || control.errors?.['required'] || control.errors?.['email']) {
        return of(null);
      }
      
      return of(control.value).pipe(
        debounceTime(800), // Slightly longer delay for email
        switchMap(value => 
          this.http.get<any>(`${environment.apiUrl}/auth/validate-email?email=${encodeURIComponent(value)}`)
        ),
        map(res => {
          if (res.isValid) {
            return null;
          }
          return { invalidEmail: res.message || 'Please enter a valid email address' };
        }),
        catchError(() => of(null))
      );
    };
  }

  // Async validator for mobile number (real-time)
  mobileValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const mobile = control.value;
      // Skip validation if not a valid mobile number format
      if (!mobile || mobile.length !== 10 || control.errors?.['required'] || control.errors?.['pattern']) {
        return of(null);
      }
      
      return of(control.value).pipe(
        debounceTime(500),
        switchMap(value => 
          this.http.get<any>(`${environment.apiUrl}/auth/check-mobile?mobile=${encodeURIComponent(value)}`)
        ),
        map(res => {
          if (!res.exists) {
            return null;
          }
          return { mobileExists: 'This mobile number is already registered' };
        }),
        catchError(() => of(null))
      );
    };
  }

  // ✅ NEW: Async validator for email existence (for submit-time check)
  emailExistsValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const email = control.value;
      if (!email || !email.includes('@') || control.errors?.['required'] || control.errors?.['email']) {
        return of(null);
      }
      
      return of(control.value).pipe(
        debounceTime(800),
        switchMap(value => 
          this.http.get<any>(`${environment.apiUrl}/auth/check-email?email=${encodeURIComponent(value)}`)
        ),
        map(res => {
          if (!res.exists) {
            return null;
          }
          return { emailExists: 'This email is already registered. Please login instead.' };
        }),
        catchError(() => of(null))
      );
    };
  }

  // Custom validator for password match
  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPass = control.get('confirmPass')?.value;
    return password === confirmPass ? null : { mismatch: true };
  }

  // Updated form with all validators
  form = this.fb.group({
    shopName:     ['', [Validators.required, Validators.minLength(3)]],
    ownerName:    ['', [Validators.required, Validators.minLength(3)]],
    mobileNumber: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)], [this.mobileValidator()]],
    email:        ['', [Validators.required, Validators.email], [this.emailValidator(), this.emailExistsValidator()]], // ✅ Added both validators
    city:         ['', Validators.required],
    state:        ['Haryana'],
    password:     ['', [Validators.required, Validators.minLength(6)]],
    confirmPass:  ['', Validators.required]
  }, { validators: this.passwordMatchValidator });

  constructor(
    public  theme: ThemeService,
    private auth:  AuthService,
    private router: Router
  ) {}

  // ── Step 1: Submit signup form ──────────────────────────────
  onSubmit(): void {
    if (this.form.invalid) { 
      this.form.markAllAsTouched(); 
      return; 
    }

    // Check if email is still validating
    if (this.form.get('email')?.status === 'PENDING') {
      this.error = 'Please wait, validating email...';
      return;
    }

    // Check if mobile is still validating
    if (this.form.get('mobileNumber')?.status === 'PENDING') {
      this.error = 'Please wait, validating mobile number...';
      return;
    }

    const { confirmPass, ...payload } = this.form.value;
    this.submitting = true;
    this.error      = '';

    this.auth.signup(payload).subscribe({
      next: (res: any) => {
        this.submitting  = false;
        
        if (res.success && res.data) {
          this.signupEmail = res.data.email;
          this.signupShopId = res.data.shopId;
          this.step = 'otp';
          this.otpValue = '';
          this.otpError = '';
          this.resendMsg = '';
        } else {
          this.error = res.message || 'Signup failed';
        }
        
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.submitting = false;
        this.error = err.error?.message || 'Signup failed. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Step 2: Verify OTP ──────────────────────────────────────
  verifyOtp(): void {
    if (!this.otpValue || this.otpValue.length !== 6) {
      this.otpError = 'Please enter 6-digit OTP';
      return;
    }
    
    this.verifying = true;
    this.otpError  = '';

    this.http.post<any>(`${environment.apiUrl}/auth/verify-otp`, {
      email: this.signupEmail,
      otp:   this.otpValue
    }).subscribe({
      next: (res: any) => {
        const d = res?.data;

        if (d?.token) {
          localStorage.setItem('access_token', d.token);
          localStorage.setItem('ab_role', d.role || 'SHOP');
          localStorage.setItem('ab_shop_id', d.shopId?.toString() ?? this.signupShopId.toString());
          localStorage.setItem('ab_shop_name', d.shopName ?? '');
          
          if (d.subscriptionStatus) {
            this.auth.setSubscriptionStatus(d.subscriptionStatus, d.subscriptionExpiry);
          } else {
            this.auth.setSubscriptionStatus('TRIAL');
          }
          
          this.router.navigate(['/shop/dashboard']);
        } else {
          this.verifying = false;
          this.otpError = res.message || 'OTP verification failed';
          this.cdr.detectChanges();
        }
      },
      error: (err: any) => {
        this.verifying = false;
        this.otpError = err.error?.message || 'Invalid OTP. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Resend OTP ──────────────────────────────────────────────
  resendOtp(): void {
    this.resending = true;
    this.resendMsg = '';
    this.otpError  = '';

    this.http.post<any>(`${environment.apiUrl}/auth/resend-otp`, {
      email: this.signupEmail
    }).subscribe({
      next: () => {
        this.resending = false;
        this.resendMsg = '✅ New OTP sent successfully!';
        this.otpValue = '';
        this.otpError = '';
        this.cdr.detectChanges();
        
        setTimeout(() => {
          this.resendMsg = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: (err: any) => {
        this.resending = false;
        this.otpError = err.error?.message || 'Could not resend OTP. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Go back to form ─────────────────────────────────────────
  goBackToForm(): void {
    this.step = 'form';
    this.otpValue = '';
    this.otpError = '';
    this.resendMsg = '';
  }

  // ── Helper to get form controls ─────────────────────────────
  get f() { return this.form.controls; }
}