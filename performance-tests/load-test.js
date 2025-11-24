import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { getConfig, getCloudConfig, getThresholds } from './config.js';
import { getAuthHeaders } from './utils/auth.js';

const cfg = getConfig();
const cloudCfg = getCloudConfig();

export const options = {
  scenarios: {
    ramping_load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 10 },  // Warm up to 10 VUs
        { duration: '2m', target: 30 },  // Ramp to 30 VUs
        { duration: '2m', target: 50 },  // Ramp to 50 VUs
        { duration: '1m', target: 0 },   // Cool down
      ],
      gracefulRampDown: '30s',
    },
  },

  thresholds: getThresholds('load'),

  // k6 Cloud configuration
  cloud: {
    projectID: cloudCfg.projectID,
    name: `${cloudCfg.name} - Load Test - ${cfg.name}`,
  },
};

export default function () {
  const headers = getAuthHeaders();
  let households = [];
  let rooms = [];

  group('User Profile', function () {
    const res = http.get(`${cfg.apiUrl}/users/me`, { headers });
    check(res, { 'profile OK': (r) => r.status === 200 });
  });
  sleep(generateRandomDelay(0.5, 1.5));

  group('Households List', function () {
    const res = http.get(`${cfg.apiUrl}/households`, { headers });
    check(res, { 'households OK': (r) => r.status === 200 });
    const householdsResponse = res.status === 200 ? JSON.parse(res.body) : { data: { items: [] } };
    households = householdsResponse.data?.items || [];
  });
  sleep(generateRandomDelay(0.5, 1.5));

  if (households.length > 0) {
    const householdId = households[0].id;

    group('Household Tasks', function () {
      const res = http.get(`${cfg.apiUrl}/households/${householdId}/tasks`, { headers });
      check(res, { 'tasks OK': (r) => r.status === 200 });
    });
    sleep(generateRandomDelay(0.5, 1.0));

    group('Household Rooms', function () {
      const res = http.get(`${cfg.apiUrl}/households/${householdId}/rooms`, { headers });
      check(res, { 'rooms OK': (r) => r.status === 200 });
      const roomsResponse = res.status === 200 ? JSON.parse(res.body) : { data: { items: [] } };
      rooms = roomsResponse.data?.items || [];
    });
    sleep(generateRandomDelay(0.5, 1.0));

    // Create task (2% of iterations to avoid data accumulation)
    if (Math.random() < 0.02 && rooms.length > 0) {
      group('Create Task', function () {
        const taskPayload = JSON.stringify({
          title: `K6 Load Test ${Date.now()}`,
          description: 'Task created by k6 load test',
          type: 0,  // Regular task
          priority: 2,  // Medium
          roomId: rooms[0].id,
          isActive: true,
          recurrenceRule: 'FREQ=WEEKLY;BYDAY=MO',  // Required for Regular tasks
        });
        const res = http.post(`${cfg.apiUrl}/households/${householdId}/tasks`, taskPayload, { headers });
        check(res, { 'create task OK': (r) => r.status === 200 || r.status === 201 });
      });
    }
  }

  sleep(generateRandomDelay(1.0, 3.0));
}

// Helper: Generate random delay to simulate real user behavior
function generateRandomDelay(min, max) {
  return Math.random() * (max - min) + min;
}
