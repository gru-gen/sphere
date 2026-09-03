import http from 'k6/http';

// The baseline: one minimal endpoint, 50 virtual users, 30 s.
export const options = {
  vus: 50,
  duration: '30s',
};

export default function () {
  http.get('http://127.0.0.1:5000/ping');
}
