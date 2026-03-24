import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PurchaseService, SupplierService, ProductService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Supplier, Product } from '../../../core/models/models';

@Component({
  selector: 'app-create-purchase',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './create-purchase.component.html',
  styleUrls: ['./create-purchase.component.scss']
})
export class CreatePurchaseComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  suppliers: Supplier[] = [];
  filteredProducts: Product[] = [];
  productSearch = '';
  showProductDrop = false;
  submitting = false;
  private productSearchTimer?: ReturnType<typeof setTimeout>;

  form = this.fb.group({
    supplierId:    ['', Validators.required],
    purchaseDate:  [new Date().toISOString().substring(0,10), Validators.required],
    invoiceNumber: [''],
    amountPaid:    [0],
    paymentMode:   ['Cash'],
    notes:         [''],
    items:         this.fb.array([])
  });

  constructor(
    private purchaseService: PurchaseService,
    private supplierService: SupplierService,
    private productService: ProductService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnDestroy(): void {
    clearTimeout(this.productSearchTimer);
  }

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => this.loadInitialData());
  }

  private loadInitialData(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) return;
    this.supplierService.getSuppliers(shopId).subscribe(r => (this.suppliers = r.data));
    this.productService.getProducts(shopId, { page: 1, pageSize: 200 }).subscribe(r => {
      this.filteredProducts = r.items;
    });
  }

  onProductFocus(): void {
    this.showProductDrop = true;
    clearTimeout(this.productSearchTimer);
    this.fetchProducts();
  }

  private fetchProducts(): void {
    const shopId = this.auth.getShopId()!;
    const q = this.productSearch.trim();
    this.productService
      .getProducts(shopId, {
        page: 1,
        pageSize: q ? 100 : 200,
        search: q || undefined
      })
      .subscribe({
        next: r => {
          this.filteredProducts = r.items;
        },
        error: () => {
          this.filteredProducts = [];
        }
      });
  }

  onProductSearch(): void {
    this.showProductDrop = true;
    clearTimeout(this.productSearchTimer);
    this.productSearchTimer = setTimeout(() => this.fetchProducts(), 320);
  }

  addProduct(p: Product): void {
    const cost = p.purchasePrice ?? p.unitPrice ?? 0;
    const item = this.fb.group({
      productId:   [p.productId ?? p.id],
      productName: [p.productName ?? p.name],
      quantity:    [1, [Validators.required, Validators.min(0.001)]],
      unitPrice:   [cost, Validators.required],
      gstPercent:  [0],
      gstAmount:   [0],
      totalAmount: [cost]
    });
    this.items.push(item);
    this.productSearch = '';
    this.showProductDrop = false;
    this.recalc(this.items.length - 1);
  }

  removeItem(i: number): void { this.items.removeAt(i); }

  recalc(i: number): void {
    const item = this.items.at(i);
    const qty = +item.value.quantity || 0;
    const price = +item.value.unitPrice || 0;
    const gstPct = +item.value.gstPercent || 0;
    const base = qty * price;
    const gstAmt = +(base * gstPct / 100).toFixed(2);
    item.patchValue({ gstAmount: gstAmt, totalAmount: +(base + gstAmt).toFixed(2) }, { emitEvent: false });
  }

  get items(): FormArray { return this.form.get('items') as FormArray; }
  get grandTotal(): number { return this.items.controls.reduce((s, i) => s + (+i.value.totalAmount || 0), 0); }

  get amountPaidNumber(): number {
    const v = this.form.get('amountPaid')?.value;
    return +(v ?? 0);
  }

  get purchaseDue(): number {
    return this.grandTotal - this.amountPaidNumber;
  }

  getSelectedSupplier(): Supplier | undefined {
    const supplierId = +(this.form.get('supplierId')?.value || 0);
    return this.suppliers.find(s => s.supplierId === supplierId);
  }

  onSubmit(): void {
    if (this.form.invalid || !this.items.length) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    this.purchaseService.createPurchase(shopId, { ...this.form.value, items: this.items.value } as any).subscribe({
      next: () => { this.submitting = false; this.router.navigate(['/shop/purchases']); },
      error: () => { this.submitting = false; }
    });
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(v ?? 0);
  }
}
