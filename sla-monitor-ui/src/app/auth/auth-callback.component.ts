import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  selector: 'app-auth-callback',
  template: `<p>Giriş yapılıyor...</p>`
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    const code = this.route.snapshot.queryParamMap.get('code');

    if (!code) {
      const returnUrl = this.auth.getReturnUrl();
      await this.router.navigateByUrl(returnUrl || '/incidents');
      return;
    }

    try {
      await this.auth.exchangeCodeForToken(code);

      const returnUrl = this.auth.getReturnUrl();
      this.auth.clearReturnUrl();

      await this.router.navigateByUrl(returnUrl || '/incidents');
    } catch (error) {
      console.error('Token alma hatası:', error);
      this.auth.clearTokens();

      const returnUrl = this.auth.getReturnUrl();
      this.auth.clearReturnUrl();

      this.auth.goToLoginPage(returnUrl || '/incidents');
    }
  }
}