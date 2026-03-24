import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-shop-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, HeaderComponent],
  templateUrl: './shop-layout.component.html',
  styleUrls: ['./shop-layout.component.scss']
})
export class ShopLayoutComponent {
  sidebarCollapsed = false;
  pageTitle = 'Dashboard';
  private titleMap: Record<string, string> = {
    '/shop/dashboard': 'Dashboard', '/shop/billing': 'Billing',
    '/shop/billing/create': 'Create Bill', '/shop/customers': 'Customers',
    '/shop/customers/add': 'Add Customer', '/shop/inventory': 'Inventory',
    '/shop/inventory/add': 'Add Product', '/shop/suppliers': 'Suppliers',
    '/shop/purchases': 'Purchases', '/shop/purchases/create': 'Create Purchase',
    '/shop/expenses': 'Expenses', '/shop/expenses/add': 'Add Expense',
    '/shop/reports': 'Reports',
  };
  constructor(private router: Router) {
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => {
        const url = e.urlAfterRedirects.split('?')[0];
        this.pageTitle = this.titleMap[url] || 'AgroBilling';
      });
  }
  toggle(): void { this.sidebarCollapsed = !this.sidebarCollapsed; }
}