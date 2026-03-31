import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LoaderService } from './services/loader.service';
import { ToastService } from './services/toast.service';
import { Chatbot } from './components/chatbot/chatbot';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, Chatbot],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  title  = signal('ReimbursementAngularApp');
  loader = inject(LoaderService);
  toast  = inject(ToastService);
}
