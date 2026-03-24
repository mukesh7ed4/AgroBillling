import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { CustomerService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-add-customer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './add-customer.component.html',
  styleUrls: ['./add-customer.component.scss']
})
export class AddCustomerComponent {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  submitting = false;
  form = this.fb.group({
    fullName:       ['', Validators.required],
    fatherName:     [''],
    mobileNumber:   ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    alternateMobile:[''],
    village:        [''],
    tehsil:         [''],
    district:       [''],
    state:          ['Haryana'],
    landAcres:      [null],
    openingBalance: [0]
  });

  constructor(
    private customerService: CustomerService,
    private auth: AuthService,
    private router: Router
  ) {}

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    this.customerService.createCustomer(shopId, this.form.value as any).subscribe({
      next: res => {
        this.submitting = false;
        const id = res.data.customerId ?? res.data.id;
        this.router.navigate(['/shop/customers', id]);
        this.cdr.detectChanges();
      },
      error: () => {
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }
}