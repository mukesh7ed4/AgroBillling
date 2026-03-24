export interface ApiResponse<T> {
  data: T;
  success?: boolean;
  message?: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page?: number;
  pageSize?: number;
  /** Full month sum (expenses API) */
  monthTotal?: number;
}

export interface BillItem {
  productId?: number;
  productName?: string;
  quantity?: number;
  unitPrice?: number;
  discountAmount?: number;
  gstPercent?: number;
  gstAmount?: number;
  totalAmount?: number;
}

export interface Bill {
  id?: number;
  billId?: number;
  shopId?: number;
  billNumber?: string;
  customerId?: number;
  /** Nested when loaded from API with customer join */
  customer?: { customerId?: number; fullName?: string; mobileNumber?: string };
  customerName?: string;
  customerMobile?: string;
  billDate?: string;
  totalAmount?: number;
  amountPaid?: number;
  amountPending?: number;
  paymentStatus?: string;
  gstPercent?: number;
  gstAmount?: number;
  discountAmount?: number;
  total?: number;
  status?: string;
  items?: BillItem[];
  payments?: BillPayment[];
}

export interface CreateBillRequest {
  customerId?: number;
  lines?: unknown[];
  items?: unknown[];
  notes?: string;
  billDate?: string;
  gstPercent?: number;
  discountAmount?: number;
  amountPaid?: number;
  [key: string]: unknown;
}

export interface AddPaymentRequest {
  billId: number;
  amount: number;
  method?: string;
  paymentMode?: string;
  reference?: string;
  notes?: string;
  paymentDate?: string;
}

export interface BillPayment {
  id?: number;
  billId?: number;
  amount?: number;
  paidAt?: string;
  paymentDate?: string;
  paymentMode?: string;
  reference?: string;
}

export interface CreditNote {
  id: number;
  billId?: number;
  amount?: number;
}

export interface CreateReturnRequest {
  billId?: number;
  customerId?: number;
  originalBillId?: number;
  returnDate?: string;
  lines?: unknown[];
  items?: unknown[];
  reason?: string;
}

export interface Customer {
  id?: number;
  customerId?: number;
  shopId?: number;
  name?: string;
  fullName?: string;
  fatherName?: string;
  phone?: string;
  mobileNumber?: string;
  email?: string;
  village?: string;
  district?: string;
  tehsil?: string;
  state?: string;
  totalBills?: number;
  totalPending?: number;
  openingBalance?: number;
}

export interface CreateCustomerRequest {
  name?: string;
  fullName?: string;
  fatherName?: string;
  phone?: string;
  mobileNumber?: string;
  email?: string;
  address?: string;
  village?: string;
  tehsil?: string;
  district?: string;
  state?: string;
  alternateMobile?: string;
  landAcres?: number | null;
  openingBalance?: number;
  [key: string]: unknown;
}

export interface LedgerBillRow {
  billId?: number;
  billNumber?: string;
  billDate?: string;
  totalAmount?: number;
  amountPaid?: number;
  amountPending?: number;
  paymentStatus?: string;
}

export interface LedgerPaymentRow {
  paymentDate?: string;
  amount?: number;
  paymentMode?: string;
  reference?: string;
}

export interface CreditNoteLedgerRow {
  creditNoteDate?: string;
  originalBillNumber?: string;
  originalBill?: { billNumber?: string };
  creditAmount?: number;
  adjustedAmount?: number;
  remainingCredit?: number;
  status?: string;
}

export interface CustomerLedger {
  customerId?: number;
  customer: Customer;
  bills: LedgerBillRow[];
  payments: LedgerPaymentRow[];
  creditNotes: CreditNoteLedgerRow[];
  entries?: unknown[];
  balance?: number;
  /** Full counts / sums from API (lists may be capped) */
  billsTotalCount?: number;
  paymentsTotalCount?: number;
  creditNotesTotalCount?: number;
  ledgerTotalBilled?: number;
  ledgerTotalPaidOnBills?: number;
}

export interface Product {
  id?: number;
  productId?: number;
  shopId?: number;
  name?: string;
  productName?: string;
  sku?: string;
  stockQty?: number;
  unitPrice?: number;
  unitShortName?: string;
  sellingPrice?: number;
  purchasePrice?: number;
  gstPercent?: number;
  useShopGst?: boolean;
  currentStock?: number;
  minStockAlert?: number;
  categoryName?: string;
  companyName?: string;
}

export interface CreateProductRequest {
  name?: string;
  productName?: string;
  sku?: string;
  categoryId?: number | string;
  unitId?: number | string;
  supplierId?: number | string;
  unitPrice?: number;
  stockQty?: number;
  purchasePrice?: number;
  sellingPrice?: number;
  gstPercent?: number;
  useShopGst?: boolean;
  currentStock?: number;
  minStockAlert?: number;
  companyName?: string;
  hsnCode?: string;
  [key: string]: unknown;
}

export interface ProductCategory {
  id: number;
  categoryId?: number;
  name?: string;
  categoryName?: string;
  shopId?: number;
}

export interface Unit {
  id: number;
  unitId?: number;
  name?: string;
  unitName?: string;
  symbol?: string;
  shortName?: string;
}

export interface Supplier {
  id?: number;
  supplierId?: number;
  shopId?: number;
  name?: string;
  companyName?: string;
  contactPersonName?: string;
  phone?: string;
  mobileNumber?: string;
  email?: string;
  gstNumber?: string;
  address?: string;
  totalPurchased?: number;
  totalPaid?: number;
  outstandingDue?: number;
}

export interface SupplierLedgerPurchaseRow {
  purchaseId?: number;
  purchaseDate?: string;
  invoiceNumber?: string;
  netPayable?: number;
  amountPaid?: number;
  paymentStatus?: string;
}

export interface SupplierLedgerPaymentRow {
  paymentDate?: string;
  invoiceNumber?: string;
  amount?: number;
  paymentMode?: string;
  reference?: string;
  purchase?: { purchaseId?: number; invoiceNumber?: string };
}

export interface SupplierLedger {
  supplierId?: number;
  supplier: Supplier;
  purchases: SupplierLedgerPurchaseRow[];
  payments: SupplierLedgerPaymentRow[];
  entries?: unknown[];
  balance?: number;
}

export interface PurchaseOrder {
  id?: number;
  purchaseId?: number;
  shopId?: number;
  supplierId?: number;
  total?: number;
  netPayable?: number;
  amountPaid?: number;
  paymentStatus?: string;
  purchaseDate?: string;
  invoiceNumber?: string;
  status?: string;
  supplierName?: string;
  supplier?: { supplierId?: number; companyName?: string };
}

export interface CreatePurchaseRequest {
  supplierId: number;
  lines?: unknown[];
  items?: unknown[];
  notes?: string;
  purchaseDate?: string;
  invoiceNumber?: string;
  amountPaid?: number;
  paymentMode?: string;
  [key: string]: unknown;
}

export interface Expense {
  id: number;
  shopId?: number;
  amount: number;
  categoryId?: number;
  categoryName?: string;
  description?: string;
  incurredOn?: string;
  expenseDate?: string;
  paymentMode?: string;
  reference?: string;
}

export interface CreateExpenseRequest {
  categoryId: number | string;
  amount: number | string;
  description?: string;
  incurredOn?: string;
  expenseDate?: string;
  paymentMode?: string;
  reference?: string;
  [key: string]: unknown;
}

export interface ExpenseCategory {
  id: number;
  categoryId?: number;
  name?: string;
  categoryName?: string;
  shopId?: number;
}

export interface SalesSummary {
  totalBills?: number;
  totalSales?: number;
  totalCollected?: number;
  totalPending?: number;
  paidBills?: number;
  partialBills?: number;
  pendingBills?: number;
}

export interface TopProductRow {
  productName?: string;
  totalQty?: number;
  totalAmount?: number;
}

export interface ExpenseBreakdownRow {
  categoryName?: string;
  totalExpense?: number;
}

/** Admin dashboard subscription row / shop summary */
export interface ShopAlert {
  shopId?: number;
  shopName?: string;
  ownerName?: string;
  mobileNumber?: string;
  city?: string;
  startDate?: string;
  endDate?: string;
  daysLeft: number;
  planName?: string;
  planId?: number;
}

export interface MonthlyDashboard {
  shopId?: number;
  year?: number;
  month?: number;
  revenue?: number;
  expenses?: number;
  salesSummary?: SalesSummary;
  totalExpenses?: number;
  totalPurchased?: number;
  paidToSuppliers?: number;
  topProducts?: TopProductRow[];
  expenseBreakdown?: ExpenseBreakdownRow[];
}

export interface AdminDashboard {
  totalShops?: number;
  activeSubscriptions?: number;
  metrics?: unknown;
  allShops: ShopAlert[];
  expired: ShopAlert[];
  expiringSoon: ShopAlert[];
}

export interface Notification {
  id: number;
  notificationId?: number;
  title?: string;
  body?: string;
  message?: string;
  read?: boolean;
  isRead?: boolean;
  type?: string;
  shopName?: string;
  createdAt?: string;
}

export interface Shop {
  id: number;
  shopId?: number;
  name?: string;
  shopName?: string;
  ownerName?: string;
  mobileNumber?: string;
  city?: string;
  gstPercent?: number;
  isActive?: boolean;
  subscriptionEndsAt?: string;
  subscription?: {
    planName?: string;
    planId?: number;
    endDate?: string;
    daysLeft?: number;
  };
}

export interface CreateShopRequest {
  name: string;
  ownerEmail?: string;
  planId?: number;
}

export interface SubscriptionPlan {
  id: number;
  planId?: number;
  name?: string;
  planName?: string;
  durationDays?: number;
  price?: number;
}

export type Theme = 'light' | 'dark';
export type Lang = 'en' | 'hi';

/** Alias for subscription views */
export type ShopSummary = ShopAlert;
