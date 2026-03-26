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
  role:         string = '';

  // No polling — notifications load on init + on bell click only
  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.loadNotifications();
  }

  ngOnDestroy() { /* no interval to clear */ }

  loadNotifications() {
    const r = this.role.toLowerCase();
    if (r !== 'employee' && r !== 'manager' && r !== 'admin' && r !== 'finance') return;
    this.loading = true;
    this.api.getMyNotifications().subscribe({
      next: (res: CreateNotificationResponseDto[]) => {
        const list = res ?? [];
        // Deduplicate by notificationId and sort newest first
        const seen = new Set<string>();
        const deduped = list.filter(n => {
          if (seen.has(n.notificationId)) return false;
          seen.add(n.notificationId);
          return true;
        });
        this.notifications = deduped.slice(0, 5);
        this.unreadCount   = deduped.filter(n => n.readStatus?.toLowerCase() === 'unread').length;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  toggleDropdown() {
    this.showDropdown = !this.showDropdown;
    // Refresh notifications each time the dropdown is opened
    if (this.showDropdown) {
      this.loadNotifications();
    }
  }

  markAsRead(n: CreateNotificationResponseDto, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (n.readStatus?.toLowerCase() !== 'unread') return;
    this.api.markAsRead(n.notificationId).subscribe({
      next: () => {
        n.readStatus = 'Read';
        this.unreadCount = Math.max(0, this.unreadCount - 1);
      },
      error: () => {}
    });
  }

  goToNotifications() {
    this.showDropdown = false;
    const r = this.role.toLowerCase();
    const routes: Record<string, string> = {
      employee: '/employee/notifications',
      manager:  '/manager/notifications',
      finance:  '/finance/notifications',
      admin:    '/admin/notifications',
    };
    this.router.navigate([routes[r] ?? '/login']);
  }

  accentClass(n: CreateNotificationResponseDto): string {
    if (n.reply && n.senderRole === 'Manager') return 'item-replied';
    if (n.readStatus?.toLowerCase() === 'unread') return 'item-unread';
    return 'item-read';
  }

  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.bell-wrapper')) this.showDropdown = false;
  }
}
