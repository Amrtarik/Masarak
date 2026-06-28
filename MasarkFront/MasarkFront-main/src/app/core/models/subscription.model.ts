// ─── Subscription DTOs — mirrors SubscriptionDTOs.cs exactly ────────────────

export type PlanType = 'Monthly' | 'PerSubject' | 'FullCurriculum';
export type SubscriptionStatus = 'Pending' | 'Active' | 'Expired' | 'Cancelled';
export type ActivationMethod = 'Stripe' | 'AdminManual' | 'Cash';

export interface PlanDto {
  planId: number;
  name: string;
  description: string | null;
  type: PlanType;
  price: number;
  currency: string;
  durationDays: number;
  maxSubjects: number;
  hasAi: boolean;
  hasLiveClass: boolean;
  hasRecordings: boolean;
}

export interface SubscriptionDto {
  subscriptionId: number;
  userId: number;
  userFullName: string;
  planName: string;
  planType: PlanType;
  status: SubscriptionStatus;
  startDate: string;
  endDate: string;
  activationMethod: ActivationMethod;
  adminNote: string | null;
}

export interface SubscriptionStatusResponse {
  hasActiveSubscription: boolean;
  activeSubscription: SubscriptionDto | null;
}

export interface InitiateCheckoutRequest {
  planId: number;
  successUrl: string;
  cancelUrl: string;
}

export interface CheckoutResult {
  checkoutUrl: string;
  sessionId: string;
}

export interface AdminActivateRequest {
  studentUserId: number;
  planId: number;
  note?: string;
}

export interface AdminCancelRequest {
  reason: string;
}

export interface LinkStudentRequest {
  studentLinkageCode: string;
}

export interface ParentStudentLinkDto {
  linkId: number;
  parentUserId: number;
  studentUserId: number;
  studentFullName: string;
  linkedAt: string;
}

export interface LinkedStudentDto {
  studentUserId: number;
  fullName: string;
  email: string;
  hasActiveSubscription: boolean;
}

// ─── Pagination ──────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

// ─── Admin filter params ─────────────────────────────────────────────────────

export interface SubscriptionFilter {
  pageNumber: number;
  pageSize: number;
  status?: SubscriptionStatus;
}