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

  login(returnUrl: string = '/incidents', forceLogin: boolean = false) {
    localStorage.setItem('return_url', returnUrl);

    let authUrl =
      `${this.authBaseUrl}/connect/authorize` +
      `?client_id=${encodeURIComponent(this.clientId)}` +
      `&response_type=code` +
      `&scope=${encodeURIComponent(this.scope)}` +
      `&redirect_uri=${encodeURIComponent(this.redirectUri)}`;

    if (forceLogin) {
      authUrl += `&prompt=login&max_age=0`;
    }

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

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  getReturnUrl(): string {
    return localStorage.getItem('return_url') || '/incidents';
  }

  setReturnUrl(url: string) {
    localStorage.setItem('return_url', url);
  }

  clearReturnUrl() {
    localStorage.removeItem('return_url');
  }

  clearTokens() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  private parseJwt(token: string): any | null {
    try {
      const payload = token.split('.')[1];
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(
        normalized.length + (4 - normalized.length % 4) % 4,
        '='
      );
      const decoded = atob(padded);
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  isTokenExpired(token: string | null, offsetSeconds: number = 10): boolean {
    if (!token) return true;

    const payload = this.parseJwt(token);
    if (!payload?.exp) return true;

    const now = Math.floor(Date.now() / 1000);
    return payload.exp <= now + offsetSeconds;
  }

  isAccessTokenValid(): boolean {
    const token = this.getAccessToken();
    return !!token && !this.isTokenExpired(token);
  }

  hasRefreshToken(): boolean {
    return !!this.getRefreshToken();
  }

  hasSession(): boolean {
    return this.isAccessTokenValid() || this.hasRefreshToken();
  }

  isLoggedIn(): boolean {
    return this.hasSession();
  }

  forceLogin(returnUrl: string = '/incidents') {
    this.clearTokens();
    this.setReturnUrl(returnUrl);
    this.login(returnUrl, true);
  }

  logout(returnUrl: string = '/incidents') {
    this.clearTokens();
    this.setReturnUrl(returnUrl);

    const afterLogoutUrl = `${window.location.origin}/logged-out`;

    const logoutUrl =
      `${this.authBaseUrl}/account/logout` +
      `?returnUrl=${encodeURIComponent(afterLogoutUrl)}`;

    window.location.href = logoutUrl;
  }
}