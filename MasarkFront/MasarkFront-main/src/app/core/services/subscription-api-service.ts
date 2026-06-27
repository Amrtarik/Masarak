import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PlanDto,
  SubscriptionDto,
  SubscriptionStatusResponse,
  InitiateCheckoutRequest,
  CheckoutResult,
  AdminActivateRequest,
  PagedResult,
  SubscriptionFilter,
} from '../models/subscription.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SubscriptionApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/subscriptions`;
  private readonly planBase = `${environment.apiUrl}/plans`;

  // ── Plans (public) ──────────────────────────────────────────────────────────
  getPlans(): Observable<PlanDto[]> {
    return this.http.get<PlanDto[]>(this.planBase);
  }

  // ── Checkout ────────────────────────────────────────────────────────────────
  initiateCheckout(planId: number): Observable<CheckoutResult> {
    const body: InitiateCheckoutRequest = {
      planId,
      successUrl: environment.stripeSuccessUrl,
      cancelUrl: environment.stripeCancelUrl,
    };

    const token = localStorage.getItem('masarak_access_token');
    const headers = token
      ? new HttpHeaders({ Authorization: `Bearer ${token}` })
      : new HttpHeaders();

    return this.http.post<CheckoutResult>(`${this.base}/checkout`, body, { headers });
  }

  // ── User subscription ───────────────────────────────────────────────────────
  /** GET /api/subscriptions/me */
  getMySubscriptionStatus(): Observable<SubscriptionStatusResponse> {
    return this.http.get<SubscriptionStatusResponse>(`${this.base}/me`);
  }

  /** GET /api/subscriptions/me/history */
  getMyHistory(): Observable<SubscriptionDto[]> {
    return this.http.get<SubscriptionDto[]>(`${this.base}/me/history`);
  }

  // ── Admin ───────────────────────────────────────────────────────────────────
  /** GET /api/admin/subscriptions?pageNumber=&pageSize=&status= */
  getAllSubscriptions(filter: SubscriptionFilter): Observable<PagedResult<SubscriptionDto>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());
    if (filter.status) params = params.set('status', filter.status);
    return this.http.get<PagedResult<SubscriptionDto>>(
      `${environment.apiUrl}/admin/subscriptions`,
      { params },
    );
  }

  /** POST /api/subscriptions/admin/activate */
  adminActivate(req: AdminActivateRequest): Observable<SubscriptionDto> {
    return this.http.post<SubscriptionDto>(`${this.base}/admin/activate`, req);
  }

  /** POST /api/subscriptions/admin/cancel/{id} */
  adminCancel(subscriptionId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/admin/cancel/${subscriptionId}`, { reason });
  }
}
