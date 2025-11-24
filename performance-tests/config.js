const envFileContent = open('../.env.k6');
const envFile = {};
envFileContent.split('\n').forEach(line => {
  const cleanLine = line.replace(/\r/, '');
  const match = cleanLine.match(/^([A-Z_][A-Z0-9_]*)=(.*)$/);
  if (match) {
    envFile[match[1]] = match[2].trim();
  }
});

export const AUTH_TOKEN = __ENV.AUTH0_TOKEN || envFile.AUTH0_TOKEN || '';

export const config = {
  localhost: {
    apiUrl: 'https://localhost:7047/api',
    name: 'Localhost Docker',
  },
  aws: {
    apiUrl: __ENV.AWS_API_URL || envFile.AWS_API_URL || '',
    name: 'AWS Production',
  },
  // k6 Cloud options
  cloud: {
    projectID: parseInt(__ENV.K6_CLOUD_PROJECT_ID || envFile.K6_CLOUD_PROJECT_ID) || null,
    name: 'Household Manager Performance Test',
  },
  // Thresholds (success criteria)
  thresholds: {
    smoke: {
      http_req_duration: ['p(95)<500'],   // 95% requests < 500ms
      http_req_failed: ['rate<0.01'],     // Error rate < 1%
      checks: ['rate>0.99'],              // 99% checks passed
    },
    load: {
      http_req_duration: ['p(95)<800', 'p(99)<1500'],
      http_req_failed: ['rate<0.05'],     // Error rate < 5%
      checks: ['rate>0.95'],              // 95% checks passed
    },
    stress: {
      http_req_duration: ['p(95)<1500', 'p(99)<3000'],
      http_req_failed: ['rate<0.1'],      // Error rate < 10%
      checks: ['rate>0.90'],              // 90% checks passed
    },
  },
};

export function getConfig() {
  const env = __ENV.ENV || 'localhost';
  return config[env];
}

export function getCloudConfig() {
  return config.cloud;
}

export function getThresholds(testType) {
  return config.thresholds[testType] || config.thresholds.load;
}
