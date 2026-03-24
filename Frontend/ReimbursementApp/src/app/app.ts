import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LoaderService } from './services/loader.service';
import { ToastService } from './services/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  title  = signal('ReimbursementAngularApp');
  loader = inject(LoaderService);
  toast  = inject(ToastService);
}
