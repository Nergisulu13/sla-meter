import { Component, OnInit } from '@angular/core';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  selector: 'app-logged-out',
  template: `<p>Yeniden giriş sayfasına yönlendiriliyor...</p>`
})
export class LoggedOutComponent implements OnInit {
  constructor(private auth: AuthService) {}

  ngOnInit(): void {
    const returnUrl = this.auth.getReturnUrl();
    this.auth.login(returnUrl || '/incidents', true);
  }
}