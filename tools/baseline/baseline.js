import http from 'k6/http';

export const options = {
  vus: 50,
  duration: '30s',
};

export default function () {
  http.get('http://127.0.0.1:5000/ping');
}
