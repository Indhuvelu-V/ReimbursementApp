import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { LoaderService } from '../services/loader.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast  = inject(ToastService);
  const router = inject(Router);
  const loader = inject(LoaderService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      loader.hide(); // always hide spinner on error

      const message = error.error?.message || error.message || 'An error occurred.';

      switch (error.status) {
        case 400:
          toast.showError(`Bad Request (400): ${message}`);
          break;

        case 401:
          toast.showError('Unauthorized (401): Session expired. Please login again.');
          sessionStorage.removeItem('token');
          router.navigate(['/login']);
          break;

        case 403:
          toast.showError('Forbidden (403): You do not have permission to perform this action.');
          break;

        case 404:
          toast.showError(`Not Found (404): ${message}`);
          break;

        case 500:
          toast.showError(`Server Error (500): ${message}`);
          break;

        case 0:
          toast.showError('Network Error: Cannot reach the server. Check your connection.');
          break;

        default:
          toast.showError(`Error (${error.status}): ${message}`);
          break;
      }

      return throwError(() => error);
    })
  );
};
