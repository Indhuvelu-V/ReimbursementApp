import { Component, Input, Output, EventEmitter, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { APIService } from '../../services/api.service';
import { renderAsync } from 'docx-preview';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-file-view-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-view-modal.html',
  styleUrls: ['./file-view-modal.css']
})
export class FileViewModal implements OnDestroy {
  @Input() visible = false;
  @Input() urls: string[] = [];
  @Output() closed = new EventEmitter<void>();

  fullscreenUrl: string | null = null;

  docPreviewUrl: SafeResourceUrl | null = null;
  docDirectUrl   = '';
  docPreviewName = '';
  docPreviewExt  = '';
  docPreviewLoading = false;
  docPreviewError   = false;
  previewMode: 'pdf' | 'docx' | 'xlsx' | 'none' | null = null;
  xlsxHtml = '';

  private blobUrls: string[] = [];

  constructor(public api: APIService, private sanitizer: DomSanitizer, private zone: NgZone) {}

  ngOnDestroy() { this.blobUrls.forEach(u => URL.revokeObjectURL(u)); }

  close() { this.closed.emit(); }
  ext(url: string) { return (url || '').split('.').pop()?.toLowerCase() ?? 'file'; }
  onImgError(e: any) { e.target.closest('.file-preview-item')?.classList.add('img-error'); e.target.style.display = 'none'; }
  openFullscreen(url: string) { this.fullscreenUrl = url; }
  closeFullscreen() { this.fullscreenUrl = null; }

  openDocPreview(url: string) {
    const resolved = this.api.resolveFileUrl(url);
    const fileExt  = this.ext(url);

    this.docPreviewName    = this.api.fileName(url);
    this.docDirectUrl      = resolved;
    this.docPreviewExt     = fileExt;
    this.docPreviewError   = false;
    this.docPreviewLoading = true;
    this.docPreviewUrl     = null;
    this.previewMode       = null;
    this.xlsxHtml          = '';

    if (fileExt === 'doc') {
      this.previewMode = 'none';
      this.docPreviewLoading = false;
      return;
    }

    // Use native fetch — static files don't need auth, avoids HttpClient interceptor issues
    fetch(resolved)
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.blob();
      })
      .then(blob => {
        if (fileExt === 'pdf') {
          const blobUrl = URL.createObjectURL(blob);
          this.blobUrls.push(blobUrl);
          this.zone.run(() => {
            this.docPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(blobUrl);
            this.previewMode = 'pdf';
            this.docPreviewLoading = false;
          });
        } else if (fileExt === 'docx') {
          this.zone.run(() => {
            this.previewMode = 'docx';
            this.docPreviewLoading = false;
          });
          // Retry until Angular renders the container
          this.zone.runOutsideAngular(() => {
            const tryRender = (attempts: number) => {
              const container = document.getElementById('docx-render-container');
              if (container) {
                renderAsync(blob, container, undefined, {
                  className: 'docx-page', inWrapper: true,
                  ignoreWidth: false, ignoreHeight: false,
                  ignoreFonts: false, breakPages: true, useBase64URL: true,
                }).catch(() => this.zone.run(() => { this.docPreviewError = true; }));
              } else if (attempts > 0) {
                setTimeout(() => tryRender(attempts - 1), 100);
              } else {
                this.zone.run(() => { this.docPreviewError = true; });
              }
            };
            setTimeout(() => tryRender(15), 80);
          });
        } else if (fileExt === 'xlsx' || fileExt === 'xls') {
          blob.arrayBuffer().then(buf => {
            try {
              const wb = XLSX.read(new Uint8Array(buf), { type: 'array' });
              const ws = wb.Sheets[wb.SheetNames[0]];
              const html = XLSX.utils.sheet_to_html(ws, { id: 'xlsx-table', editable: false });
              this.zone.run(() => {
                this.xlsxHtml = html;
                this.previewMode = 'xlsx';
                this.docPreviewLoading = false;
              });
            } catch {
              this.zone.run(() => { this.docPreviewError = true; this.docPreviewLoading = false; });
            }
          });
        }
      })
      .catch(() => {
        this.zone.run(() => { this.docPreviewLoading = false; this.docPreviewError = true; });
      });
  }

  closeDocPreview() {
    this.docPreviewUrl = null; this.docDirectUrl = '';
    this.docPreviewName = ''; this.docPreviewExt = '';
    this.docPreviewError = false; this.docPreviewLoading = false;
    this.previewMode = null; this.xlsxHtml = '';
    const c = document.getElementById('docx-render-container');
    if (c) c.innerHTML = '';
  }
}
