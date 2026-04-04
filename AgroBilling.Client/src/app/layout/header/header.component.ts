// ================================================
//  src/app/layout/header/header.component.ts
//  REPLACE existing file completely
//  RouterModule REMOVED (href used in HTML instead of routerLink)
//  Expiry warning getters added
// ================================================

import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],   // ✅ RouterModule nahi — href use kiya HTML mein
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  @Input() pageTitle = '';
  @Input() sidebarCollapsed = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  userMenuOpen = false;

  constructor(public auth: AuthService, public theme: ThemeService) {}

  get ownerName(): string { return this.auth.getShopName() || 'Admin'; }
  get initials(): string  { return this.ownerName.substring(0, 2).toUpperCase(); }
  get isAdmin(): boolean  { return this.auth.isAdmin(); }
  get shopName(): string  { return this.auth.getShopName() || 'My Shop'; }

  // ✅ Subscription expiry warning
  get daysLeft(): number | null {
    if (this.isAdmin) return null;
    return this.auth.getDaysUntilExpiry();
  }

  get showExpiryWarning(): boolean {
    if (this.isAdmin) return false;
    const d = this.daysLeft;
    return d !== null && d <= 7 && d >= 0;
  }

  get expiryBannerClass(): string {
    if (this.daysLeft !== null && this.daysLeft <= 2)
      return 'expiry-banner expiry-critical';
    return 'expiry-banner expiry-warning';
  }

  get expiryMessage(): string {
    if (this.daysLeft === 0) return '⚠️ Subscription aaj expire ho rahi hai! Abhi subscribe karo.';
    if (this.daysLeft === 1) return '⚠️ Subscription kal expire hogi! Abhi subscribe karo.';
    return `⚠️ Subscription ${this.daysLeft} din mein expire hogi. Abhi renew karo.`;
  }

  toggleUserMenu(): void { this.userMenuOpen = !this.userMenuOpen; }
  closeUserMenu(): void  { this.userMenuOpen = false; }

  logout(): void {
    this.closeUserMenu();
    this.auth.logout();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void { this.closeUserMenu(); }
}