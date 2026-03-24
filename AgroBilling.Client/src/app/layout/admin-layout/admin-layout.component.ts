import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, HeaderComponent],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent {
  sidebarCollapsed = false;
  pageTitle = 'Dashboard';

  private titleMap: Record<string, string> = {
    '/admin/dashboard':     'Admin Dashboard',
    '/admin/shops':         'Manage Shops',
    '/admin/subscriptions': 'Subscriptions',
    '/admin/notifications': 'Notifications',
  };

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      const url = e.urlAfterRedirects.split('?')[0];
      this.pageTitle = this.titleMap[url] || 'AgroBilling Admin';
    });
  }

  toggle(): void { this.sidebarCollapsed = !this.sidebarCollapsed; }
} 
