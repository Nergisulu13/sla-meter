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
  incidentCount: number;
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

  userRole = '';
  userTenant = '';

  ngOnInit() {
    this.loadUserInfo();
    this.refresh();

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        if (e.urlAfterRedirects.startsWith('/dashboard')) {
          this.loadUserInfo();
          this.refresh();
        }
      });
  }

  private loadUserInfo() {
    const token = localStorage.getItem('access_token');
    this.userRole = '';
    this.userTenant = '';

    if (!token) return;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      this.userRole =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        payload['role'] ||
        '';

      this.userTenant = payload['tenant_name'] || '';
    } catch (e) {
      console.error('Token parse edilemedi', e);
    }
  }

  getVisibleCards(): EnvironmentSlaCard[] {
    if (!this.data?.environmentCards?.length) return [];

    if (!this.userTenant || this.userTenant === 'ALL') {
      return this.data.environmentCards;
    }

    return this.data.environmentCards.filter(
      (x) => this.normalizeEnv(x.environment) === this.normalizeEnv(this.userTenant)
    );
  }

  getVisibleAverageSla(): number {
    const cards = this.getVisibleCards();
    if (!cards.length) return 0;

    const total = cards.reduce((sum, x) => sum + Number(x.slaPercent || 0), 0);
    return +(total / cards.length).toFixed(2);
  }

  getVisibleAveragePoints(): number {
    const cards = this.getVisibleCards();
    if (!cards.length) return 0;

    const total = cards.reduce((sum, x) => sum + Number(x.points || 0), 0);
    return +(total / cards.length).toFixed(2);
  }

  getVisibleDowntimeMinutes(): number {
    const cards = this.getVisibleCards();
    if (!cards.length) return 0;

    return cards.reduce((sum, x) => sum + Number(x.downtimeMinutes || 0), 0);
  }

  getVisibleDowntimeCount(): number {
    const cards = this.getVisibleCards();
    if (!cards.length) return 0;

    return cards.reduce((sum, x) => sum + Number(x.incidentCount || 0), 0);
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

  trackByEnvironment(_: number, item: EnvironmentSlaCard): string {
    return item.environment;
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