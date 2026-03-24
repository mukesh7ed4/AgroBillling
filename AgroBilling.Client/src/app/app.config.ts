import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { jwtInterceptorFn } from './core/interceptors/jwt.interceptor';
import { httpCacheInterceptorFn } from './core/interceptors/http-cache.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([jwtInterceptorFn, httpCacheInterceptorFn])
    ),
    provideAnimations()
  ]
};