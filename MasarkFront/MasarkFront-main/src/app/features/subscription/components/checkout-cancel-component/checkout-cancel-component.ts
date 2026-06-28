import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-checkout-cancel',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-surface-50 flex items-center justify-center px-4">
      <div class="card p-10 max-w-md w-full text-center">
        <div class="w-20 h-20 rounded-full bg-surface-100 flex items-center justify-center mx-auto mb-6">
          <svg class="w-10 h-10 text-surface-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-surface-900 font-display mb-2">Checkout Cancelled</h1>
        <p class="text-surface-500 mb-8">
          No payment was taken. You can return to plans anytime to subscribe.
        </p>
        <a routerLink="/plans" class="btn-primary w-full btn-lg">
          View Plans
        </a>
        <a routerLink="/dashboard" class="btn-ghost w-full mt-3">
          Back to Dashboard
        </a>
      </div>
    </div>
  `,
})
export class CheckoutCancelComponent {}