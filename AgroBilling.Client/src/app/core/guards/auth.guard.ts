// ================================================
//  src/app/core/guards/auth.guard.ts
//  REPLACE existing file completely
// ================================================

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { take, map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.hasValidToken()) return true;
  router.navigate(['/auth/login']);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.hasValidToken() && auth.isAdmin()) return true;
  if (auth.hasValidToken()) router.navigate(['/shop/dashboard']);
  else router.navigate(['/auth/login']);
  return false;
};

export const shopGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (!auth.hasValidToken()) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (!auth.isShop()) {
    router.navigate(['/admin/dashboard']);
    return false;
  }

  if (auth.getShopId() != null) return true;

  return auth.ensureShopId$().pipe(
    take(1),
    map(id => {
      if (id != null) return true;
      router.navigate(['/auth/login']);
      return false;
    })
  );
};

// ✅ FIXED subscriptionGuard — no redirect loop
// Sirf 2 cases mein block karo:
// 1. Token nahi — shopGuard already handle karta hai, yahan true return karo
// 2. EXPIRED status AND expiry date past mein — tabhi /subscribe bhejo
// Baaki sab cases mein ALLOW karo — 403 se handle hoga
export const subscriptionGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  // Token check shopGuard karta hai — yahan sirf subscription dekho
  if (!auth.hasValidToken()) return true;

  const status = auth.getSubscriptionStatus();
  const expiry = auth.getSubscriptionExpiry();

  // ✅ ACTIVE ya TRIAL — hamesha allow karo
  if (status === 'ACTIVE' || status === 'TRIAL') {
    // Agar expiry date bhi past mein hai tab block karo
    if (expiry) {
      const expiryDate = new Date(expiry);
      expiryDate.setHours(23, 59, 59, 999);
      if (new Date() > expiryDate) {
        auth.setSubscriptionStatus('EXPIRED', expiry);
        router.navigate(['/subscribe']);
        return false;
      }
    }
    return true;
  }

  // ✅ EXPIRED — block karo
  if (status === 'EXPIRED') {
    router.navigate(['/subscribe']);
    return false;
  }

  // ✅ Status null/undefined/PENDING/unknown — ALLOW karo
  // Agar subscription nahi hai toh login pe fresh status aayega
  // Guard ko assume nahi karna chahiye — server se 401/403 aayega
  return true;
};