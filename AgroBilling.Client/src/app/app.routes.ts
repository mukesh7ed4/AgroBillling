import { Routes } from '@angular/router';
import { adminGuard, shopGuard, subscriptionGuard, authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },

  // AUTH
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(c => c.LoginComponent)
  },
  {
    path: 'auth/signup',
    loadComponent: () =>
      import('./features/auth/signup/signup.component').then(c => c.SignupComponent)
  },
  {
    path: 'auth/forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent)
  },

  // SUBSCRIPTION
  {
    path: 'subscribe',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/subscription/subscription.component').then(c => c.SubscriptionComponent)
  },

  // ADMIN
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./layout/admin-layout/admin-layout.component').then(c => c.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',     loadComponent: () => import('./features/admin/dashboard/admin-dashboard.component').then(c => c.AdminDashboardComponent) },
      { path: 'shops',         loadComponent: () => import('./features/admin/shops/shops.component').then(c => c.ShopsComponent) },
      { path: 'subscriptions', loadComponent: () => import('./features/admin/subscriptions/subscriptions.component').then(c => c.SubscriptionsComponent) },
      { path: 'notifications', loadComponent: () => import('./features/admin/notifications/notifications.component').then(c => c.NotificationsComponent) },
    ]
  },

  // SHOP
  {
    path: 'shop',
    canActivate: [shopGuard, subscriptionGuard],
    loadComponent: () =>
      import('./layout/shop-layout/shop-layout.component').then(c => c.ShopLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',        loadComponent: () => import('./features/shop/dashboard/shop-dashboard.component').then(c => c.ShopDashboardComponent) },
      { path: 'billing',          loadComponent: () => import('./features/shop/billing/billing.component').then(c => c.BillingComponent) },
      { path: 'billing/create',   loadComponent: () => import('./features/shop/billing/create-bill.component').then(c => c.CreateBillComponent) },
      { path: 'billing/:id',      loadComponent: () => import('./features/shop/billing/bill-detail.component').then(c => c.BillDetailComponent) },
      { path: 'customers',        loadComponent: () => import('./features/shop/customers/customers.component').then(c => c.CustomersComponent) },
      { path: 'customers/add',    loadComponent: () => import('./features/shop/customers/add-customer.component').then(c => c.AddCustomerComponent) },
      { path: 'customers/:id',    loadComponent: () => import('./features/shop/customers/customer-detail.component').then(c => c.CustomerDetailComponent) },
      { path: 'inventory',        loadComponent: () => import('./features/shop/inventory/inventory.component').then(c => c.InventoryComponent) },
      { path: 'inventory/add',    loadComponent: () => import('./features/shop/inventory/add-product.component').then(c => c.AddProductComponent) },
      { path: 'suppliers',        loadComponent: () => import('./features/shop/suppliers/suppliers.component').then(c => c.SuppliersComponent) },
      { path: 'suppliers/:id',    loadComponent: () => import('./features/shop/suppliers/supplier-detail.component').then(c => c.SupplierDetailComponent) },
      { path: 'purchases',        loadComponent: () => import('./features/shop/purchases/purchases.component').then(c => c.PurchasesComponent) },
      { path: 'purchases/create', loadComponent: () => import('./features/shop/purchases/create-purchase.component').then(c => c.CreatePurchaseComponent) },
      { path: 'expenses',         loadComponent: () => import('./features/shop/expenses/expenses.component').then(c => c.ExpensesComponent) },
      { path: 'reports',          loadComponent: () => import('./features/shop/reports/reports.component').then(c => c.ReportsComponent) },
      { path: 'profile',          loadComponent: () => import('./features/shop/profile/profile.component').then(c => c.ProfileComponent) },
      { path: 'purchases/:id', loadComponent: () => import('./features/shop/purchases/purchases.component').then(c => c.PurchasesComponent) },
    ]
  },

  { path: '**', redirectTo: 'auth/login' }
];
