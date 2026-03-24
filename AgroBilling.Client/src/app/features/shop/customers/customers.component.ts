import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomerService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Customer } from '../../../core/models/models';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.scss']
})
export class CustomersComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  customers: Customer[] = [];
  loading = true;
  searchText = '';
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;

  constructor(private customerService: CustomerService, private auth: AuthService) {}

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
    this.customerService
      .getCustomers(shopId, this.searchText, this.pageNumber, this.pageSize)
      .subscribe({
        next: res => {
          this.customers = res.items;
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

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(v ?? 0);
  }
}
