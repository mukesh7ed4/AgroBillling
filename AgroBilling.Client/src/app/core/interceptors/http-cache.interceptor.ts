import {
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse
} from '@angular/common/http';
import { of, asyncScheduler } from 'rxjs';
import { tap, observeOn } from 'rxjs/operators';

const TTL_DEFAULT_MS = 180_000;
const TTL_DETAIL_MS = 300_000;

const cache = new Map<
  string,
  {
    body: unknown;
    headers: HttpResponse<unknown>['headers'];
    status: number;
    statusText: string;
    url: string | null;
    expires: number;
  }
>();

export function clearHttpGetCache(): void {
  cache.clear();
}

function cacheKey(req: HttpRequest<unknown>): string {
  const auth = req.headers.get('Authorization') ?? '';
  return `${req.method}|${req.urlWithParams}|${auth}`;
}

function ttlForUrl(url: string): number {
  const u = url.toLowerCase();
  if (u.includes('/detail/')) return TTL_DETAIL_MS;
  return TTL_DEFAULT_MS;
}

function skipGetCache(req: HttpRequest<unknown>): boolean {
  const u = req.url.toLowerCase();
  // Some screens must always show latest server state (after mutations).
  return (
    u.includes('/auth/') ||
    u.includes('/ledger') ||
    u.includes('/reports/') ||
    u.includes('/bills/detail/')
  );
}

function invalidateCacheForMutation(req: HttpRequest<unknown>): void {
  const u = req.url.toLowerCase();
  const keys = [...cache.keys()];
  for (const key of keys) {
    const urlPart = key.split('|')[1]?.toLowerCase() ?? '';
    if (urlPart.includes(u)) {
      cache.delete(key);
    }
  }
}

export const httpCacheInterceptorFn: HttpInterceptorFn = (req, next) => {

  if (req.method !== 'GET') {
    return next(req).pipe(
      tap(event => {
        if (event instanceof HttpResponse) {
          invalidateCacheForMutation(req);
        }
      })
    );
  }

  if (skipGetCache(req)) {
    return next(req);
  }

  const key = cacheKey(req);
  const now = Date.now();
  const hit = cache.get(key);

  // ✅ FIX: make cached response async
  if (hit && hit.expires > now) {
    return of(
      new HttpResponse({
        body: hit.body,
        headers: hit.headers,
        status: hit.status,
        statusText: hit.statusText,
        url: hit.url ?? undefined
      })
    ).pipe(observeOn(asyncScheduler)); // 🔥 IMPORTANT FIX
  }

  const ttl = ttlForUrl(req.url);

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse && event.status >= 200 && event.status < 300) {
        cache.set(key, {
          body: event.body,
          headers: event.headers,
          status: event.status,
          statusText: event.statusText,
          url: event.url,
          expires: Date.now() + ttl
        });
      }
    })
  );
};