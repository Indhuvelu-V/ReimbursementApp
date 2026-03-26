import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { ToastService } from '../../services/toast.service';
import { Policy } from '../../models/policy.model';

@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './policies.html',
  styleUrls: ['./policies.css']
})
export class Policies implements OnInit {
  policies: Policy[] = [];
  constructor(private api: APIService, private loader: LoaderService, private toast: ToastService) {}
  ngOnInit() { this.loadPolicies(); }
  loadPolicies() {
    this.loader.show();
    this.api.getPolicies().subscribe({
      next: (data) => { this.policies = data ?? []; this.loader.hide(); },
      error: () => { this.toast.showError('Failed to load policies.'); this.loader.hide(); }
    });
  }
}
