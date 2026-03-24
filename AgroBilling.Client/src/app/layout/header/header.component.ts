import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
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

  toggleUserMenu(): void { this.userMenuOpen = !this.userMenuOpen; }
  closeUserMenu(): void  { this.userMenuOpen = false; }

  logout(): void {
    this.closeUserMenu();
    this.auth.logout();
  }

  /** Close the dropdown if user presses Escape */
  @HostListener('document:keydown.escape')
  onEscape(): void { this.closeUserMenu(); }
}