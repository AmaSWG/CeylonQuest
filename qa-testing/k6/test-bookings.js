import http from 'k6/http';
import { check, sleep } from 'k6';

// Configurable threshold via environment variable (default: 2000ms for hybrid DB WAN latency)
const SLA_MS = __ENV.SLA_MS || '2000';

// Load testing options & thresholds
export const options = {
  stages: [
    { duration: '10s', target: 20 },  // Smooth ramp-up to 20 users
    { duration: '15s', target: 40 },  // Sustained load: 40 concurrent users
    { duration: '5s', target: 0 },    // Ramp-down
  ],
  thresholds: {
    // 95% of requests must complete under target SLA
    http_req_duration: [`p(95)<${SLA_MS}`],
    // Error rate must be strictly under 1%
    http_req_failed: ['rate<0.01'],
  },
};

// Target Host (Defaults to Booking Service on port 5229 or Deployed Azure)
const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5229';
const LISTING_ID = __ENV.LISTING_ID || '00000000-0000-0000-0000-000000000001';

export default function () {
  const headers = { 'Content-Type': 'application/json' };

  // Endpoint 1: Rating summary for listing
  const summaryRes = http.get(
    `${BASE_URL}/api/bookings/reviews/${LISTING_ID}/summary?BookingType=Experience`,
    { headers }
  );
  check(summaryRes, {
    'summary status is 200': (r) => r.status === 200,
    'average rating returned': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.averageRating !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  // Endpoint 2: List reviews for listing
  const listReviewsRes = http.get(
    `${BASE_URL}/api/bookings/reviews/${LISTING_ID}?BookingType=Experience&Page=1&PageSize=10`,
    { headers }
  );
  check(listReviewsRes, {
    'list reviews status is 200': (r) => r.status === 200,
  });

  sleep(1);
}

import { generateHtmlReport } from './reporter.js';

export function handleSummary(data) {
  return {
    'qa-testing/k6/reports/bookings-report.html': generateHtmlReport(data, 'CeylonQuest - Bookings & Reviews Performance Report'),
  };
}
