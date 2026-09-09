import http from 'k6/http';
import { check } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

// summary: the honest mix — 80% browse, 15% basket work, 5% full checkout.
// A shop's load is mostly looking; the writes are rare and precious.
export const options = {
  scenarios: {
    shop: {
      executor: 'constant-arrival-rate',
      rate: 2000, timeUnit: '1s', duration: '2m',
      preAllocatedVUs: 300, maxVUs: 3000,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<50'],
    http_req_failed: ['rate<0.001'],
  },
};

const BASE = 'http://localhost:5100';

export function setup() {
  const page = http.get(`${BASE}/api/products?pageSize=20`).json();
  return { productIds: page.items.map((p) => p.id) };
}

export default function (data) {
  const dice = Math.random();
  if (dice < 0.80) {
    check(http.get(`${BASE}/api/products?pageSize=20`),
      { 'browse 200': (r) => r.status === 200 });
    return;
  }

  const customerId = uuidv4();
  const productId = data.productIds[Math.floor(Math.random() * data.productIds.length)];
  const add = http.post(`${BASE}/api/basket/${customerId}/items`,
    JSON.stringify({ productId, quantity: 1 }),
    { headers: { 'Content-Type': 'application/json' } });
  check(add, { 'basket 204': (r) => r.status === 204 });

  if (dice < 0.85) {
    return;   // basket-only visitor
  }

  const checkout = http.post(`${BASE}/api/checkout`,
    JSON.stringify({ customerId }),
    { headers: { 'Content-Type': 'application/json' } });
  check(checkout, { 'checkout 201': (r) => r.status === 201 });
}