// ================================================
//  src/app/features/admin/payments/payment-requests.component.ts
//  NEW FILE — admin/payments/ folder banao
//
//  Admin: Pending payment requests dekho aur approve/reject karo
// ================================================

import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface PaymentRequest {
  requestId: number;
  shopId: number;
  shopName: string;
  ownerName: string;
  mobileNumber: string;
  planType: string;
  amount: number;
  transactionId: string;
  payerName: string;
  payerMobile: string;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-payment-requests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-requests.component.html',
  styleUrls: ['./payment-requests.component.scss']
})
export class PaymentRequestsComponent implements OnInit {
  private http = inject(HttpClient);
  private cdr  = inject(ChangeDetectorRef);

  requests: PaymentRequest[] = [];
  loading  = true;
  error    = '';

  // Review modal state
  reviewingRequest: PaymentRequest | null = null;
  reviewAction: 'APPROVE' | 'REJECT' | '' = '';
  adminNotes  = '';
  submitting  = false;
  reviewError = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/payments/pending`).subscribe({
      next: res => {
        this.requests = res.data ?? [];
        this.loading  = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error   = 'Failed to load payment requests';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openApprove(req: PaymentRequest): void {
    this.reviewingRequest = req;
    this.reviewAction     = 'APPROVE';
    this.adminNotes       = '';
    this.reviewError      = '';
  }

  openReject(req: PaymentRequest): void {
    this.reviewingRequest = req;
    this.reviewAction     = 'REJECT';
    this.adminNotes       = '';
    this.reviewError      = '';
  }

  closeModal(): void {
    if (this.submitting) return;
    this.reviewingRequest = null;
    this.reviewAction     = '';
    this.reviewError      = '';
  }

  confirmReview(): void {
    if (!this.reviewingRequest || !this.reviewAction) return;
    if (this.reviewAction === 'REJECT' && !this.adminNotes.trim()) {
      this.reviewError = 'Rejection notes required';
      return;
    }

    this.submitting = true;
    this.reviewError = '';

    this.http.post<any>(
      `${environment.apiUrl}/payments/${this.reviewingRequest.requestId}/review`,
      { action: this.reviewAction, adminNotes: this.adminNotes }
    ).subscribe({
      next: () => {
        this.submitting = false;
        this.closeModal();
        this.load(); // Reload list
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.reviewError = err.error?.message || 'Action failed. Try again.';
        this.submitting  = false;
        this.cdr.detectChanges();
      }
    });
  }

  timeAgo(dateStr: string): string {
    const d    = new Date(dateStr);
    const diff = Math.floor((Date.now() - d.getTime()) / 1000);
    if (diff < 60)   return `${diff}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
  }
}