import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    spike_test: {
      executor: 'ramping-arrival-rate',
      startRate: 1,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 100,
      stages: [
        { duration: '30s', target: 10 },  // Normal load
        { duration: '10s', target: 50 },  // Spike up
        { duration: '30s', target: 50 },  // Stay at spike
        { duration: '10s', target: 10 },  // Spike down
        { duration: '30s', target: 10 },  // Normal load
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'], // 95% under 5s during spike
    http_req_failed: ['rate<0.1'],     // Error rate under 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:5001';

export default function () {
  // Simulate user behavior during spike
  const pages = [
    '/',
    '/BoxCalculator',
    '/Orders',
    '/Customers',
  ];
  
  const randomPage = pages[Math.floor(Math.random() * pages.length)];
  const response = http.get(`${BASE_URL}${randomPage}`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 5s': (r) => r.timings.duration < 5000,
  });
  
  // Simulate some think time
  sleep(Math.random() * 2 + 1); // 1-3 seconds
}