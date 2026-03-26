import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { ToastService } from '../../services/toast.service';
import { TokenService } from '../../services/token.service';
import { CreateNotificationResponseDto } from '../../models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notifications.html',
  styleUrls: ['./notifications.css']
})
export class Notifications implements OnInit {
  private api          = inject(APIService);
  private loader       = inject(LoaderService);
  private toast        = inject(ToastService);
  private tokenService = inject(TokenService);

  notifications: CreateNotificationResponseDto[] = [];
  filtered:      CreateNotificationResponseDto[] = [];
  replyTexts:    Record<string, string> = {};
  unreadCount    = 0;
  activeTab      = 'all';
  role           = '';
  newNotifUserId = '';
  newNotifMessage= '';

  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.loadNotifications();
  }

  isEmployee(): boolean { return this.role?.toLowerCase() === 'employee'; }
  isManager():  boolean { return this.role?.toLowerCase() === 'manager'; }

  loadNotifications() {
    this.loader.show();
    this.api.getMyNotifications().subscribe({
      next: (res: CreateNotificationResponseDto[]) => {
        this.notifications = res ?? [];
        this.unreadCount   = this.notifications.filter(n => n.readStatus === 'Unread').length;
        this.applyTab();
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load notifications.'); this.loader.hide(); }
    });
  }

  setTab(tab: string) { this.activeTab = tab; this.applyTab(); }

  applyTab() {
    switch (this.activeTab) {
      case 'unread':  this.filtered = this.notifications.filter(n => n.readStatus === 'Unread'); break;
      case 'replied': this.filtered = this.notifications.filter(n => n.reply && n.senderRole === 'Manager'); break;
      case 'read':    this.filtered = this.notifications.filter(n => n.readStatus === 'Read'); break;
      default:        this.filtered = [...this.notifications];
    }
  }

  replyToNotification(n: CreateNotificationResponseDto) {
    const reply = this.replyTexts[n.notificationId]?.trim();
    if (!reply) return;
    this.loader.show();
    this.api.replyNotification({ notificationId: n.notificationId, reply }).subscribe({
      next: () => { this.toast.show('Reply sent ✅'); this.replyTexts[n.notificationId] = ''; this.loadNotifications(); },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to send reply.'); this.loader.hide(); }
    });
  }

  markRead(n: CreateNotificationResponseDto) {
    this.loader.show();
    this.api.markAsRead(n.notificationId).subscribe({
      next: () => { this.toast.show('Marked as read'); this.loadNotifications(); },
      error: () => { this.toast.showError('Failed to mark as read.'); this.loader.hide(); }
    });
  }

  markAllRead() {
    const unread = this.notifications.filter(n => n.readStatus === 'Unread');
    if (!unread.length) return;
    this.loader.show();
    let done = 0;
    unread.forEach(n => {
      this.api.markAsRead(n.notificationId).subscribe({
        next: () => { done++; if (done === unread.length) { this.toast.show('All marked as read ✅'); this.loadNotifications(); } },
        error: () => { done++; if (done === unread.length) this.loader.hide(); }
      });
    });
  }

  sendNotification() {
    if (!this.newNotifUserId || !this.newNotifMessage) return;
    this.loader.show();
    this.api.createNotification({ userId: this.newNotifUserId, message: this.newNotifMessage }).subscribe({
      next: () => { this.toast.show('Notification sent ✅'); this.newNotifUserId = ''; this.newNotifMessage = ''; this.loader.hide(); },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to send notification.'); this.loader.hide(); }
    });
  }
}
