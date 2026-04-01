import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);

  private readonly authBaseUrl = 'http://localhost:5183';
  private readonly clientId = 'sla-angular';
  private readonly redirectUri = 'http://localhost:4200/auth/callback';
  private readonly scope = 'openid profile incidents_api offline_access';

  login(returnUrl: string = '/incidents') {
    localStorage.setItem('return_url', returnUrl);

    const authUrl =
      `${this.authBaseUrl}/connect/authorize` +
      `?client_id=${encodeURIComponent(this.clientId)}` +
      `&response_type=code` +
      `&scope=${encodeURIComponent(this.scope)}` +
      `&redirect_uri=${encodeURIComponent(this.redirectUri)}`;

    window.location.href = authUrl;
  }

  async exchangeCodeForToken(code: string): Promise<void> {
    const body = new URLSearchParams();
    body.set('grant_type', 'authorization_code');
    body.set('client_id', this.clientId);
    body.set('code', code);
    body.set('redirect_uri', this.redirectUri);

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    const response = await firstValueFrom(
      this.http.post<any>(
        `${this.authBaseUrl}/connect/token`,
        body.toString(),
        { headers }
      )
    );

    if (!response?.access_token) {
      throw new Error('Access token alınamadı.');
    }

    localStorage.setItem('access_token', response.access_token);

    if (response.refresh_token) {
      localStorage.setItem('refresh_token', response.refresh_token);
    }
  }

  refreshToken(): Observable<any> {
    const refreshToken = localStorage.getItem('refresh_token');

    const body = new URLSearchParams();
    body.set('grant_type', 'refresh_token');
    body.set('refresh_token', refreshToken ?? '');
    body.set('client_id', this.clientId);

    return this.http.post<any>(
      `${this.authBaseUrl}/connect/token`,
      body.toString(),
      {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        }
      }
    );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('access_token');
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  getReturnUrl(): string {
    return localStorage.getItem('return_url') || '/incidents';
  }

  clearReturnUrl() {
    localStorage.removeItem('return_url');
  }

  clearTokens() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  logout() {
   this.clearTokens();
   this.clearReturnUrl();
   window.location.href = `${this.authBaseUrl}/account/logout`;
}
}