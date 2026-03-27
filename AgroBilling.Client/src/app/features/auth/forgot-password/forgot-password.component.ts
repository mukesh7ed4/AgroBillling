import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
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

  step: 'email' | 'otp' | 'done' = 'email';
  loading = false;
  error   = '';
  year    = new Date().getFullYear();

  emailForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  otpForm = this.fb.group({
    otp:         ['', [Validators.required, Validators.minLength(6)]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPass: ['', Validators.required]
  });

  constructor(public theme: ThemeService) {}

  sendOtp(): void {
    if (this.emailForm.invalid) { this.emailForm.markAllAsTouched(); return; }
    this.loading = true; this.error = '';
    this.http.post(`${environment.apiUrl}/auth/forgot-password`, this.emailForm.value).subscribe({
      next: () => { this.loading = false; this.step = 'otp'; },
      error: (e: any) => { this.loading = false; this.error = e.error?.message || 'Failed. Try again.'; }
    });
  }

  resetPassword(): void {
    if (this.otpForm.invalid) { this.otpForm.markAllAsTouched(); return; }
    const { otp, newPassword, confirmPass } = this.otpForm.value;
    if (newPassword !== confirmPass) { this.error = 'Passwords do not match'; return; }
    this.loading = true; this.error = '';
    this.http.post(`${environment.apiUrl}/auth/reset-password`, {
      email: this.emailForm.value.email, otp, newPassword
    }).subscribe({
      next: () => { this.loading = false; this.step = 'done'; },
      error: (e: any) => { this.loading = false; this.error = e.error?.message || 'Invalid OTP.'; }
    });
  }
}
