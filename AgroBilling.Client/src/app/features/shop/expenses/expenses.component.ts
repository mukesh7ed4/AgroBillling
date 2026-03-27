import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ExpenseService } from '../../../core/services/api.services';
import { AuthService } from '../../../core/services/auth.service';
import { Expense, ExpenseCategory } from '../../../core/models/models';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule],
  templateUrl: './expenses.component.html',
  styleUrls: ['./expenses.component.scss']
})
export class ExpensesComponent implements OnInit {
  private readonly fb  = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  expenses:   Expense[]         = [];
  categories: ExpenseCategory[] = [];
  loading      = true;
  showAddModal = false;
  submitting   = false;
  totalCount   = 0;
  pageNumber   = 1;
  readonly pageSize = 10;
  monthTotal: number | null = null;
  filterMonth = new Date().toISOString().substring(0, 7);

  form = this.fb.group({
    categoryId:  ['', Validators.required],
    expenseDate: [new Date().toISOString().substring(0, 10), Validators.required],
    amount:      ['', [Validators.required, Validators.min(0.01)]],
    description: [''],
    paymentMode: ['Cash'],
    reference:   ['']
  });

  constructor(private expenseService: ExpenseService, private auth: AuthService) {}

  ngOnInit(): void {
    this.auth.runWhenShopReady(() => {
      this.loadCategories();
      this.load();
    });
  }

  loadCategories(): void {
    const shopId = this.auth.getShopId()!;
    this.expenseService.getShopCategories(shopId).subscribe({
      next: r => {
        // Normalize — API may return data directly or wrapped
        const raw = (r as any)?.data ?? r ?? [];
        this.categories = (Array.isArray(raw) ? raw : []).map((c: any) => ({
          id:           c.id           ?? c.categoryId   ?? 0,
          categoryId:   c.categoryId   ?? c.id           ?? 0,
          name:         c.name         ?? c.categoryName ?? '',
          categoryName: c.categoryName ?? c.name         ?? '',
          shopId:       c.shopId
        }));
        this.cdr.detectChanges();
      },
      error: () => { this.categories = []; }
    });
  }

  onMonthChange(): void { this.pageNumber = 1; this.load(); }

  load(): void {
    const shopId = this.auth.getShopId();
    if (shopId == null) { this.loading = false; this.cdr.detectChanges(); return; }
    this.loading = true;
    this.expenseService.getExpenses(shopId, { month: this.filterMonth, page: this.pageNumber, pageSize: this.pageSize }).subscribe({
      next: res => {
        this.expenses   = (res.items ?? []).map((e: any) => ({
          ...e,
          categoryName: e.categoryName ?? e.category?.name ?? e.category?.categoryName ?? ''
        }));
        this.totalCount = res.totalCount ?? 0;
        this.monthTotal = (res as any).monthTotal ?? null;
        this.loading    = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    const shopId = this.auth.getShopId()!;
    this.expenseService.createExpense(shopId, this.form.value as any).subscribe({
      next: () => {
        this.submitting  = false;
        this.showAddModal = false;
        this.form.reset({ paymentMode: 'Cash', expenseDate: new Date().toISOString().substring(0, 10) });
        this.pageNumber = 1;
        this.load();
      },
      error: () => { this.submitting = false; }
    });
  }

  get totalExpenses(): number {
    if (this.monthTotal != null) return this.monthTotal;
    return this.expenses.reduce((s, e) => s + (e.amount ?? 0), 0);
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.load(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.load(); } }

  fmt(v: number | undefined): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(v ?? 0);
  }
}
