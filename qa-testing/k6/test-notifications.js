import http from 'k6/http';
import { check, sleep } from 'k6';

const SLA_MS = __ENV.SLA_MS || '2000';

// Load testing options & thresholds
export const options = {
  stages: [
    { duration: '10s', target: 20 },  // Smooth ramp-up to 20 virtual users
    { duration: '15s', target: 40 },  // Peak load: 40 concurrent users
    { duration: '5s', target: 0 },    // Ramp-down
  ],
  thresholds: {
    // 95% of requests must complete under target SLA
    http_req_duration: [`p(95)<${SLA_MS}`],
    // Error rate must be strictly under 1%
    http_req_failed: ['rate<0.01'],
  },
};

// Target Host (Defaults to Localhost or Deployed Azure via environment variable)
const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5278';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      ...(AUTH_TOKEN ? { Authorization: `Bearer ${AUTH_TOKEN}` } : {}),
    },
  };

  // Endpoint 1: Polling unread notification count
  const unreadRes = http.get(`${BASE_URL}/api/notifications/unread-count`, params);
  check(unreadRes, {
    'unread count status is 200': (r) => r.status === 200,
  });

  // Endpoint 2: Notification retrieval with pagination
  const listRes = http.get(`${BASE_URL}/api/notifications?page=1&pageSize=10`, params);
  check(listRes, {
    'list notifications status is 200': (r) => r.status === 200,
  });

  sleep(1);
}

import { generateHtmlReport } from './reporter.js';

export function handleSummary(data) {
  return {
    'qa-testing/k6/reports/notifications-report.html': generateHtmlReport(data, 'CeylonQuest - Notifications Performance Report'),
  };
}
