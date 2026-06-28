import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SubscriptionApiService } from '../../../../core/services/subscription-api-service';
import { AuthStateService } from '../../../../core/services/auth-state-service';
import { PlanDto } from '../../../../core/models/subscription.model';

@Component({
  selector: 'plans-component',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './plans-component.html',
})
export class PlansComponent implements OnInit {
  private subApi = inject(SubscriptionApiService);
  readonly auth = inject(AuthStateService);
  private router = inject(Router);

  plans = signal<PlanDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  checkingOutPlanId = signal<number | null>(null);
  checkoutError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.subApi.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Failed to load plans.');
        this.loading.set(false);
      },
    });
  }

  onSelectPlan(plan: PlanDto): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/my-subscription']);
      return;
    }

    this.checkingOutPlanId.set(plan.planId);
    this.checkoutError.set(null);

    this.subApi.initiateCheckout(plan.planId).subscribe({
      next: (result) => {
        this.checkingOutPlanId.set(null);
        window.location.href = result.checkoutUrl;
      },
      error: (err) => {
        this.checkingOutPlanId.set(null);
        this.checkoutError.set(
          err?.error?.message ?? 'Could not start checkout. Please try again.',
        );
      },
    });
  }
}
