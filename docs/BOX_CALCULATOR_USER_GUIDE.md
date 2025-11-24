# Box Calculator - User Guide

## Quick Reference: Understanding Your Costs

---

## Cost Input Guide

### Material Costs (Always Required)

| Field | Unit | When to Use 0 | Typical Range |
|-------|------|---------------|---------------|
| **Top Liner Rate** | Rs./kg | ? Never | 40-60 |
| **Bottom Liner Rate** | Rs./kg | ? Never | 35-50 |
| **Medium Rate** | Rs./kg | ? Never | 35-45 |

*Note: Setting material rates to 0 will result in unrealistic pricing*

---

### Processing Costs (Optional)

| Field | Unit | When to Use 0 | Typical Range |
|-------|------|---------------|---------------|
| **Printing Cost** | Rs./sheet | ? No printing needed | 0-10 |
| **Die Cutting Cost** | Rs./sheet | ? No die cutting | 0-5 |
| **Labor Cost** | Rs./box | ? Automated process | 0-2 |
| **Pin Cost** | Rs./box | ? Glued boxes | 0-0.50 |
| **Transport Cost** | Rs./box | ? Pickup only | 0-5 |

*? These costs can safely be set to 0 if not applicable*

---

### Business Parameters (Optional)

| Field | Unit | When to Use 0 | Typical Range |
|-------|------|---------------|---------------|
| **Overhead** | % | ? No overhead | 0-20% |
| **Profit Margin** | % | ?? Not recommended | 5-30% |
| **Wastage Factor** | % | ? Minimum 5% | 5-15% |

*?? Setting profit to 0 means you're selling at cost*

---

## How Costs Are Calculated

### Material Costs (Per Kg Basis)
```
Sheet Area (sq m) = (Length × Width) / 1550
Weight (kg) = (GSM × Sheet Area) / 1000
Cost = Weight × Rate per Kg

Example:
- Sheet: 42" × 30" = 1260 sq in = 0.813 sq m
- Top Liner: 150 GSM
- Weight = (150 × 0.813) / 1000 = 0.122 kg
- Rate = Rs. 50/kg
- Cost = 0.122 × 50 = Rs. 6.10 per sheet
```

### Processing Costs (Per Sheet Basis)
```
Cost per Box = Cost per Sheet / Apps

Example:
- Printing = Rs. 2.50 per sheet
- Apps = 12 boxes per sheet
- Cost per Box = 2.50 / 12 = Rs. 0.21 per box
```

### Labor Costs (Per Box Basis)
```
Cost per Box = Direct input value

Example:
- Labor = Rs. 0.50 per box
- Total for 1000 boxes = Rs. 500
```

---

## Understanding the Results

### Live Summary (Right Sidebar)
- **Updates in real-time** as you type
- **Always visible** (fixed position)
- **Quick preview** of final price

### Detailed Results (After Calculate)
- **Complete breakdown** of all costs
- **Shows units** (Rs./kg, Rs./sheet, Rs./box)
- **Per sheet AND per box** costs
- **Suitable for quotes** and invoices

---

## Cost Breakdown Structure

### Section 1: MATERIAL COSTS
```
Top Liner    ? Per Sheet ? Per Box ? Total
Bottom Liner ? Per Sheet ? Per Box ? Total
Medium       ? Per Sheet ? Per Box ? Total
Wastage      ? -         ? Per Box ? Total
```

### Section 2: PROCESSING COSTS
```
Printing     ? Per Sheet ? Per Box ? Total
Die Cutting  ? Per Sheet ? Per Box ? Total
Labor        ? -         ? Per Box ? Total
Pin          ? -         ? Per Box ? Total
Transport    ? -         ? Per Box ? Total
```

### Section 3: BUSINESS COSTS
```
Overhead     ? -         ? Per Box ? Total
Profit       ? -         ? Per Box ? Total
GST          ? -         ? Per Box ? Total
```

---

## Common Scenarios

### Scenario 1: Basic Box (No Printing/Cutting)
```
Set to 0:
- Printing Cost
- Die Cutting Cost
- Labor Cost (if automated)
- Pin Cost (if glued)
- Transport Cost (if pickup)

Result: Only material + wastage + overhead + profit + GST
```

### Scenario 2: Premium Box (All Features)
```
Set values for:
- All material costs (required)
- Printing (if printed)
- Die Cutting (for custom shapes)
- Labor (for assembly)
- Pin Cost (for pinned boxes)
- Transport (for delivery)

Result: Full cost including all features
```

### Scenario 3: Quote Without Profit
```
Set to 0:
- Overhead Percentage
- Profit Margin Percentage

Use case: Testing cost price only
Note: Not recommended for actual quotes
```

---

## Board Type Guide

### 3 Ply (Single Wall)
- **Structure**: 2 Liners + 1 Flute
- **GSM Range**: 280-450
- **Best for**: Light to medium weight products
- **Apps**: Higher (more boxes per sheet)

### 5 Ply (Double Wall)
- **Structure**: 3 Liners + 1 Heavy Flute
- **GSM Range**: 450-650
- **Best for**: Medium to heavy weight products
- **Apps**: Medium (fewer boxes per sheet)

### 7 Ply (Triple Wall)
- **Structure**: 4 Liners + 2 Flutes
- **GSM Range**: 600-900
- **Best for**: Heavy weight, industrial products
- **Apps**: Lower (fewer boxes per sheet)

---

## Tips for Accurate Pricing

### 1. Material Costs
- ? Use current market rates
- ? Account for bulk discounts
- ? Update rates regularly

### 2. Processing Costs
- ? Get actual quotes from printers
- ? Consider setup costs for small quantities
- ? Factor in color vs black & white printing

### 3. Business Costs
- ? Include actual overhead (rent, utilities, staff)
- ? Set realistic profit margins for your market
- ? Consider competitive pricing

### 4. Wastage Factor
- ? 8-10% is industry standard
- ? Higher for complex cutting patterns
- ? Lower for simple rectangular boxes

---

## Verification Checklist

Before finalizing a quote:

- [ ] All required material rates entered
- [ ] Board type matches customer requirements
- [ ] Quantity is correct
- [ ] Sheet size is appropriate for box dimensions
- [ ] Processing costs match actual services needed
- [ ] Overhead and profit percentages are reasonable
- [ ] GST is included/excluded as required
- [ ] Final price per box makes sense
- [ ] Total order value is verified

---

## Support

### If price seems wrong:
1. Check if any cost is set to 0 by mistake
2. Verify material rates are current
3. Ensure wastage percentage is at least 5%
4. Confirm board type matches requirements

### If apps seem low:
1. Try different sheet sizes
2. Consider rotating box orientation
3. Check if box dimensions include flaps correctly
4. Verify Height value is correct

### If total GSM is outside range:
1. Adjust liner GSM values
2. Change flute type (B, C, E)
3. Select different board type
4. Verify all three components (top, bottom, medium)

---

## Keyboard Shortcuts (Future Enhancement)

- **Tab**: Move to next field
- **Enter**: Calculate (when on Calculate button)
- **Ctrl+R**: Reset to defaults
- **Ctrl+P**: Print results

---

## Additional Resources

- Industry GSM Standards Guide
- Sheet Size Recommendations
- Flute Type Selection Guide
- Competitive Pricing Analysis
- Cost Estimation Worksheets

---

**Last Updated**: 2024  
**Version**: 2.0 (Fixed & Enhanced)  
**Support**: Contact your administrator for assistance
