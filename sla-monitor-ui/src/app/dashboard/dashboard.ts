import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

type EnvironmentSlaCard = {
  environment: string;
  slaPercent: number;
  downtimeMinutes: number;
  allowedDowntimeMinutes: number;
  points: number;
};

type DashboardDto = {
  totalDowntimeCount: number;
  totalDowntimeMinutes: number;
  averageSlaPercent: number;
  averagePoints: number;
  averageAllowedDowntimeMinutes?: number;
  environmentCards: EnvironmentSlaCard[];
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  private http = inject(HttpClient);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  data?: DashboardDto;
  loading = false;
  error = '';

  ngOnInit() {
    this.refresh();

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        if (e.urlAfterRedirects.startsWith('/dashboard')) {
          this.refresh();
        }
      });
  }

  getSlaBarGradient(sla: number): string {
    if (sla >= 99.99) {
      return 'linear-gradient(90deg, #22c55e, #10b981)';
    }
    if (sla >= 99.95) {
      return 'linear-gradient(90deg, #f59e0b, #f97316)';
    }
    return 'linear-gradient(90deg, #ef4444, #dc2626)';
  }

  private normalizeEnv(value: string | null | undefined): string {
    return (value || '').trim().toLowerCase();
  }

  getEnvironmentImage(environment: string): string {
    const env = this.normalizeEnv(environment);

    if (env.includes('huawei')) return '/huawei.png';
    if (env.includes('paris')) return '/paris.png';
    if (env.includes('eclit')) return '/eclit.png';
    if (env.includes('ohio')) return '/ohio.png';
    if (env.includes('uae')) return '/dubai.png';
    if (env.includes('preprod ireland')) return '/ireland.png';

    return '/eclit.png';
  }

  onImageError(event: Event) {
    const img = event.target as HTMLImageElement;
    img.src = '/eclit.png';
  }

  private refresh() {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    this.http.get<DashboardDto>('/api/Dashboard').subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'Dashboard verisi alınamadı. API çalışıyor mu?';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}