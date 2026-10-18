import http from 'k6/http';
import { check, sleep } from 'k6';
import { generateHtmlReport } from './reporter.js';

// Configurable SLA threshold (default: 3000ms for Azure Cloud network roundtrip)
const SLA_MS = __ENV.SLA_MS || '3000';

export const options = {
  stages: [
    { duration: '5s', target: 10 },   // Smooth ramp-up to 10 users
    { duration: '15s', target: 20 },  // Sustained load: 20 concurrent users
    { duration: '5s', target: 0 },    // Ramp-down
  ],
  thresholds: {
    // 95% of requests must complete under target SLA
    http_req_duration: [`p(95)<${SLA_MS}`],
    // Error rate must be strictly under 1%
    http_req_failed: ['rate<0.01'],
  },
};

// Target Host (Defaults to Provider Catalog on port 5141 or Deployed Azure)
const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5141';

export default function () {
  const headers = { 'Content-Type': 'application/json' };

  // 1. Simple Endpoint: Public Activity & Experience Listings
  // Viva Explanation: Visitors browsing available tour activities (surfing, safaris, hiking)
  const activityRes = http.get(`${BASE_URL}/api/catalog/activity-listings/public`, { headers });
  check(activityRes, {
    'activity listings status is 200': (r) => r.status === 200,
    'activity listings returned array': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body);
      } catch (e) {
        return false;
      }
    },
  });

  // 2. Simple Endpoint: Public Restaurant & Dining Listings
  // Viva Explanation: Visitors browsing food places and culinary reservations
  const restaurantRes = http.get(`${BASE_URL}/api/catalog/restaurant-listings/public`, { headers });
  check(restaurantRes, {
    'restaurant listings status is 200': (r) => r.status === 200,
    'restaurant listings returned array': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body);
      } catch (e) {
        return false;
      }
    },
  });

  // 3. Simple Endpoint: Public Accommodation Listings
  // Viva Explanation: Tourists browsing hotels, villas, and homestays across Sri Lanka
  const accommodationRes = http.get(`${BASE_URL}/api/catalog/accommodation-listings/public`, { headers });
  check(accommodationRes, {
    'accommodation listings status is 200': (r) => r.status === 200,
    'accommodation listings returned array': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body);
      } catch (e) {
        return false;
      }
    },
  });

  // 4. Simple Endpoint: Unified Catalog Search
  // Viva Explanation: Visitors using global search across all categories (all listings discovery)
  const searchRes = http.get(`${BASE_URL}/api/catalog/search?category=all`, { headers });
  check(searchRes, {
    'unified search status is 200': (r) => r.status === 200,
    'unified search returned valid payload': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body !== null && (Array.isArray(body) || typeof body === 'object');
      } catch (e) {
        return false;
      }
    },
  });

  sleep(1.5);
}

export function handleSummary(data) {
  return {
    'qa-testing/k6/reports/catalog-report.html': generateHtmlReport(data, 'CeylonQuest - Provider Catalog Performance Report'),
  };
}
