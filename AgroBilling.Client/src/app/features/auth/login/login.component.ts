import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  form!: FormGroup;
  loading  = false;
  error    = '';
  showPass = false;
  year     = new Date().getFullYear();

  constructor(
    private fb:     FormBuilder,
    private auth:   AuthService,
    private router: Router,
    public  theme:  ThemeService
  ) {}

  ngOnInit(): void {
    if (this.auth.hasValidToken()) { this.redirectByRole(); return; }
    this.form = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.error   = '';

    // ✅ Role nahi bhej rahe — backend khud detect karega
    this.auth.login(this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.redirectByRole();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.loading = false;
        this.error   = err.error?.message || 'Invalid credentials. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  private redirectByRole(): void {
    if (this.auth.isAdmin()) this.router.navigate(['/admin/dashboard']);
    else this.router.navigate(['/shop/dashboard']);
  }

  get email()    { return this.form.get('email'); }
  get password() { return this.form.get('password'); }
}