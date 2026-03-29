import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss']
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr   = inject(ChangeDetectorRef);

  product: any = null;
  loading  = true;
  activeTab: 'overview' | 'purchases' | 'movements' = 'overview';

  constructor(private productService: ProductService, private auth: AuthService) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) { this.router.navigate(['/shop/inventory']); return; }
    this.load(id);
  }

  load(id: number): void {
    this.loading = true;
    this.productService.getProductDetail(id).subscribe({
      next: res => {
        const d = res?.data ?? res;
        this.product = {
          ...d,
          purchaseHistory: (d.purchaseOrderItems ?? [])
            .map((i: any) => ({
              date:          i.purchase?.purchaseDate,
              invoiceNumber: i.purchase?.invoiceNumber,
              supplier:      i.purchase?.supplier?.companyName ?? '—',
              qty:           i.quantity,
              unitPrice:     i.unitPrice,
              gstPercent:    i.gstpercent ?? i.gstPercent ?? 0,
              gstAmount:     i.gstamount  ?? i.gstAmount  ?? 0,
              total:         i.totalAmount,
              purchaseId:    i.purchase?.purchaseId
            }))
            .sort((a: any, b: any) => new Date(b.date).getTime() - new Date(a.date).getTime()),
          stockMovements: (d.stockMovements ?? [])
        };
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Computed ──────────────────────────────────────────────
  get totalPurchased(): number {
    return (this.product?.purchaseHistory ?? [])
      .reduce((s: number, i: any) => s + (+i.qty || 0), 0);
  }

  get totalSpent(): number {
    return (this.product?.purchaseHistory ?? [])
      .reduce((s: number, i: any) => s + (+i.total || 0), 0);
  }

  get avgPurchasePrice(): number {
    const h = this.product?.purchaseHistory ?? [];
    if (!h.length) return 0;
    return this.totalSpent / h.reduce((s: number, i: any) => s + (+i.qty || 0), 0);
  }

  get totalSoldQty(): number {
    return (this.product?.stockMovements ?? [])
      .filter((m: any) => m.movementType === 'SALE')
      .reduce((s: number, m: any) => s + Math.abs(+m.quantityChange || 0), 0);
  }

  get isLowStock(): boolean {
    return (this.product?.currentStock ?? 0) <= (this.product?.minStockAlert ?? 0);
  }

  stockBadge(): string {
    return this.isLowStock ? 'badge-danger' : 'badge-success';
  }

  movementIcon(type: string): string {
    const icons: Record<string, string> = {
      PURCHASE: '📦', SALE: '🧾', RETURN: '↩️',
      ADJUSTMENT: '⚙️', MANUAL: '✏️'
    };
    return icons[type] ?? '📌';
  }

  movementClass(m: any): string {
    return +m.quantityChange > 0 ? 'amount-paid' : 'amount-pending';
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(v ?? 0);
  }

  fmtQty(v: number | undefined, unit?: string): string {
    return `${(v ?? 0).toFixed(2).replace(/\.00$/, '')} ${unit ?? ''}`.trim();
  }
}