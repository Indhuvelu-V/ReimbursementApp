import { Injectable, signal, inject } from '@angular/core';
import { LoaderService } from './loader.service';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private loader = inject(LoaderService);
  private _showToast = signal(false);
  private _message   = signal('');
  private _type      = signal<ToastType>('success');

  show(message: string, type: ToastType = 'success') {
    this._message.set(message); this._type.set(type); this._showToast.set(true);
    this.loader.forceHide();
    setTimeout(() => this._showToast.set(false), 3500);
  }
  showError(message: string)   { this.show(message, 'error'); }
  showWarning(message: string) { this.show(message, 'warning'); }
  showInfo(message: string)    { this.show(message, 'info'); }
  showToast() { return this._showToast(); }
  message()   { return this._message(); }
  type()      { return this._type(); }
}
