// ================================================
//  src/app/core/interceptors/jwt.interceptor.ts
//  REPLACE existing file completely
//
//  Fixes:
//  1. 401 pe logout NAHI — login loop rokne ke liye
//     (logout sirf user manually kare)
//  2. 403 pe /unauthorized redirect NAHI — route exist nahi karta
//     Components khud handle karein 403
// ================================================

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const jwtInterceptorFn: HttpInterceptorFn = (req, next) => {
  const auth  = inject(AuthService);
  const token = auth.getToken();

  // ✅ Har request mein Bearer token add karo
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // ✅ Token genuinely expire ho gaya (JWT exp time past) — tab logout karo
      // Sirf tab — not on every 401 (could be wrong credentials, not expired token)
      if (err.status === 401) {
        const isAuthEndpoint = req.url.includes('/auth/login') ||
                               req.url.includes('/auth/signup') ||
                               req.url.includes('/auth/verify') ||
                               req.url.includes('/auth/forgot');
        // Auth endpoints pe 401 = wrong password — logout mat karo
        if (!isAuthEndpoint && !auth.hasValidToken()) {
          auth.logout();
        }
      }
      // ✅ 403 pe kuch mat karo — component/service handle karega
      // Router redirect nahi — /unauthorized route exist nahi karta

      return throwError(() => err);
    })
  );
};