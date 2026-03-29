import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, retry, shareReplay, timer } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  PagedResponse,
  Bill,
  CreateBillRequest,
  AddPaymentRequest,
  BillPayment,
  CreditNote,
  CreateReturnRequest,
  Customer,
  CreateCustomerRequest,
  CustomerLedger,
  Product,
  CreateProductRequest,
  ProductCategory,
  Unit,
  Supplier,
  SupplierLedger,
  PurchaseOrder,
  CreatePurchaseRequest,
  Expense,
  CreateExpenseRequest,
  ExpenseCategory,
  MonthlyDashboard,
  AdminDashboard,
  Notification,
  Shop,
  CreateShopRequest,
  SubscriptionPlan
} from '../models/models';

const API = environment.apiUrl;

function buildParams(obj?: Record<string, unknown> | null): HttpParams {
  let p = new HttpParams();
  if (obj == null) return p;
  for (const [k, v] of Object.entries(obj)) {
    if (v != null && v !== '') {
      p = p.set(k, typeof v === 'object' ? JSON.stringify(v) : String(v));
    }
  }
  return p;
}

@Injectable({ providedIn: 'root' })
export class BillingService {
  private http = inject(HttpClient);

  /** shareReplay: same bill detail is not re-fetched when navigating back from customer etc. */
  private billDetailById = new Map<number, Observable<ApiResponse<Bill>>>();

  createBill(shopId: number, req: CreateBillRequest) {
    return this.http.post<ApiResponse<Bill>>(`${API}/bills/${shopId}`, req);
  }

  getBills(shopId: number, params?: Record<string, unknown>) {
    return this.http.get<PagedResponse<Bill>>(`${API}/bills/${shopId}`, {
      params: buildParams(params)
    });
  }

  getBillById(billId: number) {
    let obs = this.billDetailById.get(billId);
    if (!obs) {
      obs = this.http
        .get<ApiResponse<Bill>>(`${API}/bills/detail/${billId}`)
        .pipe(shareReplay({ bufferSize: 1, refCount: true }));
      this.billDetailById.set(billId, obs);
    }
    return obs;
  }

  /** Call after payment / return so the next load hits the API */
  invalidateBillDetailCache(billId?: number): void {
    if (billId != null) this.billDetailById.delete(billId);
    else this.billDetailById.clear();
  }

  addPayment(req: AddPaymentRequest) {
    return this.http.post<ApiResponse<BillPayment>>(`${API}/bills/payment`, req);
  }

  processReturn(shopId: number, req: CreateReturnRequest) {
    return this.http.post<ApiResponse<CreditNote>>(`${API}/bills/${shopId}/return`, req);
  }

  // ✅ Bulk payment — distributes amount across oldest pending bills
  bulkPayment(shopId: number, req: any) {
    return this.http.post<ApiResponse<any>>(`${API}/bills/${shopId}/bulk-payment`, req);
  }

  getCreditNotes(shopId: number) {
    return this.http.get<ApiResponse<CreditNote[]>>(
      `${API}/bills/${shopId}/credit-notes`
    );
  }
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private http = inject(HttpClient);

  getCustomers(shopId: number, search?: string, page = 1, pageSize = 10) {
    return this.http.get<PagedResponse<Customer>>(`${API}/customers/${shopId}`, {
      params: buildParams({ search, page, pageSize })
    });
  }

  getCustomerById(id: number) {
    return this.http.get<ApiResponse<Customer>>(`${API}/customers/detail/${id}`);
  }

  getCustomerLedger(
    id: number,
    params?: { billsTake?: number; paymentsTake?: number; creditsTake?: number }
  ) {
    return this.http.get<ApiResponse<CustomerLedger>>(`${API}/customers/${id}/ledger`, {
      params: buildParams(params ?? {})
    });
  }

  createCustomer(shopId: number, req: CreateCustomerRequest) {
    return this.http.post<ApiResponse<Customer>>(`${API}/customers/${shopId}`, req);
  }

  updateCustomer(id: number, req: Partial<CreateCustomerRequest>) {
    return this.http.put<ApiResponse<Customer>>(`${API}/customers/${id}`, req);
  }
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  /** Shared observables — pair with HTTP GET cache interceptor; avoids duplicate in-flight requests */
  private units$?: Observable<ApiResponse<Unit[]>>;
  private readonly categories$ = new Map<
    number,
    Observable<ApiResponse<ProductCategory[]>>
  >();

  getProducts(shopId: number, params?: Record<string, unknown>) {
    return this.http.get<PagedResponse<Product>>(`${API}/products/${shopId}`, {
      params: buildParams(params)
    });
  }

  getProductById(id: number) {
    return this.http.get<ApiResponse<Product>>(`${API}/products/detail/${id}`);
  }

  // ✅ Full product detail with stock movements + purchase history
  getProductDetail(id: number) {
    return this.http.get<ApiResponse<any>>(`${API}/products/full/${id}`);
  }

  createProduct(shopId: number, req: CreateProductRequest) {
    return this.http.post<ApiResponse<Product>>(`${API}/products/${shopId}`, req);
  }

  updateProduct(id: number, req: Partial<CreateProductRequest>) {
    return this.http.put<ApiResponse<Product>>(`${API}/products/${id}`, req);
  }

  getLowStock(shopId: number) {
    return this.http.get<ApiResponse<Product[]>>(
      `${API}/products/${shopId}/low-stock`
    );
  }

  getCategories(shopId: number) {
    let obs = this.categories$.get(shopId);
    if (!obs) {
      obs = this.http
        .get<ApiResponse<ProductCategory[]>>(`${API}/categories/${shopId}`)
        .pipe(shareReplay({ bufferSize: 1, refCount: false }));
      this.categories$.set(shopId, obs);
    }
    return obs;
  }

  /** Clears cached category streams after a new category is created (same session). */
  invalidateCategoryCache(shopId: number): void {
    this.categories$.delete(shopId);
  }

  getUnits() {
    if (!this.units$) {
      this.units$ = this.http
        .get<ApiResponse<Unit[]>>(`${API}/units`)
        .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    }
    return this.units$;
  }
}

@Injectable({ providedIn: 'root' })
export class SupplierService {
  private http = inject(HttpClient);

  getSuppliers(shopId: number) {
    return this.http.get<ApiResponse<Supplier[]>>(`${API}/suppliers/${shopId}`);
  }

  getSupplierLedger(id: number) {
    return this.http.get<ApiResponse<SupplierLedger>>(
      `${API}/suppliers/${id}/ledger`
    );
  }

  createSupplier(shopId: number, req: Partial<Supplier>) {
    return this.http.post<ApiResponse<Supplier>>(`${API}/suppliers/${shopId}`, req);
  }

  updateSupplier(id: number, req: Partial<Supplier>) {
    return this.http.put<ApiResponse<Supplier>>(`${API}/suppliers/${id}`, req);
  }

  addPayment(supplierId: number, req: unknown) {
    return this.http.post<ApiResponse<unknown>>(
      `${API}/suppliers/${supplierId}/payment`,
      req
    );
  }
}

@Injectable({ providedIn: 'root' })
export class PurchaseService {
  private http = inject(HttpClient);

  getPurchases(shopId: number, params?: Record<string, unknown>) {
    return this.http.get<PagedResponse<PurchaseOrder>>(
      `${API}/purchases/${shopId}`,
      { params: buildParams(params) }
    );
  }

  getPurchaseById(id: number) {
    return this.http.get<ApiResponse<PurchaseOrder>>(
      `${API}/purchases/detail/${id}`
    );
  }

  createPurchase(shopId: number, req: CreatePurchaseRequest) {
    return this.http.post<ApiResponse<PurchaseOrder>>(
      `${API}/purchases/${shopId}`,
      req
    );
  }

  /** Supplier payment from supplier ledger (shopId reserved for future / multi-tenant routing) */
  addSupplierPayment(shopId: number, supplierId: number, req: unknown) {
    void shopId;
    return this.http.post<ApiResponse<unknown>>(
      `${API}/suppliers/${supplierId}/payment`,
      req
    );
  }
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private http = inject(HttpClient);

  getExpenses(shopId: number, params?: Record<string, unknown>) {
    return this.http.get<PagedResponse<Expense>>(`${API}/expenses/${shopId}`, {
      params: buildParams(params)
    });
  }

  createExpense(shopId: number, req: CreateExpenseRequest) {
    return this.http.post<ApiResponse<Expense>>(
      `${API}/expenses/${shopId}`,
      req
    );
  }

  getCategories(shopId: number) {
    return this.http.get<ApiResponse<ExpenseCategory[]>>(
      `${API}/expense-categories/${shopId}`
    );
  }

  /** Alias used by shop expenses screen */
  getShopCategories(shopId: number) {
    return this.getCategories(shopId);
  }
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private http = inject(HttpClient);

  getMonthly(shopId: number, year: number, month: number) {
    return this.http
      .get<ApiResponse<MonthlyDashboard>>(
        `${API}/reports/${shopId}/monthly?year=${year}&month=${month}`
      )
      .pipe(
        retry({
          count: 2,
          delay: (_err, retryCount) => timer(200 * retryCount)
        })
      );
  }

  /** Alias used by shop dashboard & reports screens */
  getMonthlyDashboard(shopId: number, year: number, month: number) {
    return this.getMonthly(shopId, year, month);
  }

  getAdminDashboard() {
    return this.http.get<ApiResponse<Partial<AdminDashboard>>>(
      `${API}/admin/dashboard`
    );
  }

  // ✅ Export all shop data as ZIP
  exportShopData(shopId: number) {
    return this.http.get(
      `${API}/reports/${shopId}/export`,
      { responseType: 'blob' }
    );
  }
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);

  getShops(params?: Record<string, unknown>) {
    return this.http.get<PagedResponse<Shop>>(`${API}/admin/shops`, {
      params: buildParams(params)
    });
  }

  createShop(req: CreateShopRequest) {
    return this.http.post<ApiResponse<Shop>>(`${API}/admin/shops`, req);
  }

  getPlans() {
    return this.http.get<ApiResponse<SubscriptionPlan[]>>(`${API}/admin/plans`);
  }

  extendSubscription(shopId: number, req: unknown) {
    return this.http.post<ApiResponse<unknown>>(
      `${API}/admin/shops/${shopId}/extend`,
      req
    );
  }

  toggleShopStatus(shopId: number) {
    return this.http.patch<ApiResponse<unknown>>(
      `${API}/admin/shops/${shopId}/toggle`,
      {}
    );
  }

  getNotifications() {
    return this.http.get<ApiResponse<Notification[]>>(
      `${API}/admin/notifications`
    );
  }

  markRead(id: number) {
    return this.http.patch<ApiResponse<unknown>>(
      `${API}/admin/notifications/${id}/read`,
      {}
    );
  }

  /** Alias used by notifications screen */
  markNotificationRead(id: number) {
    return this.markRead(id);
  }
}