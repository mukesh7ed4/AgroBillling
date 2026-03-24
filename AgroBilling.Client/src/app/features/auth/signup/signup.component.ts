import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
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
  private http = inject(HttpClient);
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

    this.http.post<any>(`${environment.apiUrl}/auth/signup`, payload).subscribe({
      next: res => {
        this.submitting = false;
        // Auto-login after signup
        this.auth.login({ email: payload.email!, password: payload.password! })
          .subscribe({
            next: () => this.router.navigate(['/shop/dashboard']),
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