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

type Downtime = {
  id: string;
  environment: string;
  durationMinutes: number;
  customers: string;
  reason: string;
  occurredAt: string;
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

  downtimeCountMap: Record<string, number> = {};

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

  private normalizeEnv(value: string | null | undefined): string {
    return (value || '').trim().toLowerCase();
  }

  getDowntimeCount(environment: string): number {
    return this.downtimeCountMap[this.normalizeEnv(environment)] || 0;
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

  getEnvironmentIcon(environment: string): string {
    const env = (environment || '').trim().toLowerCase();

    if (env.includes('eclit')) return '🖥️';
    if (env.includes('paris')) return '🌍';
    if (env.includes('huawei')) return '📡';
    if (env.includes('ohio')) return '🏢';
    if (env.includes('uae')) return '🌐';
    if (env.includes('preprod')) return '🧪';

    return '🖥️';
  }

  private buildDowntimeCountMap(rows: Downtime[]) {
    const map: Record<string, number> = {};

    for (const row of rows || []) {
      const key = this.normalizeEnv(row.environment);
      if (!key) continue;
      map[key] = (map[key] || 0) + 1;
    }

    this.downtimeCountMap = map;
    this.cdr.detectChanges();
  }

  private refresh() {
    this.loading = true;
    this.error = '';
    this.downtimeCountMap = {};
    this.cdr.detectChanges();

    this.http.get<DashboardDto>('/api/Dashboard').subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
        this.cdr.detectChanges();

        this.http.get<Downtime[]>('/api/Downtimes').subscribe({
          next: (rows) => {
            this.buildDowntimeCountMap(rows ?? []);
          },
          error: (err) => {
            console.error('Downtime verisi alınamadı:', err);
            this.cdr.detectChanges();
          },
        });
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