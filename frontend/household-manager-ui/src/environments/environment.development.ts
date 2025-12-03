// Development environment configuration
export const environmentDev = {
  production: false,
  apiUrl: 'https://localhost:7047/api',
  auth0: {
    domain: 'household-manager-dev.eu.auth0.com',
    clientId: 'HAAmCfAQ0RmlG3zEHUPaY6NrvIbB85es', 
    authorizationParams: {
      redirect_uri: `${window.location.origin}/callback`,
      audience: 'https://household-manager-api',
      scope: 'openid profile email offline_access'
    },
    httpInterceptor: {
      allowedList: [
        {
          uri: 'https://localhost:7047/api/*',
          tokenOptions: {
            authorizationParams: {
              audience: 'https://household-manager-api',
              scope: 'openid profile email'
            }
          }
        }
      ]
    }
  }
};
