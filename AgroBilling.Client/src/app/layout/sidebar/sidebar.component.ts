import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd, NavigationStart } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

interface NavItem { label: string; labelHi: string; icon: string; route: string; }

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit {
  @Input()  collapsed = false;
  @Output() toggleCollapse = new EventEmitter<void>();

  currentRoute = '';
  isAdmin = false;
  moreSheetOpen = false;

  adminNav: NavItem[] = [
    { label: 'Dashboard',     labelHi: 'डैशबोर्ड', icon: '📊', route: '/admin/dashboard' },
    { label: 'Shops',         labelHi: 'दुकानें',    icon: '🏪', route: '/admin/shops' },
    { label: 'Subscriptions', labelHi: 'सदस्यता',   icon: '📅', route: '/admin/subscriptions' },
    { label: 'Notifications', labelHi: 'सूचनाएं',   icon: '🔔', route: '/admin/notifications' },
  ];

  shopNav: NavItem[] = [
    { label: 'Dashboard', labelHi: 'डैशबोर्ड', icon: '📊', route: '/shop/dashboard' },
    { label: 'Billing',   labelHi: 'बिलिंग',    icon: '🧾', route: '/shop/billing' },
    { label: 'Customers', labelHi: 'ग्राहक',     icon: '👥', route: '/shop/customers' },
    { label: 'Inventory', labelHi: 'स्टॉक',      icon: '📦', route: '/shop/inventory' },
    { label: 'Suppliers', labelHi: 'सप्लायर',    icon: '🏭', route: '/shop/suppliers' },
    { label: 'Purchases', labelHi: 'खरीद',       icon: '🛒', route: '/shop/purchases' },
    { label: 'Expenses',  labelHi: 'खर्च',       icon: '💸', route: '/shop/expenses' },
    { label: 'Reports',   labelHi: 'रिपोर्ट',    icon: '📈', route: '/shop/reports' },
  ];

  get navItems(): NavItem[] { return this.isAdmin ? this.adminNav : this.shopNav; }

  /** First 4 nav items shown directly in the bottom bar */
  get primaryMobileNav(): NavItem[] { return this.navItems.slice(0, 4); }

  /** Remaining items shown inside the "More" sheet */
  get secondaryMobileNav(): NavItem[] { return this.navItems.slice(4); }

  constructor(public auth: AuthService, public theme: ThemeService, private router: Router) {}

  ngOnInit(): void {
    this.isAdmin = this.auth.isAdmin();
    this.currentRoute = this.router.url;

    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => {
        this.currentRoute = e.urlAfterRedirects;
      });

    // Close the "More" sheet automatically on any navigation
    this.router.events
      .pipe(filter(e => e instanceof NavigationStart))
      .subscribe(() => {
        this.moreSheetOpen = false;
      });
  }

  isActive(route: string): boolean { return this.currentRoute.startsWith(route); }

  logout(): void { this.auth.logout(); }

  get shopName(): string { return this.auth.getShopName() || 'My Shop'; }

  /** Toggle the mobile "More" bottom sheet */
  toggleMoreSheet(): void { this.moreSheetOpen = !this.moreSheetOpen; }

  /** Close the mobile "More" bottom sheet */
  closeMoreSheet(): void { this.moreSheetOpen = false; }

  /**
   * Returns true if any route inside the secondary (hidden) nav items
   * is currently active — used to show the green dot on the "More" button
   */
  hasActiveInMore(): boolean {
    return this.secondaryMobileNav.some(item => this.isActive(item.route));
  }
}