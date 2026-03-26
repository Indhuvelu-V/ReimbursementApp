import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { APIService } from '../../services/api.service';

@Component({
  selector: 'app-file-view-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ng-container *ngIf="visible">
      <div class="modal-backdrop" (click)="close()"></div>
      <div class="modal-box file-modal">
        <div class="modal-head">
          <h4>
            <i class="bi bi-paperclip me-2" style="color:var(--accent)"></i>Attached Documents
            <span class="doc-count">{{ urls.length }} file{{ urls.length !== 1 ? 's' : '' }}</span>
          </h4>
          <button class="modal-close-btn" (click)="close()"><i class="bi bi-x-lg"></i></button>
        </div>
        <div class="modal-body">
          <div *ngIf="!urls || urls.length === 0" class="no-docs">
            <i class="bi bi-inbox"></i>
            <p>No documents uploaded for this expense.</p>
          </div>
          <div *ngIf="urls && urls.length > 0" class="file-preview-grid">
            <div *ngFor="let url of urls; let i = index" class="file-preview-item-wrap">
              <!-- Image files: click to open fullscreen preview -->
              <div *ngIf="api.isImageUrl(url)" class="file-preview-item" (click)="openFullscreen(url)" role="button">
                <img [src]="api.resolveFileUrl(url)" alt="Document {{i+1}}" class="file-preview-img"
                     (error)="onImgError($event)" />
                <div class="file-preview-overlay">
                  <i class="bi bi-zoom-in"></i>
                </div>
                <div class="file-preview-label">
                  <i class="bi bi-image me-1"></i>Image {{ i + 1 }}
                </div>
              </div>
              <!-- Non-image files: open in new tab -->
              <a *ngIf="!api.isImageUrl(url)"
                 [href]="api.resolveFileUrl(url)"
                 target="_blank"
                 class="file-preview-item">
                <div class="file-preview-doc">
                  <i class="bi" [ngClass]="api.fileIcon(url)"></i>
                  <span class="file-ext-badge">{{ ext(url) | uppercase }}</span>
                </div>
                <div class="file-preview-overlay">
                  <i class="bi bi-box-arrow-up-right"></i>
                </div>
                <div class="file-preview-label">
                  <i class="bi bi-file-earmark me-1"></i>{{ api.fileName(url) }}
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </ng-container>

    <!-- Fullscreen image lightbox -->
    <ng-container *ngIf="fullscreenUrl">
      <div class="lightbox-backdrop" (click)="closeFullscreen()">
        <div class="lightbox-toolbar">
          <a [href]="api.resolveFileUrl(fullscreenUrl)" target="_blank" class="lightbox-btn" title="Open in new tab" (click)="$event.stopPropagation()">
            <i class="bi bi-box-arrow-up-right"></i>
          </a>
          <button class="lightbox-btn" (click)="closeFullscreen()" title="Close">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
        <img [src]="api.resolveFileUrl(fullscreenUrl)" class="lightbox-img" alt="Preview"
             (click)="$event.stopPropagation()" />
      </div>
    </ng-container>
  `,
  styles: [`
    .file-modal { max-width: 680px; }
    .doc-count {
      background: var(--accent-dim); color: var(--accent);
      font-size: 11px; font-weight: 700; padding: 2px 8px;
      border-radius: 99px; margin-left: 8px;
    }
    .no-docs {
      display: flex; flex-direction: column; align-items: center;
      gap: 10px; padding: 40px 20px; color: var(--text-faint);
    }
    .no-docs i { font-size: 36px; }
    .no-docs p { font-size: 14px; }

    .file-preview-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
      gap: 12px; margin-top: 4px;
    }
    .file-preview-item-wrap { position: relative; }

    .file-preview-item {
      display: block; position: relative;
      background: var(--surface2);
      border: 1.5px solid var(--border);
      border-radius: var(--r-md);
      overflow: hidden;
      cursor: pointer;
      text-decoration: none;
      transition: border-color 0.18s, box-shadow 0.18s, transform 0.18s;
    }
    .file-preview-item:hover {
      border-color: var(--accent);
      box-shadow: 0 0 0 3px var(--accent-dim);
      transform: translateY(-2px);
    }
    .file-preview-item:hover .file-preview-overlay { opacity: 1; }

    .file-preview-img {
      width: 100%; height: 120px; object-fit: cover; display: block;
    }
    .file-preview-doc {
      display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      height: 120px; gap: 8px; background: var(--surface3);
    }
    .file-preview-doc i { font-size: 32px; color: var(--accent); }
    .file-ext-badge {
      font-size: 10px; font-weight: 800; color: var(--text-muted);
      background: var(--surface2); padding: 2px 6px;
      border-radius: var(--r-xs); letter-spacing: 0.5px;
    }

    .file-preview-overlay {
      position: absolute; inset: 0; top: 0; height: calc(100% - 32px);
      background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      font-size: 22px; color: #fff;
      opacity: 0; transition: opacity 0.18s;
    }

    .file-preview-label {
      padding: 7px 10px; font-size: 11px; font-weight: 600;
      color: var(--text-muted); white-space: nowrap;
      overflow: hidden; text-overflow: ellipsis;
      background: var(--surface);
      border-top: 1px solid var(--border);
    }

    /* ── Lightbox ── */
    .lightbox-backdrop {
      position: fixed; inset: 0; z-index: 4000;
      background: rgba(0,0,0,0.92);
      display: flex; align-items: center; justify-content: center;
      animation: fadeInBg 0.2s ease;
      cursor: zoom-out;
    }
    .lightbox-toolbar {
      position: fixed; top: 16px; right: 20px;
      display: flex; gap: 10px; z-index: 4001;
    }
    .lightbox-btn {
      width: 38px; height: 38px;
      background: rgba(255,255,255,0.12); border: 1px solid rgba(255,255,255,0.2);
      border-radius: var(--r-sm); color: #fff; font-size: 16px;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; text-decoration: none;
      transition: background 0.15s;
    }
    .lightbox-btn:hover { background: rgba(255,255,255,0.25); }
    .lightbox-img {
      max-width: 92vw; max-height: 88vh;
      border-radius: var(--r-lg);
      box-shadow: 0 20px 80px rgba(0,0,0,0.8);
      cursor: default;
      animation: popIn 0.25s cubic-bezier(0.34,1.56,0.64,1);
    }
  `]
})
export class FileViewModal {
  @Input() visible = false;
  @Input() urls: string[] = [];
  @Output() closed = new EventEmitter<void>();

  fullscreenUrl: string | null = null;

  constructor(public api: APIService) {}

  close() { this.closed.emit(); }
  ext(url: string): string { return (url || '').split('.').pop()?.toLowerCase() ?? 'file'; }
  onImgError(e: any) { e.target.closest('.file-preview-item')?.classList.add('img-error'); e.target.style.display = 'none'; }

  openFullscreen(url: string) { this.fullscreenUrl = url; }
  closeFullscreen() { this.fullscreenUrl = null; }
}
