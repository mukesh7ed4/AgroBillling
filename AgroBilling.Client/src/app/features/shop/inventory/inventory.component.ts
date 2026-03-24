import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Product } from '../../../core/models/models';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss']
})
export class InventoryComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  products: Product[] = [];
  loading = true;
  searchText = '';
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;

  constructor(private productService: ProductService, private auth: AuthService) {}
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
    this.productService
      .getProducts(shopId, {
        search: this.searchText,
        page: this.pageNumber,
        pageSize: this.pageSize
      })
      .subscribe({
        next: res => {
          this.products = res.items;
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

  isLowStock(p: Product): boolean {
    return (p.currentStock ?? 0) <= (p.minStockAlert ?? 0);
  }
  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(v ?? 0);
  }
}
