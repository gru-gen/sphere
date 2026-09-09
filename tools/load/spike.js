import http from 'k6/http';
import { check } from 'k6';

// summary: scaled to the day-one machine: sustain a base rate, then a
// 10x spike for one minute. The SHAPE of the result is the finding.
export const options = {
  scenarios: {
    spike: {
      executor: 'ramping-arrival-rate',
      startRate: 200, timeUnit: '1s',
      preAllocatedVUs: 300, maxVUs: 4000,
      stages: [
        { target: 200, duration: '1m' },    // steady base
        { target: 2000, duration: '10s' },  // why: the sale starts NOW
        { target: 2000, duration: '1m' },   // hold the 10x
        { target: 200, duration: '30s' },   // recovery — watch how long it takes
      ],
    },
  },
};

export default function () {
  const res = http.get('http://localhost:5100/api/products?pageSize=20');
  check(res, { 'status 200': (r) => r.status === 200 });
}