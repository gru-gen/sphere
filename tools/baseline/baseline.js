import http from 'k6/http';

// The day-one baseline: one minimal endpoint, 50 virtual users, 30 s.
// Record p95 and requests/s into docs/baseline.md.
export const options = {
  vus: 50,
  duration: '30s',
};

export default function () {
  http.get('http://127.0.0.1:5000/ping');
}
