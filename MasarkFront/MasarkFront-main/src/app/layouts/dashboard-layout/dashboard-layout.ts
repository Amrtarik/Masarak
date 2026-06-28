import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthApiService } from '../../core/services/auth-api-service';
import { AuthStateService } from '../../core/services/auth-state-service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayoutComponent implements OnInit {
  private readonly authApi = inject(AuthApiService);
  private readonly authState = inject(AuthStateService);
  private readonly router = inject(Router);

  readonly userRole = this.authState.userRole;
  readonly userName = this.authState.user;
  // Default vector avatar to prevent 404 Not Found error
  readonly userAvatar = "data:image/svg+xml;charset=UTF-8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%2394a3b8'%3E%3Cpath d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z'/%3E%3C/svg%3E";
  readonly userRoleDisplay = this.authState.user;

  ngOnInit(): void {
    // هنا مستقبلاً هتعملي تواصل مع الـ API عشان تعرفي الـ Role الحقيقي
    // وبناءً عليه تحددي الـ userRole والبيانات اللي فوق
  }

  onAvatarError(event: any): void {
    // Fallback vector avatar if the image fails to load
    event.target.src = "data:image/svg+xml;charset=UTF-8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%2394a3b8'%3E%3Cpath d='M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z'/%3E%3C/svg%3E";
  }

  logout(): void {
    const refreshToken = this.authState.getRefreshToken();

    if (refreshToken) {
      this.authApi.logout(refreshToken).subscribe({
        next: () => {
          this.authState.clearAuth();
          this.router.navigate(['/auth/login']);
        },
        error: () => {
          this.authState.clearAuth();
          this.router.navigate(['/auth/login']);
        },
      });
      return;
    }

    this.authState.clearAuth();
    this.router.navigate(['/auth/login']);
  }
}
