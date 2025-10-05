import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 5 },   // Ramp up to 5 users
    { duration: '1m', target: 5 },    // Stay at 5 users
    { duration: '30s', target: 10 },  // Ramp up to 10 users
    { duration: '2m', target: 10 },   // Stay at 10 users
    { duration: '30s', target: 20 },  // Ramp up to 20 users
    { duration: '2m', target: 20 },   // Stay at 20 users
    { duration: '30s', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests should be below 2s
    http_req_failed: ['rate<0.05'],    // Error rate should be below 5%
    errors: ['rate<0.1'],              // Custom error rate should be below 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:5001';

export default function () {
  // Test 1: Home page load
  testHomePage();
  
  // Test 2: Box Calculator
  testBoxCalculator();
  
  // Test 3: Orders page
  testOrdersPage();
  
  // Test 4: API endpoints
  testApiEndpoints();
  
  sleep(1);
}

function testHomePage() {
  const response = http.get(`${BASE_URL}/`);
  
  const success = check(response, {
    'Home page status is 200': (r) => r.status === 200,
    'Home page loads in <2s': (r) => r.timings.duration < 2000,
    'Home page contains title': (r) => r.body.includes('AmplePack'),
  });
  
  errorRate.add(!success);
}

function testBoxCalculator() {
  // Load the box calculator page
  let response = http.get(`${BASE_URL}/BoxCalculator`);
  
  let success = check(response, {
    'Box Calculator page status is 200': (r) => r.status === 200,
    'Box Calculator page loads in <2s': (r) => r.timings.duration < 2000,
    'Box Calculator page contains form': (r) => r.body.includes('form'),
  });
  
  errorRate.add(!success);
  
  // Test calculation API
  const calculationData = {
    Length: 10,
    Width: 8,
    Height: 6,
    BoardGSM: 150,
    BoardType: 'Single Wall',
    CompressionRatio: 1.0,
    Quantity: 1000,
    SheetLength: 40,
    SheetWidth: 30,
    OverheadPercentage: 15,
    ProfitMarginPercentage: 20,
    DiscountPercentage: 0,
    IncludeGST: true
  };
  
  response = http.post(`${BASE_URL}/BoxCalculator/Calculate`, JSON.stringify(calculationData), {
    headers: {
      'Content-Type': 'application/json',
    },
  });
  
  success = check(response, {
    'Box calculation status is 200': (r) => r.status === 200,
    'Box calculation completes in <3s': (r) => r.timings.duration < 3000,
    'Box calculation returns JSON': (r) => r.headers['Content-Type'] && r.headers['Content-Type'].includes('application/json'),
    'Box calculation returns price': (r) => {
      try {
        const result = JSON.parse(r.body);
        return result.finalPricePerBoxIncGST && result.finalPricePerBoxIncGST > 0;
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(!success);
}

function testOrdersPage() {
  const response = http.get(`${BASE_URL}/Orders`);
  
  const success = check(response, {
    'Orders page status is 200': (r) => r.status === 200,
    'Orders page loads in <2s': (r) => r.timings.duration < 2000,
    'Orders page contains orders content': (r) => r.body.includes('Orders') || r.body.includes('order'),
  });
  
  errorRate.add(!success);
}

function testApiEndpoints() {
  // Test layout visualization API
  const layoutData = {
    Length: 10,
    Width: 8,
    Height: 6,
    SheetLength: 40,
    SheetWidth: 30,
    BoardGSM: 150,
    CompressionRatio: 1.0,
    Quantity: 1000
  };
  
  const response = http.post(`${BASE_URL}/BoxCalculator/GetLayoutVisualization`, JSON.stringify(layoutData), {
    headers: {
      'Content-Type': 'application/json',
    },
  });
  
  const success = check(response, {
    'Layout visualization status is 200': (r) => r.status === 200,
    'Layout visualization completes in <2s': (r) => r.timings.duration < 2000,
    'Layout visualization returns JSON': (r) => r.headers['Content-Type'] && r.headers['Content-Type'].includes('application/json'),
    'Layout visualization returns layout data': (r) => {
      try {
        const result = JSON.parse(r.body);
        return result.totalBlanksPerSheet !== undefined && result.efficiencyPercentage !== undefined;
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(!success);
}

// Setup function (runs once at the start)
export function setup() {
  console.log(`Starting performance test against ${BASE_URL}`);
  
  // Verify the application is accessible
  const response = http.get(BASE_URL);
  if (response.status !== 200) {
    throw new Error(`Application not accessible at ${BASE_URL}. Status: ${response.status}`);
  }
  
  return { baseUrl: BASE_URL };
}

// Teardown function (runs once at the end)
export function teardown(data) {
  console.log(`Performance test completed for ${data.baseUrl}`);
}