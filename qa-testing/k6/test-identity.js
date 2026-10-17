import http from 'k6/http';
import { check, sleep } from 'k6';
import { generateHtmlReport } from './reporter.js';

// Configurable SLA threshold (default: 3000ms for BCrypt verification)
const SLA_MS = __ENV.SLA_MS || '3000';

export const options = {
  stages: [
    { duration: '5s', target: 5 },    // Ramp-up to 5 users
    { duration: '15s', target: 10 },  // Sustained load: 10 concurrent users
    { duration: '5s', target: 0 },    // Ramp-down
  ],
  thresholds: {
    // 95% of requests must complete under target SLA
    http_req_duration: [`p(95)<${SLA_MS}`],
    // Error rate must be strictly under 1%
    http_req_failed: ['rate<0.01'],
  },
};

// Target Host (Defaults to Identity Service on port 5278 or Deployed Azure)
const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5278';

export default function () {
  const headers = { 'Content-Type': 'application/json' };

  // 1. Endpoint: Public Platform Reviews retrieval
  const reviewsRes = http.get(`${BASE_URL}/api/platform-reviews?page=1&pageSize=10`, { headers });
  check(reviewsRes, {
    'platform reviews status is 200': (r) => r.status === 200,
  });

  // 2. Endpoint: User Authentication (Login & BCrypt Verification)
  const loginPayload = JSON.stringify({
    email: 'piyumiyashodara@gmail.com',
    password: 'Piyumi@2003',
  });

  const loginRes = http.post(`${BASE_URL}/api/auth/login`, loginPayload, { headers });
  check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'jwt token returned': (r) => {
      try {
        const body = JSON.parse(r.body);
        const token = body.accessToken || body.token;
        return token !== undefined && token.length > 0;
      } catch (e) {
        return false;
      }
    },
  });

  sleep(1.5);
}

export function handleSummary(data) {
  return {
    'qa-testing/k6/reports/identity-report.html': generateHtmlReport(data, 'CeylonQuest - Identity Service Performance Report'),
  };
}
