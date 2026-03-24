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
  role:         string = '';
 
  private refreshInterval: any;
 
  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.loadNotifications();
    // Auto-refresh every 15s for all
    const r = this.role.toLowerCase();
    if (r === 'employee' || r === 'manager' || r === 'admin' || r === 'finance') {
      this.refreshInterval = setInterval(() => this.loadNotifications(), 15000);
    }
  }
 
  ngOnDestroy() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }
 
  loadNotifications() {
    const r = this.role.toLowerCase();
    if (r !== 'employee' && r !== 'manager' && r !== 'admin' && r !== 'finance' ) return;
 
    // UPDATED: calls /Notification/my — works for both all.
    // Old endpoint /Notification/employee only allowed Employee role.
    this.api.getMyNotifications().subscribe({
      next: (res: CreateNotificationResponseDto[]) => {
        const list = res ?? [];
        this.notifications = list.slice(0, 5);
        this.unreadCount   = list.filter(n => n.readStatus?.toLowerCase() === 'unread').length;
      },
      error: () => {}
    });
  }
 
  toggleDropdown() { this.showDropdown = !this.showDropdown; }
 
  goToNotifications() {
    this.showDropdown = false;
    const r = this.role.toLowerCase();
    const routes: Record<string, string> = {
      employee: '/employee/notifications',
      manager:  '/manager/notifications',
      finance:  '/finance/notifications',
      admin:    '/admin/notifications',
    };
    const route = routes[r] ?? '/login';
    this.router.navigate([route]);
  }
 
  // UPDATED: green accent only for Manager-sent notifications that have a reply.
  // System notifications (Approved/Paid) never show green — they use blue/gray only.
  accentClass(n: CreateNotificationResponseDto): string {
    if (n.reply && n.senderRole === 'Manager') return 'item-replied';
    if (n.readStatus === 'Unread') return 'item-unread';
    return 'item-read';
  }
 
  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.bell-wrapper')) {
      this.showDropdown = false;
    }
  }
}
 