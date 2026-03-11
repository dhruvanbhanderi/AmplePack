# ?? Box Calculator - Comprehensive Test Plan

## **Test Environment**
- **Framework**: .NET 9
- **Project**: AmplePack Box Calculator
- **Test Date**: 2024
- **Tester**: QA Team

---

## **Test Categories**

1. ? Input Validation Tests (10 tests)
2. ? Calculation Accuracy Tests (15 tests)
3. ? Integration Tests (8 tests)
4. ? Security Tests (7 tests)
5. ? Performance Tests (5 tests)
6. ? Edge Case Tests (10 tests)

**Total**: 55 Test Cases

---

## **1. INPUT VALIDATION TESTS** (Priority: Critical)

### TC-001: Apps Per Sheet - Zero Value
**Input**: AppsPerSheet = 0
**Expected**: Error message "Apps must be between 1 and 1000"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-002: Apps Per Sheet - Negative Value
**Input**: AppsPerSheet = -5
**Expected**: Validation error
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-003: Apps Per Sheet - Above Maximum
**Input**: AppsPerSheet = 1001
**Expected**: Error message "Apps must be between 1 and 1000"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-004: Quantity - Zero Value
**Input**: Quantity = 0
**Expected**: Error message "Quantity must be between 1 and 1,000,000"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-005: Quantity - Above Maximum
**Input**: Quantity = 1,000,001
**Expected**: Error message "Quantity must be between 1 and 1,000,000"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-006: Paper1 GSM - Below Minimum
**Input**: Paper1GSM = 99
**Expected**: Error message "GSM must be between 100 and 400"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-007: Paper1 GSM - Above Maximum
**Input**: Paper1GSM = 401
**Expected**: Error message "GSM must be between 100 and 400"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-008: Medium GSM - Below Minimum
**Input**: MediumGSM = 79
**Expected**: Error message "Medium GSM must be between 80 and 200"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-009: Board Type - Invalid Value
**Input**: BoardType = "Invalid Type"
**Expected**: Validation error "Invalid board type. Must be 3 Ply, 5 Ply, or 7 Ply"
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-010: Flute Type - Invalid Value
**Input**: FluteType = "X"
**Expected**: Validation error "Invalid flute type. Must be B, C, E, BC, or EB"
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **2. CALCULATION ACCURACY TESTS** (Priority: Critical)

### TC-101: 3-Ply Standard Calculation
**Input**:
- BoardType: "3 Ply"
- AppsPerSheet: 12
- Quantity: 1000
- SheetLength: 42"
- SheetWidth: 30"
- Paper1GSM: 150
- Paper2GSM: 125
- MediumGSM: 120
- FluteType: "B" (factor 1.4)
- All rates: default values
- All optional costs: 0
- Profit: 0%
- GST: No

**Expected Calculation**:
```
Sheet Area = 42 × 30 = 1260 sq inches
Sheet Area (m²) = 1260 / 1550 = 0.8129 m²

Materials:
- Paper1 weight = (150 × 0.8129) / 1000 = 0.12194 kg
- Paper2 weight = (125 × 0.8129) / 1000 = 0.10161 kg
- Medium weight = (120 × 1.4 × 0.8129) / 1000 = 0.13657 kg

Costs per sheet:
- Paper1 = 0.12194 × 50 = Rs. 6.097
- Paper2 = 0.10161 × 45 = Rs. 4.572
- Medium = 0.13657 × 42 = Rs. 5.736
- Total per sheet = Rs. 16.405

Per box (÷12):
- Material = Rs. 1.3671
- Total = Rs. 1.3671

For 1000 boxes:
- Total order value = Rs. 1,367.10
```

**Status**: ? Not Tested | ? Pass | ? Fail
**Notes**: _____________________________

### TC-102: 5-Ply Standard Calculation
**Input**:
- BoardType: "5 Ply"
- AppsPerSheet: 8
- Quantity: 500
- Paper1GSM: 150
- Paper2GSM: 125
- MediumGSM: 120
- FluteType: "BC" (factor 1.8)

**Expected Calculation**:
```
Materials (5-ply):
- Top liner: 150 GSM × 0.8129 m² = 0.12194 kg
- Inner liner: 150 × 0.85 × 0.8129 m² = 0.10365 kg
- Bottom liner: 125 × 0.8129 m² = 0.10161 kg
- Medium 1: 120 × 1.8 × 0.8129 m² = 0.17559 kg
- Medium 2: 120 × 1.8 × 0.9 × 0.8129 m² = 0.15803 kg

Total weight per sheet = 0.66082 kg

Costs per sheet:
- Paper1 (top + inner) = (0.12194 + 0.10365) × 50 = Rs. 11.280
- Paper2 (bottom) = 0.10161 × 45 = Rs. 4.572
- Medium (both layers) = (0.17559 + 0.15803) × 42 = Rs. 14.012
- Total per sheet = Rs. 29.864

Per box (÷8) = Rs. 3.733
```

**Status**: ? Not Tested | ? Pass | ? Fail
**Notes**: _____________________________

### TC-103: 7-Ply Heavy Duty Calculation
**Input**:
- BoardType: "7 Ply"
- AppsPerSheet: 4
- Quantity: 200
- All maximum GSM values
- FluteType: "EB"

**Expected**: Calculate successfully with accurate multi-layer costs
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-104: Single Box Calculation (Edge Case)
**Input**: AppsPerSheet = 1, Quantity = 1
**Expected**: Per box price equals per sheet price
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-105: Verify Sheets Needed Calculation
**Input**: AppsPerSheet = 12, Quantity = 1000
**Expected**: Sheets needed = Math.Ceiling(1000/12) = 84 sheets
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-106: GST Calculation (18%)
**Input**: 
- Selling price before GST: Rs. 10.00
- GST Rate: 18%
**Expected**: 
- GST Amount: Rs. 1.80
- Final price: Rs. 11.80
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-107: All Processing Costs Combined
**Input**:
- Printing: Rs. 2.50/sheet
- Die Cutting: Rs. 1.50/sheet
- Labor: Rs. 0.50/box
- Pin: Rs. 0.30/box
- Lamination: Rs. 1.00/box
- Transport: Rs. 0.20/box
- Apps: 12

**Expected**:
```
Per box from sheet:
- Printing = 2.50 / 12 = Rs. 0.2083
- Die Cutting = 1.50 / 12 = Rs. 0.1250
Per box direct:
- Labor = Rs. 0.50
- Pin = Rs. 0.30
- Lamination = Rs. 1.00
- Transport = Rs. 0.20

Total processing = Rs. 2.3333
```

**Status**: ? Not Tested | ? Pass | ? Fail

### TC-108: Zero Profit Margin
**Input**: ProfitMarginPercentage = 0
**Expected**: SellingPrice = TotalCost (no profit added)
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-109: 100% Profit Margin
**Input**: 
- TotalCost = Rs. 10.00
- ProfitMarginPercentage = 100
**Expected**: 
- Profit = Rs. 10.00
- Selling Price = Rs. 20.00
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-110: GSM Display Matches Weight Calculation
**For Each Board Type**:
1. Calculate displayed total GSM
2. Calculate actual sheet weight
3. Verify: (Weight / Sheet Area in m²) × 1000 = Total GSM

**Expected**: Match within 0.1 GSM
**Status**: 
- 3-Ply: ? | ? | ?
- 5-Ply: ? | ? | ?
- 7-Ply: ? | ? | ?

### TC-111: Decimal Precision - Fractional Quantities
**Input**:
- Quantity: 777
- Price per box: Rs. 12.3456
**Expected**: Total = Rs. 9,592.5712 (exact to 4 decimals)
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-112: Cost Items Sum Verification
**Input**: Any valid calculation
**Expected**: Sum of all cost items = Final price per box
**Tolerance**: ±0.0001 (4 decimal places)
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-113: Rounding Consistency
**Input**: Multiple calculations with same input
**Expected**: Always produces identical output
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-114: Material Rate Changes
**Input**: Change Paper1RatePerKg from 50 to 55
**Expected**: Only Paper1 costs change, proportionally
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-115: Sheet Size Impact
**Input**: 
- Calculate with 42" × 30"
- Calculate with 40" × 28"
**Expected**: Smaller sheet = higher cost per box (less apps possible)
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **3. INTEGRATION TESTS** (Priority: High)

### TC-201: Calculate ? Display Results
**Steps**:
1. Enter all required fields
2. Click Calculate
3. Verify all result fields populated

**Expected**: Complete breakdown displayed
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-202: Live Calculate Updates
**Steps**:
1. Enter fields
2. Observe real-time preview
3. Change quantity
4. Verify immediate update

**Expected**: Live preview updates without page refresh
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-203: Board Type Change
**Steps**:
1. Select 3-Ply
2. Calculate
3. Change to 5-Ply
4. Calculate again

**Expected**: GSM and costs update correctly
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-204: GST Toggle
**Steps**:
1. Calculate without GST
2. Enable GST
3. Verify price increases by GST amount
4. Disable GST
5. Verify price returns to original

**Expected**: GST toggles correctly
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-205: Invalid Input ? Calculate
**Steps**:
1. Enter invalid BoardType
2. Attempt to calculate
3. Verify error message shown

**Expected**: Clear validation error displayed
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-206: Form Reset
**Steps**:
1. Fill all fields
2. Calculate
3. Reset form
4. Verify defaults restored

**Expected**: All fields return to default values
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-207: Multiple Calculations in Session
**Steps**:
1. Perform 10 different calculations
2. Verify each produces correct results

**Expected**: No session/state contamination
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-208: Concurrent Users
**Steps**:
1. Simulate 10 concurrent users
2. Each performs calculation simultaneously

**Expected**: All calculations complete successfully
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **4. SECURITY TESTS** (Priority: Critical)

### TC-301: SQL Injection - BoardType
**Input**: BoardType = "3 Ply'; DROP TABLE--"
**Expected**: Validation blocks input
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-302: XSS - BoardType
**Input**: BoardType = "<script>alert('xss')</script>"
**Expected**: Validation blocks input
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-303: Null Reference Exploit
**Input**: Send null for all required fields
**Expected**: Graceful error, no crash
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-304: Overflow Attack
**Input**: 
- Quantity = Int32.MaxValue
- Price = Decimal.MaxValue
**Expected**: Overflow detected and reported
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-305: Invalid Dictionary Key
**Input**: BoardType = "NonExistent"
**Expected**: Safe handling with TryGetValue
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-306: Unicode Characters
**Input**: BoardType = "3 Ply\u0000\u0001"
**Expected**: Validation catches invalid characters
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-307: Authorization Check
**Steps**:
1. Access calculator without authentication
**Expected**: Redirect to login
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **5. PERFORMANCE TESTS** (Priority: Medium)

### TC-401: Single Calculation Response Time
**Input**: Standard 3-Ply calculation
**Expected**: < 100ms response time
**Actual**: _____ ms
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-402: 100 Concurrent Users
**Input**: 100 simultaneous calculations
**Expected**: All complete in < 500ms
**Actual**: _____ ms
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-403: Large Quantity (1M boxes)
**Input**: Quantity = 1,000,000
**Expected**: Completes successfully in < 200ms
**Actual**: _____ ms
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-404: Memory Usage
**Steps**: Perform 1000 calculations sequentially
**Expected**: Memory usage remains stable (no leaks)
**Actual**: _____ MB
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-405: CPU Usage
**Steps**: Perform calculations for 5 minutes continuously
**Expected**: CPU usage < 20% average
**Actual**: _____ %
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **6. EDGE CASE TESTS** (Priority: Medium)

### TC-501: Minimum Everything
**Input**: All values at minimum allowed
**Expected**: Calculates without error
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-502: Maximum Everything
**Input**: All values at maximum allowed
**Expected**: Calculates without overflow
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-503: Very Small Sheet (10" × 10")
**Input**: SheetLength = 10, SheetWidth = 10, AppsPerSheet = 1
**Expected**: Calculates correctly
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-504: Very Large Sheet (200" × 200")
**Input**: SheetLength = 200, SheetWidth = 200, AppsPerSheet = 1000
**Expected**: Calculates correctly
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-505: All Zero Costs
**Input**: All processing costs = 0, Profit = 0%
**Expected**: Only material costs calculated
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-506: Maximum Profit (100%)
**Input**: ProfitMarginPercentage = 100
**Expected**: Selling price = 2 × cost
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-507: Maximum GST (50%)
**Input**: GSTRate = 50
**Expected**: GST = 50% of selling price
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-508: Flute Type Variations
**Steps**: Test all flute types (B, C, E, BC, EB)
**Expected**: Each produces different medium weight
**Status**:
- B: ? | ? | ?
- C: ? | ? | ?
- E: ? | ? | ?
- BC: ? | ? | ?
- EB: ? | ? | ?

### TC-509: Boundary Apps Value
**Input**: AppsPerSheet = 1 (minimum)
**Expected**: Per box = per sheet cost
**Status**: ? Not Tested | ? Pass | ? Fail

### TC-510: Boundary Quantity
**Input**: Quantity = 1 (minimum)
**Expected**: Total = 1 × per box price
**Status**: ? Not Tested | ? Pass | ? Fail

---

## **TEST EXECUTION SUMMARY**

### **Statistics**
- Total Test Cases: 55
- Critical Priority: 25
- High Priority: 18
- Medium Priority: 12

### **Execution Metrics**
- Tests Passed: _____ / 55
- Tests Failed: _____ / 55
- Tests Blocked: _____ / 55
- Pass Rate: _____% 

### **Critical Issues Found**
1. ________________________________
2. ________________________________
3. ________________________________

### **Sign-Off**
- **Tester Name**: _____________________________
- **Date**: _____________________________
- **Approval**: ? Approved | ? Conditional | ? Rejected
- **Notes**: ________________________________________________________

---

## **REGRESSION TEST CHECKLIST**

After any code change, run:
- ? TC-101 (3-Ply standard calculation)
- ? TC-102 (5-Ply calculation)
- ? TC-103 (7-Ply calculation)
- ? TC-110 (GSM accuracy)
- ? TC-301 (SQL injection)
- ? TC-401 (Performance)

---

**Document Version**: 1.0
**Last Updated**: 2024
**Next Review Date**: _____________________________
