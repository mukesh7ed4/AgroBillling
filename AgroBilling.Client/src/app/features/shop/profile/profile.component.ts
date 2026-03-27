import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent {
  private fb   = inject(FormBuilder);
  private http = inject(HttpClient);
  private cdr  = inject(ChangeDetectorRef);

  loading    = false;
  success    = '';
  error      = '';

  form = this.fb.group({
    currentPassword: ['', [Validators.required, Validators.minLength(6)]],
    newPassword:     ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  }, { validators: this.passwordMatch });

  constructor(public auth: AuthService) {}

  private passwordMatch(g: any) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true; this.error = ''; this.success = '';
    const shopId = this.auth.getShopId();
    this.http.post(`${environment.apiUrl}/auth/change-password`, {
      shopId,
      currentPassword: this.form.value.currentPassword,
      newPassword:     this.form.value.newPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.success = 'Password changed successfully!';
        this.form.reset();
        this.cdr.detectChanges();
      },
      error: (e: any) => {
        this.loading = false;
        this.error   = e.error?.message || 'Failed. Check current password.';
        this.cdr.detectChanges();
      }
    });
  }

  get f() { return this.form.controls; }
  get shopName(): string { return this.auth.getShopName() || 'Shop'; }
}
