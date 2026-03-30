import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'http://localhost:5183',
  redirectUri: window.location.origin + '/auth/callback',
  clientId: 'sla-angular',
  responseType: 'code',
  scope: 'openid profile incidents_api',
  requireHttps: false,
  strictDiscoveryDocumentValidation: false,
  showDebugInformation: true,
};