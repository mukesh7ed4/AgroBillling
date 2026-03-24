import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  defer,
  interval,
  of,
  tap,
  take,
  map,
  filter,
  defaultIfEmpty
} from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/models';
import { clearHttpGetCache } from '../interceptors/http-cache.interceptor';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'access_token';
  private readonly roleKey = 'ab_role';
  private readonly shopIdKey = 'ab_shop_id';
  private readonly shopNameKey = 'ab_shop_name';
  private readonly subStatusKey  = 'ab_sub_status';
private readonly subExpiryKey  = 'ab_sub_expiry';

  private _shopId$ = new BehaviorSubject<number | null>(this.readShopIdFromLocalStorage());
  private _isLoggedIn$ = new BehaviorSubject<boolean>(this.hasValidToken());

  /** Emits current shop id; hydrated from localStorage + JWT on startup. */
  readonly shopId$ = this._shopId$.asObservable();
  readonly isLoggedIn$ = this._isLoggedIn$.asObservable();

  constructor() {
    this.hydrateShopIdFromJwtIfMissing();
    this._isLoggedIn$.next(this.hasValidToken());
  }

  /**
   * Polls up to ~2s until localStorage/JWT yields a shop id, then emits once (number | null).
   * Always completes with one emission — use for guards and loaders.
   */
  ensureShopId$(): Observable<number | null> {
    return defer(() => {
      const id = this.getShopId();
      if (id != null) return of(id);
      return interval(20).pipe(
        take(100),
        map(() => this.getShopId()),
        filter((x): x is number => x != null),
        take(1)
      );
    }).pipe(
      take(1),
      defaultIfEmpty(null as number | null),
      map(id => id ?? this.getShopId())
    );
  }

  /** Run after shop id has been waited for (fn should still null-check getShopId()). */
  runWhenShopReady(fn: () => void): void {
    this.ensureShopId$()
      .pipe(take(1))
      .subscribe(() => fn());
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  hasValidToken(): boolean {
    const t = this.getToken();
    if (!t) return false;
    return !this.isJwtExpired(t);
  }

  signup(body: any): Observable<ApiResponse<any>> {
    return this.http
      .post<ApiResponse<any>>(`${environment.apiUrl}/auth/signup`, body)
      .pipe(
        tap(res => {
          // optional: auto login after signup (agar backend token bhejta hai)
          const data = res.data as any;
  
          const token = data?.token || data?.access_token;
          if (token) {
            localStorage.setItem(this.tokenKey, token);
            this._isLoggedIn$.next(true);
          }
  
          const shopId = data?.shopId;
          if (shopId) {
            localStorage.setItem(this.shopIdKey, shopId.toString());
            this._shopId$.next(shopId);
          }
  
          const shopName = data?.shopName;
          if (shopName) {
            localStorage.setItem(this.shopNameKey, shopName);
          }
        })
      );
  }

  private isJwtExpired(token: string): boolean {
    const payload = this.decodeJwtPayload(token);
    if (!payload) return true;
    const exp = payload['exp'];
    if (exp == null) return false;
    const e = typeof exp === 'number' ? exp : Number(exp);
    if (!Number.isFinite(e)) return false;
    return e * 1000 < Date.now();
  }

  /** Base64url-safe JWT payload decode (standard atob fails on many real tokens). */
  private decodeJwtPayload(token: string): Record<string, unknown> | null {
    try {
      const part = token.split('.')[1];
      if (!part) return null;
      const base64 = part.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + (4 - (base64.length % 4)) % 4, '=');
      return JSON.parse(atob(padded)) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private jwtPayload(): Record<string, unknown> | null {
    const t = this.getToken();
    if (!t) return null;
    return this.decodeJwtPayload(t);
  }

  isAdmin(): boolean {
    const r = localStorage.getItem(this.roleKey);
    if (r === 'ADMIN' || r === 'admin') return true;
    const p = this.jwtPayload();
    const role = String(p?.['role'] ?? p?.['Role'] ?? '').toUpperCase();
    return role === 'ADMIN';
  }

  isShop(): boolean {
    if (this.isAdmin()) return false;
    const r = localStorage.getItem(this.roleKey);
    if (r === 'SHOP' || r === 'shop') return true;
    const p = this.jwtPayload();
    const role = String(p?.['role'] ?? p?.['Role'] ?? '').toUpperCase();
    if (role === 'SHOP') return true;
    return this.getShopId() != null && this.hasValidToken();
  }

  private readShopIdFromLocalStorage(): number | null {
    const v = localStorage.getItem(this.shopIdKey);
    if (v == null || v === '') return null;
    const n = Number(v);
    return Number.isFinite(n) ? n : null;
  }

  private parseShopIdFromPayload(p: Record<string, unknown> | null): number | null {
    if (!p) return null;
    const id = p['shopId'] ?? p['ShopId'] ?? p['shop_id'];
    if (id == null) return null;
    const n = Number(id);
    return Number.isFinite(n) ? n : null;
  }

  private hydrateShopIdFromJwtIfMissing(): void {
    if (this._shopId$.value != null) return;
    const fromLs = this.readShopIdFromLocalStorage();
    if (fromLs != null) {
      this._shopId$.next(fromLs);
      return;
    }
    const sid = this.parseShopIdFromPayload(this.jwtPayload());
    if (sid != null) {
      localStorage.setItem(this.shopIdKey, String(sid));
      this._shopId$.next(sid);
    }
  }

  getShopId(): number | null {
    const subj = this._shopId$.value;
    if (subj != null) return subj;

    const fromLs = this.readShopIdFromLocalStorage();
    if (fromLs != null) {
      this._shopId$.next(fromLs);
      return fromLs;
    }

    const p = this.jwtPayload();
    const n = this.parseShopIdFromPayload(p);
    if (n != null) {
      localStorage.setItem(this.shopIdKey, String(n));
      this._shopId$.next(n);
    }
    return n;
  }

  getSubscriptionStatus(): 'ACTIVE' | 'TRIAL' | 'EXPIRED' | 'PENDING' | null {
    return localStorage.getItem(this.subStatusKey) as any;
  }
  
  getSubscriptionExpiry(): string | null {
    return localStorage.getItem(this.subExpiryKey);
  }
  
  setSubscriptionStatus(status: string, expiry?: string): void {
    localStorage.setItem(this.subStatusKey, status);
    if (expiry) localStorage.setItem(this.subExpiryKey, expiry);
  }

  login(body: { email: string; password: string }): Observable<ApiResponse<any>> {
    return this.http
      .post<ApiResponse<any>>(`${environment.apiUrl}/auth/login`, body)
      .pipe(
        tap(res => {
          const data  = res.data;
          const token = data?.token;
  
          if (token) {
            localStorage.setItem(this.tokenKey, token);
  
            const payload = this.decodeJwtPayload(token);
            const role = String(payload?.['role'] ?? payload?.['Role'] ?? '').toUpperCase();
            localStorage.setItem(this.roleKey, role);
          }
  
          const sid = data?.shopId;
          if (sid != null) {
            localStorage.setItem(this.shopIdKey, String(sid));
            this._shopId$.next(sid);
          } else if (token) {
            const fromJwt = this.parseShopIdFromPayload(this.decodeJwtPayload(token));
            if (fromJwt != null) {
              localStorage.setItem(this.shopIdKey, String(fromJwt));
              this._shopId$.next(fromJwt);
            }
          }
  
          if (data?.shopName) {
            localStorage.setItem(this.shopNameKey, data.shopName);
          }
  
          // ✅ Subscription
          if (data?.subscriptionStatus) {
            this.setSubscriptionStatus(data.subscriptionStatus, data.subscriptionExpiry);
          }
  
          this._isLoggedIn$.next(this.hasValidToken());
        })
      );
  }
  getShopName(): string | null {
    const n = localStorage.getItem(this.shopNameKey);
    if (n) return n;
    const p = this.jwtPayload();
    const name = p?.['shopName'] ?? p?.['ShopName'];
    return name != null ? String(name) : null;
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.shopIdKey);
    localStorage.removeItem(this.shopNameKey);
    clearHttpGetCache();
    this._shopId$.next(null);
    this._isLoggedIn$.next(false);
    this.router.navigate(['/auth/login']);
  }
}
