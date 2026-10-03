import http from 'k6/http';
import { check } from 'k6';

// summary: the READ path against NFR-1. Open model: arrivals do not wait for
// responses — like real customers, who do not care that the shop is busy.
export const options = {
  scenarios: {
    browse: {
      executor: 'constant-arrival-rate',
      rate: 2000,             // why: NFR-1 is an ARRIVAL rate, not a user count
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 200,
      maxVUs: 2000,
    },
  },
  thresholds: {
    // why: the NFRs as executable code — the run itself passes or fails
    http_req_duration: ['p(95)<50'],
    http_req_failed: ['rate<0.001'],
  },
};

export default function () {
  const res = http.get('http://localhost:5100/api/products?pageSize=20');
  check(res, { 'status 200': (r) => r.status === 200 });
}
