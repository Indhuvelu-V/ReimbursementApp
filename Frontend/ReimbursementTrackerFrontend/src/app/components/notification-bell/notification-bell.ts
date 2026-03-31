import { Component, OnInit, OnDestroy, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { APIService } from '../../services/api.service';
import { TokenService } from '../../services/token.service';
import { CreateNotificationResponseDto } from '../../models/notification.model';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.html',
  styleUrls: ['./notification-bell.css']
})
export class NotificationBell implements OnInit, OnDestroy {
  api          = inject(APIService);
  router       = inject(Router);
  tokenService = inject(TokenService);

  notifications: CreateNotificationResponseDto[] = [];
  unreadCount   = 0;
  showDropdown  = false;
  loading       = false;
  role          = '';

  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.loadNotifications();
  }

  ngOnDestroy() {}

  loadNotifications() {
    const r = this.role.toLowerCase();
    if (!['employee', 'manager', 'admin', 'finance'].includes(r)) return;
    this.loading = true;

    if (r === 'manager') {
      // Manager: received + sent (with replies) — both from real DB state
      this.api.getMyNotifications().subscribe({
        next: (received) => {
          this.api.getSentNotifications().subscribe({
            next: (sent) => {
              // Sent notifications with a reply that are still "Unread" count for manager
              const combined = [...(received ?? []), ...(sent ?? [])];
              this.buildList(combined);
              this.loading = false;
            },
            error: () => { this.buildList(received ?? []); this.loading = false; }
          });
        },
        error: () => { this.loading = false; }
      });
    } else {
      this.api.getMyNotifications().subscribe({
        next: (res) => { this.buildList(res ?? []); this.loading = false; },
        error: () => { this.loading = false; }
      });
    }
  }

  private buildList(list: CreateNotificationResponseDto[]) {
    const seen = new Set<string>();
    const deduped = list
      .filter(n => { if (seen.has(n.notificationId)) return false; seen.add(n.notificationId); return true; })
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    this.notifications = deduped.slice(0, 5);
    this.unreadCount   = deduped.filter(n => n.readStatus?.toLowerCase() === 'unread').length;
  }

  toggleDropdown() {
    this.showDropdown = !this.showDropdown;
    if (this.showDropdown) this.loadNotifications();
  }

  markAsRead(n: CreateNotificationResponseDto, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (n.readStatus?.toLowerCase() !== 'unread') return;

    const currentUserId = this.tokenService.getUserIdFromToken() ?? '';
    const isSent = n.senderId === currentUserId;

    const api$ = isSent
      ? this.api.markSentAsRead(n.notificationId)
      : this.api.markAsRead(n.notificationId);

    api$.subscribe({
      next: () => {
        n.readStatus = 'Read';
        this.unreadCount = Math.max(0, this.unreadCount - 1);
      },
      error: () => {}
    });
  }

  markAllRead(event: Event) {
    event.stopPropagation();
    const currentUserId = this.tokenService.getUserIdFromToken() ?? '';
    const unread = this.notifications.filter(n => n.readStatus?.toLowerCase() === 'unread');
    unread.forEach(n => {
      const isSent = n.senderId === currentUserId;
      const api$ = isSent ? this.api.markSentAsRead(n.notificationId) : this.api.markAsRead(n.notificationId);
      api$.subscribe({ next: () => { n.readStatus = 'Read'; }, error: () => {} });
    });
    this.unreadCount = 0;
  }

  goToNotifications() {
    this.showDropdown = false;
    const routes: Record<string, string> = {
      employee: '/employee/notifications',
      manager:  '/manager/notifications',
      finance:  '/finance/notifications',
      admin:    '/admin/notifications',
    };
    this.router.navigate([routes[this.role.toLowerCase()] ?? '/login']);
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const utcStr = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const d = new Date(utcStr);
    if (isNaN(d.getTime())) return '';
    return d.toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
  }

  accentClass(n: CreateNotificationResponseDto): string {
    if (n.reply) return 'item-replied';
    if (n.readStatus?.toLowerCase() === 'unread') return 'item-unread';
    return 'item-read';
  }

  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    if (!(event.target as HTMLElement).closest('.bell-wrapper')) this.showDropdown = false;
  }
}
