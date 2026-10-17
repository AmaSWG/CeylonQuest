import http from 'k6/http';
import { check, sleep } from 'k6';
import { generateHtmlReport } from './reporter.js';

// Configurable SLA threshold (default: 2000ms for search & filtering)
const SLA_MS = __ENV.SLA_MS || '2000';

export const options = {
  stages: [
    { duration: '10s', target: 20 },  // Ramp-up to 20 users
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

// Target Host (Defaults to Provider Catalog on port 5141 / 8082 or Deployed Azure)
const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5141';

export default function () {
  const headers = { 'Content-Type': 'application/json' };

  // 1. Endpoint: Global Catalog Search with pagination
  const searchRes = http.get(`${BASE_URL}/api/catalog/search?q=&type=all&page=1&pageSize=8`, { headers });
  check(searchRes, {
    'catalog search status is 200': (r) => r.status === 200,
    'search results array returned': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body) || (body.items !== undefined);
      } catch (e) {
        return false;
      }
    },
  });

  // 2. Endpoint: Location-filtered Catalog Search
  const locRes = http.get(`${BASE_URL}/api/catalog/search?location=Colombo&pageSize=8`, { headers });
  check(locRes, {
    'location search status is 200': (r) => r.status === 200,
  });

  sleep(1);
}

export function handleSummary(data) {
  return {
    'qa-testing/k6/reports/catalog-report.html': generateHtmlReport(data, 'CeylonQuest - Provider Catalog Service Performance Report'),
  };
}
