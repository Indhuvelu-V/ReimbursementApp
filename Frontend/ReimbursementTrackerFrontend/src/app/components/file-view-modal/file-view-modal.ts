import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { APIService } from '../../services/api.service';

@Component({
  selector: 'app-file-view-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-view-modal.html',
  styleUrls: ['./file-view-modal.css']
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
