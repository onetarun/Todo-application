import { InjectionToken } from '@angular/core';

// Relative by default: in development the dev server proxies /api to the backend (proxy.conf.json),
// so the browser only ever talks to one origin. Override the provider to point elsewhere.
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  factory: () => '/api',
});
