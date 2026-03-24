import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/api.services';
import { Shop, SubscriptionPlan } from '../../../core/models/models';

@Component({
  selector: 'app-shops',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './shops.component.html',
  styleUrls: ['./shops.component.scss']
})
export class ShopsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  shops: Shop[] = [];
  plans: SubscriptionPlan[] = [];
  loading = true;
  showAddModal = false;
  showExtendModal = false;
  selectedShop: Shop | null = null;
  submitting = false;
  searchText = '';
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;

  addForm = this.fb.group({
    ownerName:      ['', Validators.required],
    shopName:       ['', Validators.required],
    mobileNumber:   ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    email:          [''],
    address:        ['', Validators.required],
    city:           ['', Validators.required],
    state:          ['Haryana'],
    pinCode:        [''],
    gstNumber:      [''],
    gstPercent:     [18],
    billStartNumber:[1],
    password:       ['', [Validators.required, Validators.minLength(6)]],
    planId:         ['', Validators.required]
  });

  extendForm = this.fb.group({
    planId:      ['', Validators.required],
    amountPaid:  [0],
    paymentMode: ['Cash'],
    paymentRef:  [''],
    notes:       ['']
  });

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
    this.adminService.getPlans().subscribe(r => this.plans = r.data);
  }

  load(): void {
    this.loading = true;
    this.adminService
      .getShops({
        search: this.searchText,
        page: this.pageNumber,
        pageSize: this.pageSize
      })
      .subscribe({
        next: res => {
          this.shops = res.items;
          this.totalCount = res.totalCount;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.load();
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.load();
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.load();
    }
  }

  openExtend(shop: Shop): void {
    this.selectedShop = shop;
    this.extendForm.reset({ paymentMode: 'Cash', amountPaid: 0 });
    this.showExtendModal = true;
  }

  submitAdd(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.submitting = true;
    this.adminService.createShop(this.addForm.value as any).subscribe({
      next: () => { this.submitting = false; this.showAddModal = false; this.addForm.reset({ gstPercent: 18, billStartNumber: 1, state: 'Haryana' }); this.load(); },
      error: () => { this.submitting = false; }
    });
  }

  submitExtend(): void {
    if (this.extendForm.invalid || !this.selectedShop) { this.extendForm.markAllAsTouched(); return; }
    this.submitting = true;
    this.adminService
      .extendSubscription(this.selectedShop.shopId ?? this.selectedShop.id, this.extendForm.value)
      .subscribe({
      next: () => { this.submitting = false; this.showExtendModal = false; this.load(); },
      error: () => { this.submitting = false; }
    });
  }

  daysLeft(sub: any): number {
    if (!sub?.endDate) return 0;
    return Math.ceil((new Date(sub.endDate).getTime() - Date.now()) / 86400000);
  }

  alertClass(days: number): string {
    if (days < 0)  return 'badge-danger';
    if (days <= 7) return 'badge-warning';
    return 'badge-success';
  }
}