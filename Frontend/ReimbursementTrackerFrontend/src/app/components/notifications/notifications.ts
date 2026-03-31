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

  // Inbox (received)
  received:     CreateNotificationResponseDto[] = [];
  filteredRecv: CreateNotificationResponseDto[] = [];
  unreadCount   = 0;
  recvTab       = 'all';   // all | unread | replied | read

  // Sent (outbox — manager sees replies here)
  sent:         CreateNotificationResponseDto[] = [];

  // Main view tab
  mainTab: 'inbox' | 'sent' = 'inbox';

  replyTexts: Record<string, string> = {};
  role        = '';

  // Send form (Manager only)
  newNotifUserId  = '';
  newNotifMessage = '';
  showSendForm    = false;

  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.loadAll();
  }

  isManager(): boolean { return this.role?.toLowerCase() === 'manager'; }
  isSystem(n: CreateNotificationResponseDto): boolean {
    return n.senderRole?.toLowerCase() === 'system';
  }

  loadAll() {
    this.loadReceived();
    if (this.isManager()) this.loadSent();
  }

  loadReceived() {
    this.loader.show();
    this.api.getMyNotifications().subscribe({
      next: (res) => {
        this.received   = res ?? [];
        this.unreadCount = this.received.filter(n => n.readStatus === 'Unread').length;
        this.applyRecvTab();
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load notifications.'); this.loader.hide(); }
    });
  }

  loadSent() {
    this.api.getSentNotifications().subscribe({
      next: (res) => { this.sent = res ?? []; },
      error: () => {}
    });
  }

  setMainTab(tab: 'inbox' | 'sent') {
    this.mainTab = tab;
    if (tab === 'sent' && this.isManager()) this.loadSent();
  }

  setRecvTab(tab: string) { this.recvTab = tab; this.applyRecvTab(); }

  applyRecvTab() {
    switch (this.recvTab) {
      case 'unread':  this.filteredRecv = this.received.filter(n => n.readStatus === 'Unread'); break;
      case 'replied': this.filteredRecv = this.received.filter(n => !!n.reply); break;
      case 'read':    this.filteredRecv = this.received.filter(n => n.readStatus === 'Read'); break;
      default:        this.filteredRecv = [...this.received];
    }
  }

  replyToNotification(n: CreateNotificationResponseDto) {
    const reply = this.replyTexts[n.notificationId]?.trim();
    if (!reply) return;
    this.loader.show();
    this.api.replyNotification({ notificationId: n.notificationId, reply }).subscribe({
      next: () => {
        this.toast.show('Reply sent ✅');
        this.replyTexts[n.notificationId] = '';
        this.loadReceived();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to send reply.'); this.loader.hide(); }
    });
  }

  markRead(n: CreateNotificationResponseDto) {
    this.loader.show();
    this.api.markAsRead(n.notificationId).subscribe({
      next: () => { this.toast.show('Marked as read'); this.loadReceived(); },
      error: () => { this.toast.showError('Failed to mark as read.'); this.loader.hide(); }
    });
  }

  markAllRead() {
    const unread = this.received.filter(n => n.readStatus === 'Unread');
    if (!unread.length) return;
    this.loader.show();
    let done = 0;
    unread.forEach(n => {
      this.api.markAsRead(n.notificationId).subscribe({
        next: () => { done++; if (done === unread.length) { this.toast.show('All marked as read ✅'); this.loadReceived(); } },
        error: () => { done++; if (done === unread.length) this.loader.hide(); }
      });
    });
  }

  sendNotification() {
    if (!this.newNotifUserId.trim() || !this.newNotifMessage.trim()) return;
    this.loader.show();
    this.api.createNotification({ userId: this.newNotifUserId.trim(), message: this.newNotifMessage.trim() }).subscribe({
      next: () => {
        this.toast.show('Notification sent ✅');
        this.newNotifUserId = ''; this.newNotifMessage = '';
        this.showSendForm = false;
        this.loadSent();
        this.loader.hide();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to send.'); this.loader.hide(); }
    });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const utcStr = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const d = new Date(utcStr);
    if (isNaN(d.getTime())) return '';
    return d.toLocaleString('en-IN', {
      day: '2-digit', month: 'short',
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }
}
