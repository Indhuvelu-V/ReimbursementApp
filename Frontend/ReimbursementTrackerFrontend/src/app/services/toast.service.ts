import { Injectable, signal, inject } from '@angular/core';
import { LoaderService } from './loader.service';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private loader = inject(LoaderService);

  // ── Toast signals ──────────────────────────────────────────────────────────
  private _showToast = signal(false);
  private _message   = signal('');
  private _type      = signal<ToastType>('success');

  show(message: string, type: ToastType = 'success') {
    this._message.set(message);
    this._type.set(type);
    this._showToast.set(true);
    this.loader.forceHide();
    setTimeout(() => this._showToast.set(false), 3500);
  }
  showError(message: string)   { this.show(message, 'error'); }
  showWarning(message: string) { this.show(message, 'warning'); }
  showInfo(message: string)    { this.show(message, 'info'); }

  showToast() { return this._showToast(); }
  message()   { return this._message(); }
  type()      { return this._type(); }

  // ── Confirm modal signals ──────────────────────────────────────────────────
  private _showConfirm    = signal(false);
  private _confirmTitle   = signal('');
  private _confirmMessage = signal('');
  private _confirmResolve: ((v: boolean) => void) | null = null;

  /** Opens a confirm modal and returns a Promise<boolean> */
  confirm(title: string, message: string): Promise<boolean> {
    this._confirmTitle.set(title);
    this._confirmMessage.set(message);
    this._showConfirm.set(true);
    return new Promise(resolve => { this._confirmResolve = resolve; });
  }

  acceptConfirm() {
    this._showConfirm.set(false);
    this._confirmResolve?.(true);
    this._confirmResolve = null;
  }

  cancelConfirm() {
    this._showConfirm.set(false);
    this._confirmResolve?.(false);
    this._confirmResolve = null;
  }

  showConfirm()    { return this._showConfirm(); }
  confirmTitle()   { return this._confirmTitle(); }
  confirmMessage() { return this._confirmMessage(); }
}
