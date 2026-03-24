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

// ✅ Subscription check — trial/subscription expire pe /subscribe pe bhejo
export const subscriptionGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (!auth.hasValidToken()) return true; // shopGuard already handles

  const status = auth.getSubscriptionStatus();

  if (status === 'ACTIVE' || status === 'TRIAL') return true;

  // Expired ya no subscription — subscribe page pe bhejo
  router.navigate(['/subscribe']);
  return false;
};