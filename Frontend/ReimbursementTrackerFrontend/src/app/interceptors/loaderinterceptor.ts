import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoaderService } from '../services/loader.service';

// URLs that should NOT trigger the global spinner (background polls, silent refreshes)
const SILENT_URLS = [
  '/Notification/GetMyNotifications',
];

export const loaderInterceptor: HttpInterceptorFn = (req, next) => {
  const loader = inject(LoaderService);

  // Skip spinner for silent background requests (prevents repeated spinner every 15s)
  const isSilent = SILENT_URLS.some(url => req.url.includes(url));
  if (isSilent) {
    return next(req);
  }

  loader.show();
  return next(req).pipe(finalize(() => loader.hide()));
};
