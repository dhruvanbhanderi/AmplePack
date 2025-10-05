# AmplePack Comprehensive Testing Guide

This document provides a complete guide to the automated testing setup for the AmplePack application.

## ?? Overview

The AmplePack testing framework includes:
- **Unit Testing** (xUnit + Moq + FluentAssertions)
- **Integration Testing** (TestServer + InMemory Database)
- **UI/E2E Testing** (Playwright)
- **Performance Testing** (k6 + NBomber)
- **Security Testing** (OWASP ZAP)
- **Continuous Integration** (GitHub Actions)

## ?? Project Structure

```
AmplePack/
??? AmplePack/                          # Main application
??? AmplePack.Tests/                    # Unit & Integration tests
?   ??? Unit/
?   ?   ??? Controllers/
?   ?   ??? Services/
?   ?   ??? Models/
?   ??? Integration/
?   ??? Performance/
?   ??? Infrastructure/
??? AmplePack.UITests/                  # Playwright UI tests
?   ??? Tests/
?   ??? Infrastructure/
?   ??? playwright.config.json
??? PerformanceTests/                   # k6 performance tests
?   ??? load-test.js
?   ??? spike-test.js
?   ??? run-performance-tests.sh
??? SecurityTests/                      # Security testing scripts
?   ??? run-security-tests.sh
??? .github/workflows/                  # CI/CD pipelines
?   ??? comprehensive-testing.yml
??? .zap/                              # OWASP ZAP configuration
    ??? rules.tsv
```

## ?? Quick Start

### Prerequisites
- .NET 9 SDK
- Node.js 18+ (for Playwright)
- k6 (for performance testing)
- Docker (for security testing)

### Setup
```bash
# Make setup script executable and run it
chmod +x setup-testing-environment.sh
./setup-testing-environment.sh
```

### Run All Tests
```bash
# Quick test run (unit + integration only)
./run-all-tests.sh --quick

# Full test suite
./run-all-tests.sh

# Specify custom URL
./run-all-tests.sh --url https://localhost:7001
```

## ?? Testing Layers

### 1. Unit Tests

**Location**: `AmplePack.Tests/Unit/`

**Technologies**: xUnit, Moq, FluentAssertions, AutoFixture

**Command**:
```bash
dotnet test AmplePack.Tests/AmplePack.Tests.csproj --filter Category!=Integration
```

**Features**:
- Controller testing with mocked dependencies
- Service layer testing with isolated logic
- Model validation testing
- Comprehensive test data generation using Bogus

**Example**:
```csharp
[Fact]
public void CalculateBoxPrice_With_Valid_Input_Should_Return_Valid_Result()
{
    // Arrange
    var input = TestDataFactory.CreateValidBoxCalculatorInput();
    
    // Act
    var result = _service.CalculateBoxPrice(input);
    
    // Assert
    result.Should().NotBeNull();
    result.FinalPricePerBoxIncGST.Should().BeGreaterThan(0);
}
```

### 2. Integration Tests

**Location**: `AmplePack.Tests/Integration/`

**Technologies**: ASP.NET Core TestServer, InMemory Database

**Command**:
```bash
dotnet test AmplePack.Tests/AmplePack.Tests.csproj --filter Category=Integration
```

**Features**:
- Full HTTP request/response testing
- Database integration with seeded test data
- API endpoint validation
- Real service interaction testing

**Example**:
```csharp
[Fact]
public async Task Post_BoxCalculator_Calculate_With_Valid_Data_Should_Return_Json()
{
    // Arrange
    var input = TestDataFactory.CreateValidBoxCalculatorInput();
    var json = JsonSerializer.Serialize(input);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/BoxCalculator/Calculate", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### 3. UI/E2E Tests

**Location**: `AmplePack.UITests/`

**Technologies**: Playwright for .NET

**Command**:
```bash
cd AmplePack.UITests
dotnet test --configuration Release
```

**Features**:
- Cross-browser testing (Chromium, Firefox, Safari)
- Real user interaction simulation
- Visual regression testing
- Mobile responsiveness testing
- Performance timing validation

**Example**:
```csharp
[Fact]
public async Task BoxCalculator_Should_Calculate_Price_With_Valid_Input()
{
    await NavigateToAsync("/BoxCalculator");
    
    await FillAndWaitAsync("#Length", "10");
    await FillAndWaitAsync("#Width", "8");
    await Page.ClickAsync("button[type='submit']");
    
    await Expect(Page.Locator("#calculation-results")).ToBeVisibleAsync();
}
```

### 4. Performance Tests

**Location**: `PerformanceTests/`

**Technologies**: k6, NBomber

**Command**:
```bash
cd PerformanceTests
./run-performance-tests.sh --url https://localhost:5001
```

**Features**:
- Load testing with ramping user scenarios
- Spike testing for traffic bursts
- API endpoint performance validation
- Response time and throughput metrics
- Performance regression detection

**k6 Test Example**:
```javascript
export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 5 },
    { duration: '30s', target: 20 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
  },
};
```

### 5. Security Tests

**Location**: `SecurityTests/`

**Technologies**: OWASP ZAP, Custom security checks

**Command**:
```bash
cd SecurityTests
./run-security-tests.sh --url https://localhost:5001
```

**Features**:
- OWASP Top 10 vulnerability scanning
- SQL injection testing
- XSS vulnerability detection
- Security header validation
- Information disclosure checks

## ?? Continuous Integration

### GitHub Actions Workflow

**File**: `.github/workflows/comprehensive-testing.yml`

**Triggers**:
- Push to main, develop, mvc-stage-2 branches
- Pull requests to main, develop
- Manual workflow dispatch

**Jobs**:
1. **Build & Unit Tests** - Builds solution and runs unit tests
2. **Integration Tests** - Runs integration tests with test database
3. **UI Tests** - Runs Playwright tests with application server
4. **Performance Tests** - Runs k6 load tests (main branch only)
5. **Security Tests** - Runs OWASP ZAP scans (main branch only)
6. **Code Quality** - SonarCloud analysis
7. **Build Artifacts** - Creates deployment packages
8. **Test Summary** - Generates comprehensive test report

### Workflow Features:
- Parallel test execution for faster feedback
- Conditional job execution (performance/security on main branch)
- Artifact collection for all test results
- Code coverage reporting
- Test result visualization

## ?? Test Reporting

### Coverage Reports
- **Unit Tests**: XPlat Code Coverage with Cobertura format
- **Integration Tests**: Combined coverage reporting
- **Codecov Integration**: Automatic coverage reporting to Codecov

### Performance Reports
- **k6 Results**: JSON and CSV formats for analysis
- **NBomber Reports**: HTML reports with detailed metrics
- **Historical Trending**: Performance regression detection

### Security Reports
- **ZAP Reports**: HTML and JSON formats
- **Vulnerability Summary**: Categorized by severity
- **Compliance Reporting**: OWASP Top 10 coverage

## ??? Development Workflow

### Local Development
1. **Write Code** with corresponding tests
2. **Run Quick Tests** during development:
   ```bash
   ./run-all-tests.sh --quick
   ```
3. **Run Full Suite** before committing:
   ```bash
   ./run-all-tests.sh
   ```

### Pre-Commit Checklist
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Code coverage above 80%
- [ ] No high-severity security issues
- [ ] Performance benchmarks maintained

### CI/CD Pipeline
1. **Code Push** triggers automated testing
2. **Parallel Execution** of test suites
3. **Quality Gates** prevent deployment of failing builds
4. **Automatic Deployment** on successful main branch builds

## ?? Test Strategy

### Test Pyramid Distribution
- **70% Unit Tests** - Fast, isolated, comprehensive coverage
- **20% Integration Tests** - Service integration validation
- **10% UI Tests** - Critical user journey validation

### Coverage Goals
- **Unit Test Coverage**: 90%+
- **Integration Coverage**: 80%+
- **Critical Path Coverage**: 100%

### Performance Benchmarks
- **API Response Time**: 95th percentile < 2 seconds
- **Page Load Time**: < 3 seconds
- **Concurrent Users**: Support 50+ simultaneous users
- **Error Rate**: < 1% under normal load

### Security Standards
- **OWASP Top 10**: No high-severity vulnerabilities
- **Security Headers**: All recommended headers present
- **Input Validation**: 100% of inputs validated
- **Authentication**: Comprehensive auth/authz testing

## ?? Troubleshooting

### Common Issues

**Application doesn't start for testing**:
```bash
# Check if port is already in use
netstat -tlnp | grep 5001
# Kill process using the port
sudo kill -9 <PID>
```

**Playwright tests fail**:
```bash
# Reinstall browsers
cd AmplePack.UITests
pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps
```

**k6 performance tests fail**:
```bash
# Verify k6 installation
k6 version
# Check application accessibility
curl -I https://localhost:5001
```

**Security tests timeout**:
```bash
# Increase timeout in ZAP configuration
# Or run baseline scan only for faster results
```

### Debug Commands

**Verbose test output**:
```bash
dotnet test --verbosity detailed
```

**Coverage report generation**:
```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

**Performance test analysis**:
```bash
# Analyze k6 results
jq '.metrics.http_req_duration.values' test-results/load_test_*.json
```

## ?? Best Practices

### Test Organization
- **Arrange-Act-Assert** pattern for all tests
- **Descriptive test names** that explain the scenario
- **Test categories** for filtering and organization
- **Shared test infrastructure** for consistency

### Test Data Management
- **Factory pattern** for test data creation
- **Realistic test data** using Bogus faker
- **Isolated test data** to prevent test interference
- **Cleanup strategies** for integration tests

### Performance Testing
- **Realistic load patterns** based on production metrics
- **Gradual load ramping** to identify breaking points
- **Multiple test scenarios** (load, stress, spike)
- **Performance budgets** with automated alerts

### Security Testing
- **Regular security scans** in CI/CD pipeline
- **False positive management** with ZAP rules
- **Comprehensive input testing** for all endpoints
- **Security regression prevention** with baseline comparisons

## ?? Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [k6 Documentation](https://k6.io/docs/)
- [OWASP ZAP](https://www.zaproxy.org/docs/)
- [GitHub Actions](https://docs.github.com/en/actions)

---

**Note**: This testing framework is designed to grow with your application. Regular review and updates of test scenarios ensure continued effectiveness and coverage.