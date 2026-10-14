import http from 'k6/http';
import { check } from 'k6';
import { sha256 } from 'k6/crypto';
import encoding from 'k6/encoding';
import { Counter, Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'http://host.docker.internal:5183').replace(/\/$/, '');
const cacheMode = (__ENV.CACHE_MODE || 'Redis').toLowerCase();
const email = __ENV.ADMIN_EMAIL || 'admin@acmetech.io';
const password = __ENV.ADMIN_PASSWORD || 'Admin@1234!';
const vus = Number(__ENV.VUS || 25);
const duration = __ENV.DURATION || '60s';
const servicesRequests = new Counter('services_requests');
const servicesSuccessRate = new Rate('services_success_rate');
const servicesLatency = new Trend('services_latency', true);

export const options = {
  scenarios: {
    services_browse: {
      executor: 'constant-vus',
      exec: 'getServices',
      vus,
      duration,
    },
  },
  thresholds: {
    services_success_rate: ['rate>0.99'],
    checks: ['rate>0.99'],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(50)', 'p(90)', 'p(95)', 'p(99)'],
};

export function setup() {
  const login = http.post(`${baseUrl}/api/auth/login`, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(login, { 'benchmark admin login succeeds': (response) => response.status === 200 });
  if (login.status !== 200) throw new Error(`Admin login failed (${login.status}): ${login.body}`);

  const accessToken = login.json('accessToken');
  const tokenPayload = JSON.parse(encodingDecode(accessToken.split('.')[1]));
  const organizationId = tokenPayload.orgId;
  const headers = { Authorization: `Bearer ${accessToken}` };
  const warm = http.get(`${baseUrl}/api/organizations/${organizationId}/services`, { headers });
  check(warm, { 'setup service-list request succeeds': (response) => response.status === 200 });
  if (warm.status !== 200) throw new Error(`Service setup request failed (${warm.status}): ${warm.body}`);

  return {
    accessToken,
    organizationId,
    expectedBodyHash: sha256(warm.body, 'hex'),
  };
}

export function getServices(data) {
  const response = http.get(`${baseUrl}/api/organizations/${data.organizationId}/services`, {
    headers: { Authorization: `Bearer ${data.accessToken}` },
    tags: { operation: 'organization_services', cache_mode: cacheMode },
  });
  servicesRequests.add(1);
  servicesSuccessRate.add(response.status === 200);
  servicesLatency.add(response.timings.duration);
  check(response, {
    'GET services returns 200': (result) => result.status === 200,
    'GET services returns the setup payload': (result) => sha256(result.body, 'hex') === data.expectedBodyHash,
  });
}

export function handleSummary(data) {
  const safeSummary = JSON.parse(JSON.stringify(data));
  if (safeSummary.setup_data) delete safeSummary.setup_data.accessToken;
  const path = `/results/${__ENV.RUN_NAME || 'redis-ab-summary'}.json`;
  return {
    stdout: `Benchmark summary saved to ${path}; bearer token removed from exported data.\n`,
    [path]: JSON.stringify(safeSummary, null, 2),
  };
}

function encodingDecode(value) {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
  return encoding.b64decode(padded, 'std', 's');
}
