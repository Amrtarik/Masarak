import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-checkout-success',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-surface-50 flex items-center justify-center px-4">
      <div class="card p-10 max-w-md w-full text-center">
        <div class="w-20 h-20 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-6">
          <svg class="w-10 h-10 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-surface-900 font-display mb-2">Payment Successful!</h1>
        <p class="text-surface-500 mb-8">
          Your subscription is now active. Welcome to Masarak — let's start learning!
        </p>
        <a routerLink="/my-subscription" class="btn-primary w-full btn-lg">
          View My Subscription
        </a>
        <a routerLink="/dashboard" class="btn-ghost w-full mt-3">
          Go to Dashboard
        </a>
      </div>
    </div>
  `,
})
export class CheckoutSuccessComponent {}