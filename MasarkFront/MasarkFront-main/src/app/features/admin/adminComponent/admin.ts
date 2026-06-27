import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminApiService } from '../../../core/services/admin-api-service';

interface Activity {
  fullName: string;
  role: string;
  createdAt: string;
}

interface DashboardData {
  totalStudents: number;
  activeTeachers: number;
  totalRevenue: number;
  recentActivities: Activity[];
}

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class AdminComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);

  data = signal<DashboardData | null>(null);
  isLoading = signal<boolean>(true);

  ngOnInit() {
    this.adminApi.getDashboard().subscribe({
      next: (res) => {
        this.data.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load admin dashboard', err);
        this.isLoading.set(false);
      }
    });
  }
}
