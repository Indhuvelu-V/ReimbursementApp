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

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  toggle() {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.messages.length === 0) {
      const r = this.role;
      const welcomeMap: Record<string, string> = {
        employee: `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs an **Employee** you can create, submit, and track your expenses. Feel free to ask me anything!`,
        manager:  `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs a **Manager** you can review and approve or reject submitted expenses. Feel free to ask me anything!`,
        finance:  `Hi there! 👋 I'm your Reimbursement Tracker assistant.\n\nAs a **Finance** user you can process payments for approved expenses. Feel free to ask me anything!`,
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
    setTimeout(() => {
      this.isTyping = false;
      this.addBot(this.getReply(msg));
    }, 600);
  }

  onKey(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  private addUser(text: string) {
    this.messages.push({ from: 'user', text, time: this.now() });
  }

  private addBot(text: string) {
    this.messages.push({ from: 'bot', text, time: this.now() });
  }

  private now(): string {
    return new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  private scrollToBottom() {
    try { this.msgList?.nativeElement?.scrollTo({ top: 99999, behavior: 'smooth' }); } catch {}
  }

  formatText(text: string): string {
    return text
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }

  // ── Role-aware reply engine ───────────────────────────────────────────────

  private getReply(msg: string): string {
    const q = msg.toLowerCase();
    const r = this.role; // employee | manager | finance | admin

    // ── Greetings ─────────────────────────────────────────────────────────
    if (/^(hi|hello|hey|good morning|good afternoon|good evening)/.test(q))
      return `Hello! 😊 Feel free to ask me anything about the app — I'm here to help!`;

    if (/thank|thanks/.test(q))
      return `You're welcome! 😊 Feel free to ask if you have more questions.`;

    if (/bye|goodbye|see you/.test(q))
      return `Goodbye! Have a great day! �`;

    // ── "What can I do" / "my role" ───────────────────────────────────────
    if (/what.*can i|my role|what.*i do|what.*access|my permission|what.*i have/.test(q)) {
      if (r === 'employee')
        return `**As an Employee you can:** 👤\n\n• **Create** an expense (one per month)\n• **Edit** your Draft, Submitted, or Rejected expenses\n• **Submit** a Draft expense for manager approval\n• **Resubmit** a Rejected expense after editing\n• **Delete** Draft or Submitted expenses\n• **View** your own expenses and their status\n• **Attach** documents (images, PDF, Excel, Word)\n• **Receive notifications** when your expense is approved, rejected, or paid`;

      if (r === 'manager')
        return `**As a Manager you can:** �\n\n• **View all submitted expenses** from employees\n• **Approve** or **Reject** submitted expenses\n• **View approval history** — all your past decisions\n• **View all expenses** across the system\n• **Send notifications** to employees\n• **Filter** expenses by status, date, amount, username\n\n💡 You cannot approve your own expenses.`;

      if (r === 'finance')
        return `**As a Finance user you can:** �\n\n• **View all approved expenses** awaiting payment\n• **Complete payments** — enter reference number and payment mode\n• **View all payments** with filters (date, amount, status, username)\n• **View all expenses** across the system\n\n💡 You can only process expenses that have been approved by a Manager.`;

      if (r === 'admin')
        return `**As an Admin you can:** 🛡️\n\n• **All Expenses** — view, filter, manage every expense\n• **All Approvals** — view all approval decisions\n• **All Payments** — view all processed payments\n• **All Users** — view and search registered users\n• **Audit Logs** — full activity trail, filter by user/date/action, delete logs\n• **Expense Categories** — update max limits (Travel, Food, Medical, Office Supplies)\n• **Policies** — view reimbursement policies\n• **Notifications** — send messages to users`;
    }

    // ── Expense: Create ───────────────────────────────────────────────────
    if (/creat.*expense|new expense|add expense|how.*expense|first expense/.test(q)) {
      if (r === 'manager' || r === 'finance' || r === 'admin')
        return `**Creating an Expense** �\n\nYou can create expenses too (your role allows it).\n\n1. Go to **Expenses** in the navbar\n2. Click **Create Expense** tab\n3. Select **Category**, enter **Amount** and **Date** (current month only)\n4. Attach documents if needed\n5. Click **Create Expense**\n\nThe expense starts as **Draft**. Submit it for manager approval when ready.\n\n⚠️ Only one expense per month is allowed.`;

      return `**Creating an Expense** 📝\n\n1. Go to **Expenses** in the navbar\n2. Click the **Create Expense** tab\n3. Fill in:\n   • **Category** — Travel, Food, Medical, or Office Supplies\n   • **Amount** — within the category's max limit\n   • **Date** — must be within the current month\n   • **Documents** — attach receipts (optional)\n4. Click **Create Expense**\n\n⚠️ You can only create **one expense per month**.\n\nAfter creating, it's in **Draft** status — submit it when ready.`;
    }

    // ── Expense: Submit ───────────────────────────────────────────────────
    if (/submit.*expense|how.*submit|after creat|next step/.test(q)) {
      if (r === 'manager')
        return `As a Manager, you don't submit expenses for approval — you **approve** them.\n\nBut if you've created your own expense:\n1. Go to **My Expenses** tab\n2. Find the Draft expense\n3. Click **Submit**\n\nYour expense will go to another manager for approval (you can't approve your own).`;

      return `**Submitting an Expense** �\n\n1. Go to **My Expenses** tab\n2. Find your **Draft** expense\n3. Click the **Submit** button\n\nStatus changes to **Submitted** and your manager is notified.\n\n💡 You can edit a Draft before submitting.`;
    }

    // ── Approval ──────────────────────────────────────────────────────────
    if (/approv|how.*approv|pending approv|reject/.test(q)) {
      if (r === 'manager')
        return `**Approving Expenses** ✅ — This is your main task!\n\n1. Go to your **Manager Dashboard**\n2. You'll see **Pending Approvals** — all submitted expenses waiting for your review\n3. Click on an expense to view details and attached documents\n4. Click **Approve** or **Reject**\n5. The employee gets notified immediately\n\n💡 If approved → Finance will process the payment\n💡 If rejected → Employee can edit and resubmit\n\n⚠️ You cannot approve your own expenses.`;

      if (r === 'finance')
        return `**Approval Process** ✅\n\nApprovals are handled by Managers. Once a Manager approves an expense, it appears in your **Finance Dashboard** under **Awaiting Payment**.\n\nYou don't approve — you **pay** approved expenses.`;

      if (r === 'admin')
        return `**Approval Process** ✅\n\nAs Admin you can view all approvals in **All Approvals**.\n\nFlow: Employee submits → Manager approves/rejects → Finance pays\n\nYou can filter approvals by approver name, employee name, status, and date.`;

      return `**Approval Process** ✅\n\nAfter you submit an expense:\n1. Your **Manager** reviews it\n2. Manager clicks **Approve** or **Reject**\n3. You get a notification with the decision\n\nIf **Approved** → Finance will process your payment\nIf **Rejected** → Edit your expense and resubmit`;
    }

    // ── Payment ───────────────────────────────────────────────────────────
    if (/payment|pay.*expense|complete.*payment|how.*paid|process.*payment/.test(q)) {
      if (r === 'finance')
        return `**Processing Payments** 💰 — This is your main task!\n\n1. Go to your **Finance Dashboard**\n2. You'll see **Awaiting Payment** — all manager-approved expenses\n3. Click **Complete Payment** on an expense\n4. Enter:\n   • **Reference Number** (bank transfer ID, cheque number, etc.)\n   • **Payment Mode** (Bank Transfer, Cash, Cheque, etc.)\n5. Click **Confirm**\n\nThe expense status changes to **Paid** and the employee is notified.\n\nYou can also view **All Payments** with filters for date, amount, status, and username.`;

      if (r === 'manager')
        return `**Payment Process** 💰\n\nAfter you approve an expense, the **Finance** team processes the payment.\n\nYou can view payment status in **All Payments** section.\n\nFlow: You approve → Finance pays → Employee notified`;

      if (r === 'admin')
        return `**Payment Process** 💰\n\nFinance users process payments for approved expenses.\n\nAs Admin you can view all payments in **All Payments** with full filtering options.`;

      return `**Payment Process** 💰\n\nAfter your expense is approved by a Manager:\n1. **Finance** sees it under Awaiting Payment\n2. Finance processes the payment with a reference number\n3. Your expense status changes to **Paid**\n4. You receive a notification\n\nYou can track this in your **My Expenses** tab.`;
    }

    // ── Audit Logs ────────────────────────────────────────────────────────
    if (/audit|log|activit|track/.test(q)) {
      if (r === 'admin')
        return `**Audit Logs** 📋 — Admin only feature\n\n• Every action in the system is recorded here\n• Filter by **username**, **date range**, or **action type**\n• Delete individual logs or **bulk delete filtered logs**\n• See who did what, when, and on which expense\n\nGo to **Admin Dashboard → Audit Logs** to access them.`;

      return `**Audit Logs** 📋\n\nAudit logs track every action in the system. Only **Admins** can view them.\n\nThey record: who acted, what they did, when, and which expense was affected.`;
    }

    // ── Admin features ────────────────────────────────────────────────────
    if (/admin|dashboard.*admin|admin.*dashboard|all user|manage user/.test(q)) {
      if (r === 'admin')
        return `**Admin Dashboard** 🛡️\n\nYour available sections:\n\n• **All Users** — search and view all registered users by role or name\n• **Audit Logs** — full activity trail with filtering and bulk delete\n• **Expense Categories** — update max limits for Travel, Food, Medical, Office Supplies\n• **Policies** — view reimbursement policies\n• **All Expenses** — view every expense with filters\n• **All Approvals** — view all approval decisions\n• **All Payments** — view all processed payments`;

      return `The Admin Dashboard is only accessible to **Admin** users. It includes user management, audit logs, category limits, and full system visibility.`;
    }

    // ── Category / Limits ─────────────────────────────────────────────────
    if (/categor|limit|max.*amount|travel|food|medical|office/.test(q)) {
      const base = `**Expense Categories & Limits** 📊\n\n• **Travel** — Max ₹5,000\n• **Food** — Max ₹1,000\n• **Medical** — Max ₹10,000\n• **Office Supplies** — Max ₹3,000`;
      if (r === 'admin')
        return `${base}\n\n💡 As Admin, you can update these limits in **Admin Dashboard → Expense Categories**.`;
      return `${base}\n\nThe max limit is shown when you select a category. You cannot exceed it.`;
    }

    // ── Notifications ─────────────────────────────────────────────────────
    if (/notif|message|alert/.test(q)) {
      if (r === 'manager')
        return `**Notifications** 🔔\n\nAs a Manager you can:\n• Receive alerts when employees submit expenses\n• **Send notifications** to employees (e.g., to inform them about rejection reasons)\n• View all your notifications in the **Notifications** section`;

      return `**Notifications** 🔔\n\nYou receive notifications when:\n• Your expense is approved or rejected\n• Your payment is processed\n• A manager sends you a message\n\nView and reply to them in the **Notifications** section of the navbar.`;
    }

    // ── Status ────────────────────────────────────────────────────────────
    if (/status|what.*draft|what.*submitted|what.*approved|what.*rejected|what.*paid/.test(q))
      return `**Expense Status Flow** 🔄\n\n• **Draft** — Created, not submitted. Can edit or delete.\n• **Submitted** — Sent to manager for review.\n• **Approved** — Manager approved. Awaiting Finance payment.\n• **Rejected** — Manager rejected. Edit and resubmit.\n• **Paid** — Finance processed the payment. ✅\n\nDraft → Submitted → Approved → Paid\n           ↘ Rejected → Edit → Resubmit`;

    // ── Resubmit ──────────────────────────────────────────────────────────
    if (/resubmit|rejected.*expense|expense.*rejected/.test(q))
      return `**Resubmitting a Rejected Expense** 🔁\n\n1. Go to **My Expenses** tab\n2. Find the **Rejected** expense\n3. Click **Edit** — fix the issue (change amount, category, or documents)\n4. Click **Resubmit**\n\nThe manager will review it again.`;

    // ── Documents ─────────────────────────────────────────────────────────
    if (/document|file|upload|attach|receipt|image|pdf|excel|word/.test(q))
      return `**Documents & File Upload** 📎\n\nSupported formats: PNG, JPG, PDF, Excel (XLSX/XLS), Word (DOCX)\nMax size: **10MB per file**\n\n**Previewing:**\n• Images → fullscreen lightbox\n• PDF → inline browser viewer\n• Excel → rendered as table\n• DOCX → rendered inline\n• .doc (old Word) → download only\n\nWhen editing an expense, you can remove existing files and add new ones.`;

    // ── Policies ──────────────────────────────────────────────────────────
    if (/polic|rule|guideline/.test(q))
      return `**Reimbursement Policies** 📜\n\nPolicies are defined per category. View them in the **Policies** section.\n\n• Travel, Food, Medical, Office Supplies each have their own policy\n\n${r === 'admin' ? '💡 As Admin, you can update category limits which affect the policies.' : 'Contact your Admin if you need a limit updated.'}`;

    // ── Flow overview ─────────────────────────────────────────────────────
    if (/flow|process|how.*work|overview|workflow|step/.test(q))
      return `**Full Reimbursement Flow** 🔄\n\n1. **Employee** creates an expense → Draft\n2. **Employee** submits it → Submitted\n3. **Manager** approves or rejects\n   • Rejected → Employee edits & resubmits\n4. **Finance** processes payment → Paid\n5. **Employee** gets notified\n\nEvery step is logged in Audit Logs (Admin only).`;

    // ── Help ──────────────────────────────────────────────────────────────
    if (/help|what.*ask|what.*do|option/.test(q))
      return `Sure! You can ask me anything about the app. I'll answer based on your **${this.tokenService.getRoleFromToken()}** role. 😊`;

    // ── Fallback ──────────────────────────────────────────────────────────
    return `I'm not sure about that. 🤔 Feel free to ask anything — I'm happy to help!`;
  }
}
