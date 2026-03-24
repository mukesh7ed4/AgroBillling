import { Component, OnDestroy, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BillingService, CustomerService, ProductService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Customer, Product } from '../../../core/models/models';

@Component({
  selector: 'app-create-bill',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './create-bill.component.html',
  styleUrls: ['./create-bill.component.scss']
})
export class CreateBillComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  filteredCustomers: Customer[] = [];
  filteredProducts: Product[] = [];
  customerSearch = '';
  productSearch = '';
  showCustomerDrop = false;
  showProductDrop = false;
  selectedCustomer: Customer | null = null;
  submitting = false;
  productStockMap: Map<number | string, number> = new Map();

  private readonly cdr = inject(ChangeDetectorRef);

  private customerSearchTimer?: ReturnType<typeof setTimeout>;
  private productSearchTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private fb: FormBuilder,
    private billingService: BillingService,
    private customerService: CustomerService,
    private productService: ProductService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      customerId:     ['', Validators.required],
      billDate:       [new Date().toISOString().substring(0,10), Validators.required],
      gstPercent:     [0],
      discountAmount: [0],
      amountPaid:     [0],
      notes:          [''],
      items:          this.fb.array([])
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.customerSearchTimer);
    clearTimeout(this.productSearchTimer);
  }

  private fetchCustomers(): void {
    const shopId = this.auth.getShopId()!;
    const q = this.customerSearch.trim();
    this.customerService.getCustomers(shopId, q || undefined, 1, 100).subscribe({
      next: res => {
        let list = res.items;
        const sel = this.selectedCustomer;
        if (sel) {
          const sid = sel.customerId ?? sel.id;
          if (sid != null && !list.some(c => (c.customerId ?? c.id) === sid)) {
            list = [sel, ...list];
          }
        }
        this.filteredCustomers = list;
        this.cdr.detectChanges();
      },
      error: () => {
        this.filteredCustomers = [];
        this.cdr.detectChanges();
      }
    });
  }

  onCustomerFocus(): void {
    this.showCustomerDrop = true;
    clearTimeout(this.customerSearchTimer);
    this.fetchCustomers();
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
        next: res => {
          this.filteredProducts = res.items;
          this.cdr.detectChanges();
        },
        error: () => {
          this.filteredProducts = [];
          this.cdr.detectChanges();
        }
      });
  }

  // Customer search (server-side; debounced)
  onCustomerSearch(): void {
    this.showCustomerDrop = true;
    clearTimeout(this.customerSearchTimer);
    this.customerSearchTimer = setTimeout(() => this.fetchCustomers(), 320);
  }

  selectCustomer(c: Customer): void {
    this.selectedCustomer = c;
    this.customerSearch = c.fullName ?? c.name ?? '';
    this.form.patchValue({ customerId: c.customerId ?? c.id });
    this.showCustomerDrop = false;
  }

  // Product search (server-side; debounced)
  onProductSearch(): void {
    this.showProductDrop = true;
    clearTimeout(this.productSearchTimer);
    this.productSearchTimer = setTimeout(() => this.fetchProducts(), 320);
  }

  addProductToItems(p: Product): void {
    const price = p.sellingPrice ?? p.unitPrice ?? 0;
    const productId = p.productId ?? p.id ?? 0;
    const maxStock = p.currentStock ?? 0;

    this.productStockMap.set(productId, maxStock);

    const itemGroup = this.fb.group({
      productId:      [productId],
      productName:    [p.productName ?? p.name],
      unitShortName:  [p.unitShortName || ''],
      quantity:       [1, [Validators.required, Validators.min(0.001), this.createStockValidator(maxStock)]],
      unitPrice:      [price, Validators.required],
      discountAmount: [0],
      gstPercent:     [p.useShopGst ? this.form.value.gstPercent : p.gstPercent],
      gstAmount:      [0],
      totalAmount:    [price]
    });
    this.items.push(itemGroup);
    this.productSearch = '';
    this.showProductDrop = false;
    this.recalcItem(this.items.length - 1);
  }

  private createStockValidator(maxStock: number) {
    return (control: AbstractControl): ValidationErrors | null => {
      const quantity = +control.value || 0;
      if (quantity > maxStock) {
        return { insufficientStock: { available: maxStock, requested: quantity } };
      }
      return null;
    };
  }

  getQuantityError(index: number): string {
    const control = this.items.at(index).get('quantity');
    if (control?.hasError('insufficientStock')) {
      const error = control.getError('insufficientStock');
      return `Quantity not available. Available: ${error.available} units`;
    }
    if (control?.hasError('min')) {
      return 'Quantity must be greater than 0';
    }
    if (control?.hasError('required')) {
      return 'Quantity is required';
    }
    return '';
  }

  isQuantityInvalid(index: number): boolean {
    const control = this.items.at(index).get('quantity');
    return control ? (control.invalid && control.touched) : false;
  }

  removeItem(i: number): void { this.items.removeAt(i); }

  recalcItem(i: number): void {
    const item = this.items.at(i);
    const qty      = +item.value.quantity || 0;
    const price    = +item.value.unitPrice || 0;
    const disc     = +item.value.discountAmount || 0;
    const gstPct   = +item.value.gstPercent || 0;
    const base     = (qty * price) - disc;
    const gstAmt   = +(base * gstPct / 100).toFixed(2);
    const total    = +(base + gstAmt).toFixed(2);
    item.patchValue({ gstAmount: gstAmt, totalAmount: total }, { emitEvent: false });
  }

  get items(): FormArray { return this.form.get('items') as FormArray; }

  get subTotal(): number { return this.items.controls.reduce((s, i) => s + (+i.value.totalAmount || 0), 0); }
  get discountAmt(): number { return +this.form.value.discountAmount || 0; }
  get gstAmt(): number { return +(this.subTotal * (+this.form.value.gstPercent || 0) / 100).toFixed(2); }
  get grandTotal(): number { return +(this.subTotal - this.discountAmt + this.gstAmt).toFixed(2); }
  get amountPaid(): number { return +this.form.value.amountPaid || 0; }
  get amountPending(): number { return +(this.grandTotal - this.amountPaid).toFixed(2); }
  get paymentStatus(): string {
    if (this.amountPaid >= this.grandTotal) return '✅ PAID';
    if (this.amountPaid > 0) return '🕐 PARTIAL';
    return '❌ PENDING';
  }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(v ?? 0);
  }

  fmtProductPrice(p: Product): string {
    return this.fmt(p.sellingPrice ?? p.unitPrice);
  }

  onSubmit(): void {
    if (this.form.invalid || !this.items.length) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    const payload = { ...this.form.value, items: this.items.value };
    this.billingService.createBill(shopId, payload).subscribe({
      next: res => {
        this.submitting = false;
        const id = res.data.billId ?? res.data.id;
        this.router.navigate(['/shop/billing', id]);
        this.cdr.detectChanges();
      },
      error: () => {
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }
}
