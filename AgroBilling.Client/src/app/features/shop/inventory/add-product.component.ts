import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
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
  private readonly cdr = inject(ChangeDetectorRef);
  submitting = false;
  errorMessage: string = '';  // ✅ Add error message
  successMessage: string = '';  // ✅ Add success message
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
    this.auth.runWhenShopReady(() => {
      this.loadLookups();
    });
  }

  private loadLookups(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) return;
    
    this.productService.getCategories(shopId).subscribe({
      next: (res) => {
        this.categories = res.data || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading categories:', err);
        this.errorMessage = 'Failed to load categories';
        this.cdr.detectChanges();
      }
    });
    
    this.productService.getUnits().subscribe({
      next: (res) => {
        this.units = res.data || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading units:', err);
        this.errorMessage = 'Failed to load units';
        this.cdr.detectChanges();
      }
    });
    
    
    this.supplierService.getSuppliers(shopId).subscribe({
      next: (res) => {
        this.suppliers = res.data || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading suppliers:', err);
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
    this.errorMessage = '';
    this.successMessage = '';
    
    const shopId = this.auth.getShopId()!;
    const raw = this.form.value as any;
    
    const payload = {
      ...raw,
      categoryId: Number(raw.categoryId),
      unitId: Number(raw.unitId),
      supplierId: raw.supplierId ? Number(raw.supplierId) : null
    };
    
    console.log('Submitting product:', payload);
    console.log('API URL:', `/api/products/${shopId}`);
    
    this.productService.createProduct(shopId, payload).subscribe({
      next: (res) => { 
        console.log('Product created successfully:', res);
        this.successMessage = 'Product added successfully! Redirecting...';
        this.submitting = false;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.router.navigate(['/shop/inventory']);
        }, 1500);
      },
      error: (err) => { 
        console.error('Error creating product:', err);
        this.errorMessage = err.error?.message || err.message || 'Failed to create product';
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
    
  }
}