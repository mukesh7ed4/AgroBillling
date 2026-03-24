import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ProductService, SupplierService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { ProductCategory, Unit, Supplier } from '../../../core/models/models';

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './add-product.component.html',
  styleUrls: ['./add-product.component.scss']
})
export class AddProductComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  submitting = false;
  categories: ProductCategory[] = [];
  units: Unit[] = [];
  suppliers: Supplier[] = [];

  form = this.fb.group({
    categoryId:    ['', Validators.required],
    supplierId:    [''],
    productName:   ['', Validators.required],
    companyName:   [''],
    hsnCode:       [''],
    unitId:        ['', Validators.required],
    purchasePrice: [0, [Validators.required, Validators.min(0)]],
    sellingPrice:  [0, [Validators.required, Validators.min(0.01)]],
    gstPercent:    [0],
    useShopGst:    [true],
    currentStock:  [0, Validators.min(0)],
    minStockAlert: [5]
  });

  constructor(
    private productService: ProductService,
    private supplierService: SupplierService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => this.loadLookups());
  }

  private loadLookups(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) return;
    this.productService.getCategories(shopId).subscribe(r => (this.categories = r.data));
    this.productService.getUnits().subscribe(r => (this.units = r.data));
    this.supplierService.getSuppliers(shopId).subscribe(r => (this.suppliers = r.data));
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    this.productService.createProduct(shopId, this.form.value as any).subscribe({
      next: () => { this.submitting = false; this.router.navigate(['/shop/inventory']); },
      error: () => { this.submitting = false; }
    });
  }
}