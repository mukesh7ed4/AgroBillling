import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { SupplierService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Supplier } from '../../../core/models/models';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './suppliers.component.html',
  styleUrls: ['./suppliers.component.scss']
})
export class SuppliersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  suppliers: Supplier[] = [];
  loading = true;
  showAddModal = false;
  submitting = false;

  form = this.fb.group({
    companyName:       ['', Validators.required],
    contactPersonName: [''],
    mobileNumber:      [''],
    email:             [''],
    address:           [''],
    gstNumber:         [''],   // ✅ FIXED
    openingBalance:    [0]
  });

  constructor(
    private supplierService: SupplierService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => this.load());
  }

  load(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) {
      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    this.loading = true;

    this.supplierService.getSuppliers(shopId).subscribe({
      next: res => {
        this.suppliers = res.data || []; // ✅ safety
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const shopId = this.auth.getShopId()!;

    this.supplierService.createSupplier(shopId, this.form.value as any).subscribe({
      next: () => {
        this.submitting = false;
        this.showAddModal = false;
        this.form.reset({ openingBalance: 0 });
        this.load();
      },
      error: () => {
        this.submitting = false;
      }
    });
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(v ?? 0);
  }
}