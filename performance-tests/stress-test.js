import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { getConfig, getCloudConfig, getThresholds } from './config.js';
import { getAuthHeaders } from './utils/auth.js';

const cfg = getConfig();
const cloudCfg = getCloudConfig();

export const options = {
  scenarios: {
    // Scenario 1: Ramping to stress levels (max 80 VUs)
    ramping_stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 30 },   // Warm up to normal load
        { duration: '3m', target: 60 },   // Ramp to high load
        { duration: '2m', target: 80 },   // Push to stress level
        { duration: '2m', target: 60 },   // Recover to high load
        { duration: '1m', target: 0 },    // Cool down
      ],
      gracefulRampDown: '30s',
      startTime: '0s',
    },

    // Scenario 2: Constant arrival rate (runs in parallel, max 20 VUs)
    constant_rate_stress: {
      executor: 'constant-arrival-rate',
      duration: '3m',
      rate: 20,                   // 20 requests per second
      timeUnit: '1s',
      preAllocatedVUs: 5,
      maxVUs: 20,                 // 80 VUs (ramping) + 20 VUs (constant) = 100 total
      startTime: '5m',            // Start during recovery phase
    },
  },

  thresholds: getThresholds('stress'),

  // k6 Cloud configuration
  cloud: {
    projectID: cloudCfg.projectID,
    name: `${cloudCfg.name} - Stress Test - ${cfg.name}`,
  },
};

export default function () {
  const headers = getAuthHeaders();

  group('User Profile', function () {
    const res = http.get(`${cfg.apiUrl}/users/me`, { headers });
    check(res, {
      'profile not server error': (r) => r.status < 500,
      'profile responded': (r) => r.status !== 0,
    });
  });

  group('Households', function () {
    const res = http.get(`${cfg.apiUrl}/households`, { headers });
    check(res, {
      'households not server error': (r) => r.status < 500,
      'households responded': (r) => r.status !== 0,
    });
  });

  sleep(generateRandomDelay(0.3, 0.8));
}

// Helper: Generate random delay to simulate real user behavior
function generateRandomDelay(min, max) {
  return Math.random() * (max - min) + min;
}
