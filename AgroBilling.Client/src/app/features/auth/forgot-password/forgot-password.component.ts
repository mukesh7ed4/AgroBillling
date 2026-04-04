import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ThemeService } from '../../../core/services/theme.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  private fb   = inject(FormBuilder);
  private http = inject(HttpClient);
  private cdr  = inject(ChangeDetectorRef);

  step: 'email' | 'otp' | 'done' = 'email';
  loading = false;
  error   = '';
  successMsg = '';
  year    = new Date().getFullYear();
  
  userEmail = '';
  showNewPass = false;
  showConfirmPass = false;

  emailForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  otpForm = this.fb.group({
    otp:         ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPass: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  constructor(public theme: ThemeService) {}

  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('newPassword')?.value;
    const confirm = group.get('confirmPass')?.value;
    return password === confirm ? null : { mismatch: true };
  }

  sendOtp(): void {
    if (this.emailForm.invalid) { 
      this.emailForm.markAllAsTouched(); 
      return; 
    }
    
    this.loading = true; 
    this.error = '';
    this.successMsg = '';
    this.userEmail = this.emailForm.value.email!;
    
    console.log('Sending forgot password request to:', `${environment.apiUrl}/auth/forgot-password`);
    console.log('With email:', this.userEmail);
    
    this.http.post(`${environment.apiUrl}/auth/forgot-password`, { 
      email: this.userEmail 
    }).subscribe({
      next: (response: any) => {
        console.log('Forgot password response:', response);
        this.loading = false;
        
        // Check the exact message from backend
        if (response.message === 'OTP sent to your email.' && response.success === true) {
          // Email exists in database - proceed to OTP step
          this.successMsg = 'OTP sent successfully! Check your email.';
          this.step = 'otp';
          this.cdr.detectChanges();
          
          setTimeout(() => {
            this.successMsg = '';
            this.cdr.detectChanges();
          }, 3000);
        } 
        else if (response.message === 'If email exists, OTP has been sent.') {
          // Email does NOT exist in database - show error and stay on same step
          this.error = 'Email not found. Please register first.';
          this.cdr.detectChanges();
        }
        else {
          // Any other response
          this.error = response.message || 'Unable to send OTP. Please try again.';
          this.cdr.detectChanges();
        }
      },
      error: (error: HttpErrorResponse) => {
        console.error('Forgot password error:', error);
        this.loading = false;
        
        if (error.status === 404) {
          this.error = 'Email not found. Please register first.';
        } else if (error.status === 400) {
          this.error = error.error?.message || 'Email not registered. Please sign up first.';
        } else if (error.status === 429) {
          this.error = 'Too many requests. Please try again later.';
        } else {
          this.error = error.error?.message || 'Failed to send OTP. Please try again.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  resetPassword(): void {
    if (this.otpForm.invalid) { 
      this.otpForm.markAllAsTouched(); 
      return; 
    }
    
    const { otp, newPassword } = this.otpForm.value;
    
    this.loading = true; 
    this.error = '';
    
    this.http.post(`${environment.apiUrl}/auth/reset-password`, {
      email: this.userEmail, 
      otp, 
      newPassword
    }).subscribe({
      next: (response: any) => {
        console.log('Reset password response:', response);
        this.loading = false;
        this.step = 'done';
        this.cdr.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Reset password error:', error);
        this.loading = false;
        
        if (error.status === 400) {
          this.error = 'Invalid or expired OTP. Please request a new one.';
        } else {
          this.error = error.error?.message || 'Failed to reset password. Please try again.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  resendOtp(): void {
    this.loading = true;
    this.error = '';
    this.successMsg = '';
    
    this.http.post(`${environment.apiUrl}/auth/forgot-password`, { 
      email: this.userEmail 
    }).subscribe({
      next: (response: any) => {
        this.loading = false;
        
        if (response.message === 'OTP sent to your email.' && response.success === true) {
          this.successMsg = 'New OTP sent to your email!';
          this.cdr.detectChanges();
          
          setTimeout(() => {
            this.successMsg = '';
            this.cdr.detectChanges();
          }, 3000);
        } 
        else if (response.message === 'If email exists, OTP has been sent.') {
          this.error = 'Email not found. Please register first.';
          this.step = 'email';
          this.cdr.detectChanges();
        }
        else {
          this.error = 'Unable to resend OTP. Please try again.';
          this.cdr.detectChanges();
        }
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.error = error.error?.message || 'Failed to resend OTP. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  get email() { return this.emailForm.get('email'); }
  get otp() { return this.otpForm.get('otp'); }
  get newPassword() { return this.otpForm.get('newPassword'); }
  get confirmPass() { return this.otpForm.get('confirmPass'); }
}