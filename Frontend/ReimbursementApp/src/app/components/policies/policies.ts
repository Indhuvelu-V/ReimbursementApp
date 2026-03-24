import { Component, OnInit } from '@angular/core';
import { Policy } from '../../models/policy.model';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './policies.html',
  styleUrls: ['./policies.css']
})
export class Policies implements OnInit {
  policies: Policy[] = [];

  constructor(
    private apiService: APIService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    const token = sessionStorage.getItem('token');
    if (token) this.loadPolicies();
  }

  loadPolicies() {
    this.apiService.getPolicies().subscribe({
      next:  (res) => { this.policies = res; },
      error: ()    => { this.toast.showError('Failed to load policies.'); }
    });
  }
}
