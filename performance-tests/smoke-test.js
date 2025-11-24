import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { getConfig, getCloudConfig, getThresholds } from './config.js';
import { getAuthHeaders } from './utils/auth.js';

const cfg = getConfig();
const cloudCfg = getCloudConfig();

export const options = {
  scenarios: {
    smoke_test: {
      executor: 'constant-vus',
      vus: 2,
      duration: '30s',
    },
  },

  thresholds: getThresholds('smoke'),

  // k6 Cloud configuration
  cloud: {
    projectID: cloudCfg.projectID,
    name: `${cloudCfg.name} - Smoke Test - ${cfg.name}`,
  },
};

export default function () {
  const headers = getAuthHeaders();

  group('User Profile', function () {
    const res = http.get(`${cfg.apiUrl}/users/me`, { headers });
    check(res, {
      'profile status 200': (r) => r.status === 200,
      'profile has data': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.data && body.data.user && body.data.user.email !== undefined;
        } catch (e) {
          return false;
        }
      },
    });
  });
  sleep(generateRandomDelay(0.5, 1.5));

  group('Households', function () {
    const res = http.get(`${cfg.apiUrl}/households`, { headers });
    check(res, {
      'households status 200': (r) => r.status === 200,
      'households has data': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.data !== undefined && Array.isArray(body.data.items);
        } catch (e) {
          return false;
        }
      },
    });
  });
  sleep(generateRandomDelay(0.5, 1.5));
}

// Helper: Generate random delay to simulate real user behavior
function generateRandomDelay(min, max) {
  return Math.random() * (max - min) + min;
}
