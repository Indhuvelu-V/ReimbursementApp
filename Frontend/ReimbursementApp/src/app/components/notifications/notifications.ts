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
  api    = inject(APIService);
  loader = inject(LoaderService);
  toast  = inject(ToastService);
  token  = inject(TokenService);
 
  notifications: CreateNotificationResponseDto[] = [];
  replyMap:      { [key: string]: string } = {};
  role:          string = '';
 
  activeFilter:  'all' | 'unread' | 'replied' | 'read' = 'all';
  expandedId:    string | null = null;
 
  ngOnInit() {
    this.role = this.token.getRoleFromToken() ?? '';
    this.loadNotifications();
  }
 
  // ── LOAD ────────────────────────────────────────────────
  loadNotifications() {
    this.loader.show();
    this.api.getMyNotifications().subscribe({
      next: (res: CreateNotificationResponseDto[]) => {
        this.notifications = res ?? [];
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load notifications. Please try again.');
        this.loader.hide();
      }
    });
  }
 
  // ── SEND REPLY ──────────────────────────────────────────
  // Only called for Manager-sent notifications (HTML already guards this).
  // On success: auto-calls markAsRead which now correctly saves to DB.
  sendReply(notificationId: string) {
    const reply = this.replyMap[notificationId]?.trim();
    if (!reply) {
      this.toast.showWarning('Please type a reply before sending.');
      return;
    }
    this.loader.show();
    this.api.replyNotification({ notificationId, reply }).subscribe({
      next: () => {
        this.toast.show('Reply sent successfully ✅');
        this.replyMap[notificationId] = '';
        this.expandedId = null;
        // markAsRead now calls the fixed backend endpoint that persists to DB
        this.api.markAsRead(notificationId).subscribe({
          next: () => this.loadNotifications(),
          error: () => this.loader.hide()
        });
      },
      error: () => {
        this.toast.showError('Failed to send reply. Please try again.');
        this.loader.hide();
      }
    });
  }
 
  // ── MARK AS READ ────────────────────────────────────────
  // Calls fixed backend endpoint — ReadStatus now actually saves to DB.
  markAsRead(notificationId: string) {
    this.loader.show();
    this.api.markAsRead(notificationId).subscribe({
      next: () => {
        this.toast.show('Marked as read ✅');
        this.loadNotifications();
      },
      error: () => {
        this.toast.showError('Failed to mark as read.');
        this.loader.hide();
      }
    });
  }
 
  // ── ACCENT CLASS ────────────────────────────────────────
  // green only when Manager-sent AND replied
  accentClass(n: CreateNotificationResponseDto): string {
    if (n.reply && n.senderRole === 'Manager') return 'accent-replied';
    if (n.readStatus === 'Unread') return 'accent-unread';
    return 'accent-read';
  }
 
  // ── UNREAD COUNT ────────────────────────────────────────
  get unreadCount(): number {
    return this.notifications.filter(n => n.readStatus === 'Unread').length;
  }
 
  // ── FILTER ──────────────────────────────────────────────
  get filteredNotifications(): CreateNotificationResponseDto[] {
    switch (this.activeFilter) {
      case 'unread':  return this.notifications.filter(n => n.readStatus === 'Unread' && !n.reply);
      case 'replied': return this.notifications.filter(n => !!n.reply);
      case 'read':    return this.notifications.filter(n => n.readStatus === 'Read'   && !n.reply);
      default:        return this.notifications;
    }
  }
 
  get unreadOnly():   number { return this.notifications.filter(n => n.readStatus === 'Unread' && !n.reply).length; }
  get repliedCount(): number { return this.notifications.filter(n => !!n.reply).length; }
  get readCount():    number { return this.notifications.filter(n => n.readStatus === 'Read'   && !n.reply).length; }
 
  setFilter(f: 'all' | 'unread' | 'replied' | 'read') {
    this.activeFilter = f;
  }
 
  // ── TOGGLE REPLY BOX ────────────────────────────────────
  toggleExpand(notificationId: string) {
    this.expandedId = this.expandedId === notificationId ? null : notificationId;
  }
 
  // ── MARK ALL READ ───────────────────────────────────────
  markAllRead() {
    const unread = this.notifications.filter(n => n.readStatus === 'Unread' && !n.reply);
    if (!unread.length) {
      this.toast.showWarning('No unread notifications to mark.');
      return;
    }
    this.loader.show();
    let done = 0;
    unread.forEach(n => {
      this.api.markAsRead(n.notificationId).subscribe({
        next: () => {
          done++;
          if (done === unread.length) {
            this.toast.show(`${done} notification(s) marked as read ✅`);
            this.loadNotifications();
          }
        },
        error: () => {
          done++;
          if (done === unread.length) this.loader.hide();
        }
      });
    });
  }
}
 