import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly authBaseUrl = 'http://localhost:5183';
  private readonly clientId = 'sla-angular';
  private readonly redirectUri = 'http://localhost:4200/auth/callback';
  private readonly scope = 'openid profile incidents_api';

  private readonly accessTokenKey = 'access_token';
  private readonly returnUrlKey = 'return_url';

  constructor(private http: HttpClient) {}

  login(returnUrl: string = '/incidents') {
    localStorage.setItem(this.returnUrlKey, returnUrl);

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

    localStorage.setItem(this.accessTokenKey, response.access_token);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  getReturnUrl(): string {
    return localStorage.getItem(this.returnUrlKey) || '/incidents';
  }

  clearReturnUrl() {
    localStorage.removeItem(this.returnUrlKey);
  }

  logout() {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.returnUrlKey);
  }
}