import { AUTH_TOKEN } from '../config.js';

export function getAuthToken() {
  if (!AUTH_TOKEN) {
    throw new Error('AUTH0_TOKEN is not set. Update it in config.js or pass via -e AUTH0_TOKEN=...');
  }

  return AUTH_TOKEN;
}

export function getAuthHeaders() {
  const token = getAuthToken();
  return {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}
