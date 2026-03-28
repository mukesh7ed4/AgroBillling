import { Component, OnDestroy, OnInit, inject, ChangeDetectorRef } from '@angular/core';
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
  private readonly fb  = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  suppliers:        Supplier[] = [];
  filteredProducts: Product[]  = [];
  allProducts:      Product[]  = [];

  productSearch   = '';
  showProductDrop = false;
  submitting      = false;
  dataLoaded      = false;

  // ✅ New product inline modal
  showNewProductModal = false;
  newProductName      = '';
  newProductPrice     = 0;
  newProductGst       = 0;

  private searchTimer?: ReturnType<typeof setTimeout>;

  form = this.fb.group({
    supplierId:    ['', Validators.required],
    purchaseDate:  [new Date().toISOString().substring(0, 10), Validators.required],
    invoiceNumber: [''],    // ✅ Optional — backend auto-generates if blank
    amountPaid:    [0],
    paymentMode:   ['Cash'],
    notes:         [''],
    items:         this.fb.array([])
  });

  constructor(
    private purchaseService: PurchaseService,
    private supplierService: SupplierService,
    private productService:  ProductService,
    private auth:   AuthService,
    private router: Router
  ) {}

  ngOnDestroy(): void { clearTimeout(this.searchTimer); }

  ngOnInit(): void { this.auth.runWhenShopReady(() => this.loadInitialData()); }

  private loadInitialData(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) return;

    this.supplierService.getSuppliers(shopId).subscribe({
      next: r => { this.suppliers = (r as any)?.data ?? r ?? []; this.cdr.detectChanges(); }
    });

    this.productService.getProducts(shopId, { page: 1, pageSize: 500 }).subscribe({
      next: r => {
        this.allProducts      = r.items ?? [];
        this.filteredProducts = this.allProducts;
        this.dataLoaded       = true;
        this.cdr.detectChanges();
      }
    });
  }

  onProductSearch(): void {
    const q = this.productSearch.trim().toLowerCase();
    this.filteredProducts = q
      ? this.allProducts.filter(p => (p.productName ?? p.name ?? '').toLowerCase().includes(q))
      : this.allProducts;
    this.showProductDrop = true;
  }

  onProductFocus(): void {
    this.filteredProducts = this.productSearch.trim() ? this.filteredProducts : this.allProducts;
    this.showProductDrop  = true;
  }

  hideDropdown(): void { setTimeout(() => { this.showProductDrop = false; }, 200); }

  addProduct(p: Product): void {
    const cost = p.purchasePrice ?? p.unitPrice ?? 0;
    const gst  = p.gstPercent ?? 0;
    const item = this.fb.group({
      productId:   [p.productId ?? p.id],
      productName: [p.productName ?? p.name],
      quantity:    [1, [Validators.required, Validators.min(0.001)]],
      unitPrice:   [cost, Validators.required],
      gstPercent:  [gst],
      gstAmount:   [0],
      totalAmount: [cost],
      isNew:       [false]
    });
    this.items.push(item);
    this.productSearch   = '';
    this.showProductDrop = false;
    this.recalc(this.items.length - 1);
    this.cdr.detectChanges();
  }

  // ✅ Open inline new product modal
  openNewProduct(): void {
    this.newProductName  = this.productSearch.trim();
    this.newProductPrice = 0;
    this.newProductGst   = 0;
    this.showNewProductModal = true;
    this.showProductDrop     = false;
  }

  // ✅ Add new product (productId=0, backend creates it)
  confirmNewProduct(): void {
    if (!this.newProductName.trim()) return;
    const base   = this.newProductPrice;
    const gstAmt = +(base * this.newProductGst / 100).toFixed(2);
    const item   = this.fb.group({
      productId:   [0],                          // 0 = new product
      productName: [this.newProductName.trim()],
      quantity:    [1, [Validators.required, Validators.min(0.001)]],
      unitPrice:   [base, Validators.required],
      gstPercent:  [this.newProductGst],
      gstAmount:   [gstAmt],
      totalAmount: [+(base + gstAmt).toFixed(2)],
      isNew:       [true]
    });
    this.items.push(item);
    this.showNewProductModal = false;
    this.productSearch       = '';
    this.cdr.detectChanges();
  }

  removeItem(i: number): void { this.items.removeAt(i); }

  recalc(i: number): void {
    const item   = this.items.at(i);
    const qty    = +item.value.quantity   || 0;
    const price  = +item.value.unitPrice  || 0;
    const gstPct = +item.value.gstPercent || 0;
    const base   = qty * price;
    const gstAmt = +(base * gstPct / 100).toFixed(2);
    item.patchValue({ gstAmount: gstAmt, totalAmount: +(base + gstAmt).toFixed(2) }, { emitEvent: false });
  }

  get items(): FormArray { return this.form.get('items') as FormArray; }

  get subTotal(): number {
    return this.items.controls.reduce((s, i) => {
      const qty = +i.value.quantity || 0; const price = +i.value.unitPrice || 0;
      return s + qty * price;
    }, 0);
  }
  get totalGst(): number {
    return this.items.controls.reduce((s, i) => s + (+i.value.gstAmount || 0), 0);
  }
  get grandTotal(): number {
    return this.items.controls.reduce((s, i) => s + (+i.value.totalAmount || 0), 0);
  }
  get amountPaidNumber(): number { return +(this.form.get('amountPaid')?.value ?? 0); }
  get purchaseDue():      number { return this.grandTotal - this.amountPaidNumber; }

  getSelectedSupplier(): Supplier | undefined {
    const id = +(this.form.get('supplierId')?.value || 0);
    return this.suppliers.find(s => (s.supplierId ?? s.id) === id);
  }

  onSubmit(): void {
    if (this.form.invalid || !this.items.length) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    this.purchaseService.createPurchase(shopId, { ...this.form.value, items: this.items.value } as any)
      .subscribe({
        next: () => { this.submitting = false; this.router.navigate(['/shop/purchases']); },
        error: () => { this.submitting = false; }
      });
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 }).format(v ?? 0);
  }
}