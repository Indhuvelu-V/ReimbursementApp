import { Component, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TokenService } from '../../services/token.service';

interface ChatMessage {
  from: 'user' | 'bot';
  text: string;
  time: string;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.html',
  styleUrls: ['./chatbot.css']
})
export class Chatbot implements AfterViewChecked {
  @ViewChild('msgList') msgList!: ElementRef<HTMLDivElement>;

  isOpen   = false;
  input    = '';
  messages: ChatMessage[] = [];
  isTyping = false;

  constructor(private tokenService: TokenService, private router: Router) {}

  get isAuthPage(): boolean {
    const url = this.router.url;
    return url.includes('/login') || url.includes('/register') || url === '/';
  }

  private get role(): string {
    return (this.tokenService.getRoleFromToken() ?? 'Employee').toLowerCase();
  }

  ngAfterViewChecked() { this.scrollToBottom(); }

  toggle() {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.messages.length === 0) {
      const r = this.role;
      const welcomeMap: Record<string, string> = {
        employee: `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs an **Employee** you can create, submit, and track your expenses. Feel free to ask me anything!`,
        manager:  `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs a **Manager** you can review and approve expenses from your department. Feel free to ask me anything!`,
        finance:  `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs a **Finance** user you can process payments for your department's approved expenses. Feel free to ask me anything!`,
        admin:    `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs an **Admin** you have full access — users, categories, audit logs, and everything else. Feel free to ask me anything!`,
      };
      this.addBot(welcomeMap[r] ?? `Hi there! 👋 Feel free to ask me anything about the app!`);
    }
  }

  send() {
    const msg = this.input.trim();
    if (!msg) return;
    this.addUser(msg);
    this.input = '';
    this.isTyping = true;
    setTimeout(() => { this.isTyping = false; this.addBot(this.getReply(msg)); }, 600);
  }

  onKey(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  private addUser(text: string) { this.messages.push({ from: 'user', text, time: this.now() }); }
  private addBot(text: string)  { this.messages.push({ from: 'bot',  text, time: this.now() }); }
  private now(): string { return new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true }); }
  private scrollToBottom() { try { this.msgList?.nativeElement?.scrollTo({ top: 99999, behavior: 'smooth' }); } catch {} }

  formatText(text: string): string {
    return text.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>').replace(/\n/g, '<br>');
  }

  private getReply(msg: string): string {
    const q = msg.toLowerCase();
    const r = this.role;

    // ── Login / Register ──────────────────────────────────────────────────
    if (/login|sign in|how.*login/.test(q))
      return `**How to Login** 🔐\n\nEnter your **Username** and **Password** then click Sign In.\n\n💡 Multiple users can share the same username — login is validated by username + password combination.\n\nIf you don't have an account, click **Register here**.`;

    if (/register|sign up|create.*account|new.*account/.test(q))
      return `**How to Register** 📝\n\nFill in: User ID, Username, Email, Phone, Password, Role, Department, and Bank Details.\n\n⚠️ Only one Admin and one Finance user allowed in the system.\n⚠️ Only one Manager per department allowed.`;

    // ── Greetings ─────────────────────────────────────────────────────────
    if (/^(hi|hello|hey|good morning|good afternoon|good evening)/.test(q))
      return `Hello! 😊 Feel free to ask me anything about the app — I'm here to help!`;

    if (/thank|thanks/.test(q)) return `You're welcome! 😊`;
    if (/bye|goodbye|see you/.test(q)) return `Goodbye! Have a great day! 👋`;

    // ── Role ──────────────────────────────────────────────────────────────
    if (/what.*can i|my role|what.*i do|what.*access|my permission/.test(q)) {
      if (r === 'employee')
        return `**As an Employee you can:** 👤\n\n• Create 1 expense per month (current month, no future dates)\n• Attach 1 file per expense (max 10MB)\n• Edit Draft, Submitted, or Rejected expenses\n• Submit, Resubmit, Delete expenses\n• View your own expenses and status\n• Receive notifications\n• Update your profile and bank details`;
      if (r === 'manager')
        return `**As a Manager you can:** 👔\n\n• View submitted expenses from your department only\n• Approve or Reject submitted expenses\n• View your approval history\n• Send notifications to employees\n\n⚠️ Same department only. Cannot approve your own expenses.`;
      if (r === 'finance')
        return `**As a Finance user you can:** 💰\n\n• View approved expenses from your department\n• Complete payments with reference number and mode\n• View all payments with filters\n\n⚠️ Your department's expenses only.`;
      if (r === 'admin')
        return `**As an Admin you can:** 🛡️\n\n• All Expenses, Approvals, Payments\n• All Users — view, assign managers, update status\n• Audit Logs — filter, delete\n• Expense Categories — update limits\n• Policies, Notifications`;
    }

    // ── Create Expense ────────────────────────────────────────────────────
    if (/creat.*expense|new expense|add expense|how.*expense/.test(q))
      return `**Creating an Expense** 📝\n\n1. Go to **Expenses** in the navbar\n2. Click **Create Expense** tab\n3. Fill in Category, Amount, Date (current month, no future dates)\n4. Attach 1 document (optional, max 10MB)\n5. Click **Create Expense**\n\n⚠️ Only **1 expense per month**.\n⚠️ Only **1 file** per expense.`;

    // ── Submit ────────────────────────────────────────────────────────────
    if (/submit.*expense|how.*submit|after creat|next step/.test(q))
      return `**Submitting an Expense** 📤\n\n1. Go to **My Expenses** tab\n2. Find your **Draft** expense\n3. Click **Submit**\n\nStatus → **Submitted** and your department manager is notified.`;

    // ── Department routing ────────────────────────────────────────────────
    if (/department|dept|same.*dept|which.*manager|who.*approv/.test(q))
      return `**Department-Based Routing** 🏢\n\n• IT employee → IT Manager approves → IT Finance pays\n• HR employee → HR Manager approves → HR Finance pays\n\n⚠️ Manager can only approve their department's expenses.\n⚠️ Finance can only process their department's payments.`;

    // ── Approval ──────────────────────────────────────────────────────────
    if (/approv|how.*approv|pending approv|reject/.test(q)) {
      if (r === 'manager')
        return `**Approving Expenses** ✅\n\n1. Go to **Approvals** in the navbar\n2. See **Pending Approvals** from your department\n3. Review, add comment (optional)\n4. Click **Approve** or **Reject**\n5. Employee notified immediately\n\n⚠️ Same department only. Cannot approve your own.`;
      if (r === 'finance')
        return `Approvals are handled by Managers. Once approved, expenses appear in your Finance Dashboard for payment.`;
      return `**Approval Process** ✅\n\nAfter you submit → your department's Manager reviews → Approve or Reject → you get notified.\n\nIf Approved → Finance processes payment.\nIf Rejected → Edit and resubmit.`;
    }

    // ── Payment ───────────────────────────────────────────────────────────
    if (/payment|pay.*expense|complete.*payment|how.*paid|process.*payment/.test(q)) {
      if (r === 'finance')
        return `**Processing Payments** 💰\n\n1. Go to **Payments** in the navbar\n2. See approved expenses from your department\n3. Click **Complete Payment**\n4. Enter Reference Number and Payment Mode\n5. Confirm\n\nExpense → **Paid**, employee notified.\n\n⚠️ Your department's expenses only.`;
      return `**Payment Process** 💰\n\nAfter Manager approves → Finance processes payment → Status: **Paid** → You get notified.\n\nTrack in your **My Expenses** tab.`;
    }

    // ── File upload ───────────────────────────────────────────────────────
    if (/document|file|upload|attach|receipt|image|pdf|excel|word/.test(q))
      return `**File Upload** 📎\n\n• **1 file per expense** only\n• Supported: PNG, JPG, JPEG, PDF, Excel (XLSX/XLS), Word (DOCX)\n• Max size: **10MB**\n\nWhen editing, remove the existing file and upload a new one.`;

    // ── Profile / Bank ────────────────────────────────────────────────────
    if (/profile|bank|account.*number|ifsc|branch|update.*profile/.test(q))
      return `**Profile & Bank Details** 👤\n\nYour profile includes Username, Email, Phone, Bank Name, Account Number, IFSC Code, Branch Name.\n\nYou can update phone and bank details anytime.\n\n💡 Bank details are used by Finance for payment processing.`;

    // ── Manager assignment ────────────────────────────────────────────────
    if (/assign.*manager|reporting.*manager|who.*my manager/.test(q))
      return `**Reporting Manager** 👔\n\nWhen you register, the system auto-assigns the Manager from your department.\n\nAdmin can also manually assign a manager via **User Management → Assign Manager**.`;

    // ── Status ────────────────────────────────────────────────────────────
    if (/status|what.*draft|what.*submitted|what.*approved|what.*rejected|what.*paid/.test(q))
      return `**Expense Status Flow** 🔄\n\n• **Draft** — Created, not submitted\n• **Submitted** — Sent to manager\n• **Approved** — Manager approved, awaiting payment\n• **Rejected** — Edit and resubmit\n• **Paid** — Finance processed ✅\n\nDraft → Submitted → Approved → Paid\n           ↘ Rejected → Edit → Resubmit`;

    // ── Resubmit ──────────────────────────────────────────────────────────
    if (/resubmit|rejected.*expense|expense.*rejected/.test(q))
      return `**Resubmitting** 🔁\n\n1. Go to **My Expenses** tab\n2. Find the **Rejected** expense\n3. Click **Edit** — fix the issue\n4. Click **Resubmit**`;

    // ── Category / Limits ─────────────────────────────────────────────────
    if (/categor|limit|max.*amount|travel|food|medical|office/.test(q)) {
      const base = `**Categories & Limits** 📊\n\n• Travel — Max ₹5,000\n• Food — Max ₹1,000\n• Medical — Max ₹10,000\n• Office Supplies — Max ₹3,000`;
      return r === 'admin' ? `${base}\n\n💡 Update limits in Admin Dashboard → Expense Categories.` : `${base}\n\nMax limit shown when you select a category.`;
    }

    // ── Notifications ─────────────────────────────────────────────────────
    if (/notif|message|alert/.test(q))
      return `**Notifications** 🔔\n\nYou receive notifications when your expense is approved, rejected, or paid.\n\nManagers can also send messages to employees.\n\nView and reply in the **Notifications** section.`;

    // ── Audit Logs ────────────────────────────────────────────────────────
    if (/audit|log|activit|track/.test(q))
      return r === 'admin'
        ? `**Audit Logs** 📋 — Admin only\n\nFilter by username, date, or action type (created, submitted, approved, rejected, payment, deleted).\n\nBulk delete filtered logs available.`
        : `**Audit Logs** 📋\n\nOnly Admins can view audit logs. They record every action in the system.`;

    // ── Flow ──────────────────────────────────────────────────────────────
    if (/flow|process|how.*work|overview|workflow|step/.test(q))
      return `**Full Flow** 🔄\n\n1. Employee creates expense (1/month, 1 file) → Draft\n2. Employee submits → Submitted\n3. Department Manager approves or rejects\n4. Department Finance processes payment → Paid\n5. Employee notified at every step\n\nAll steps logged in Audit Logs (Admin only).`;

    // ── Policies ──────────────────────────────────────────────────────────
    if (/polic|rule|guideline/.test(q))
      return `**Policies** 📜\n\nView reimbursement policies in the **Policies** section.\n\n${r === 'admin' ? '💡 Update category limits in Expense Categories.' : 'Contact Admin to update limits.'}`;

    // ── Help ──────────────────────────────────────────────────────────────
    if (/help|what.*ask|what.*do|option/.test(q))
      return `Ask me anything! I'll answer based on your **${this.tokenService.getRoleFromToken()}** role. 😊\n\nTry: "How do I create an expense?", "How does approval work?", "What is my role?"`;

    return `I'm not sure about that. 🤔 Try asking about expenses, approvals, payments, or your role!`;
  }
}
