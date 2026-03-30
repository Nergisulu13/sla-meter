import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { App } from './app/app';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/auth/auth.interceptor';

export default function bootstrap() {
  return bootstrapApplication(App, {
    providers: [
      provideRouter(routes),
      provideHttpClient(withInterceptors([authInterceptor]))
    ]
  });
}