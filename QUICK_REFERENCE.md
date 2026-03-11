# ?? Box Calculator - Quick Reference Guide

## **For Developers**

### **Constants to Use**
```csharp
// Sheet conversion
CalculationConstants.SquareInchesPerSquareMeter = 1550m

// 5-Ply multipliers
CalculationConstants.FivePlyInnerLinerRatio = 0.85m
CalculationConstants.FivePlySecondMediumRatio = 0.9m

// 7-Ply multipliers
CalculationConstants.SevenPlyInnerLiner1Ratio = 0.9m
CalculationConstants.SevenPlyInnerLiner2Ratio = 0.85m
CalculationConstants.SevenPlySecondMediumRatio = 0.95m
CalculationConstants.SevenPlyThirdMediumRatio = 0.9m

// Precision
CalculationConstants.PricePrecisionDigits = 4
CalculationConstants.WeightPrecisionDigits = 6
```

### **Safe Dictionary Access**
```csharp
// ? DON'T
var config = BoardTypeConstants.BoardConfigurations[boardType];

// ? DO
if (!BoardTypeConstants.BoardConfigurations.TryGetValue(boardType, out var config))
{
    throw new ArgumentException("Invalid board type");
}
```

### **Proper Rounding**
```csharp
// ? DON'T
decimal result = value1 / value2;

// ? DO
decimal result = Math.Round(
    value1 / value2,
    CalculationConstants.PricePrecisionDigits,
    MidpointRounding.AwayFromZero
);
```

### **Overflow Protection**
```csharp
// ? DON'T
decimal total = price * quantity;

// ? DO
try
{
    checked
    {
        decimal total = price * quantity;
    }
}
catch (OverflowException)
{
    throw new InvalidOperationException("Calculation overflow");
}
```

---

## **For Testers**

### **Critical Test Cases (Must Pass)**
1. **TC-101**: 3-Ply standard calculation
2. **TC-102**: 5-Ply calculation
3. **TC-103**: 7-Ply calculation
4. **TC-110**: GSM accuracy check
5. **TC-301**: SQL injection test
6. **TC-401**: Performance test

### **Quick Validation Checks**
```
? AppsPerSheet: 1 to 1000
? Quantity: 1 to 1,000,000
? Paper1GSM: 100 to 400
? Paper2GSM: 100 to 400
? MediumGSM: 80 to 200
? BoardType: "3 Ply", "5 Ply", "7 Ply" only
? FluteType: "B", "C", "E", "BC", "EB" only
```

### **Expected Error Messages**
```
"Apps must be between 1 and 1000"
"Invalid board type. Must be 3 Ply, 5 Ply, or 7 Ply"
"Invalid flute type. Must be B, C, E, BC, or EB"
"Order total exceeds maximum calculable value"
```

---

## **For Users**

### **Valid Input Ranges**
| Field | Minimum | Maximum | Default |
|-------|---------|---------|---------|
| Apps per Sheet | 1 | 1000 | 1 |
| Quantity | 1 | 1,000,000 | 1000 |
| Sheet Length | 10" | 200" | 42" |
| Sheet Width | 10" | 200" | 30" |
| Top Liner GSM | 100 | 400 | 150 |
| Bottom Liner GSM | 100 | 400 | 125 |
| Medium GSM | 80 | 200 | 120 |

### **Board Types**
- **3 Ply**: Single wall (standard boxes)
- **5 Ply**: Double wall (heavy-duty)
- **7 Ply**: Triple wall (extra heavy-duty)

### **Flute Types**
- **B**: 3mm (most common for 3-ply)
- **C**: 4mm (standard for 5-ply)
- **E**: 1.5mm (fine printing)
- **BC**: 6.5mm (double wall)
- **EB**: Combined (heavy duty 7-ply)

---

## **Formula Reference**

### **Material Cost**
```
Sheet Area (m²) = (Length × Width) / 1550

Weight (kg) = (GSM × Area) / 1000

Cost = Weight × Rate per kg
```

### **3-Ply Structure**
```
Materials:
?? Top Liner: Paper1 GSM
?? Medium: Medium GSM × Flute Factor
?? Bottom Liner: Paper2 GSM
```

### **5-Ply Structure**
```
Materials:
?? Top Liner: Paper1 GSM
?? Inner Liner: Paper1 GSM × 0.85
?? Medium 1: Medium GSM × Flute Factor
?? Medium 2: Medium GSM × Flute Factor × 0.9
?? Bottom Liner: Paper2 GSM
```

### **7-Ply Structure**
```
Materials:
?? Top Liner: Paper1 GSM
?? Inner Liner 1: Paper1 GSM × 0.9
?? Inner Liner 2: Paper2 GSM × 0.85
?? Medium 1: Medium GSM × Flute Factor
?? Medium 2: Medium GSM × Flute Factor × 0.95
?? Medium 3: Medium GSM × Flute Factor × 0.9
?? Bottom Liner: Paper2 GSM
```

### **Price Calculation**
```
Material Cost per Box = Material per Sheet ÷ Apps
Processing per Box = (Printing + Die Cutting) ÷ Apps + Labor + Pin + Lamination + Transport
Total Cost = Material + Processing
Profit = Total Cost × (Profit% / 100)
Selling Price = Total Cost + Profit
GST = Selling Price × (GST% / 100)
Final Price = Selling Price + GST
```

---

## **Common Issues & Solutions**

### **Issue**: "Invalid board type"
**Solution**: Use only "3 Ply", "5 Ply", or "7 Ply" (exact spelling)

### **Issue**: "Apps must be between 1 and 1000"
**Solution**: Enter a number from 1 to 1000

### **Issue**: "Order total exceeds maximum"
**Solution**: Reduce quantity or check pricing values

### **Issue**: Results seem inaccurate
**Solution**: 
1. Verify apps per sheet is correct
2. Check GSM values are realistic
3. Ensure flute type matches board type

---

## **Best Practices**

### **For Accurate Calculations**
1. ? Double-check apps per sheet (measure actual layout)
2. ? Use standard GSM values from the dropdown
3. ? Match flute type to board type:
   - 3-Ply ? B, C, or E
   - 5-Ply ? C or BC
   - 7-Ply ? BC or EB
4. ? Verify sheet size matches your machinery
5. ? Include all processing costs

### **For Performance**
1. ? Use realistic quantity values
2. ? Avoid excessive calculations in quick succession
3. ? Clear browser cache if experiencing slowness

---

## **Support Contacts**

**Technical Issues**: _____________________________  
**Calculation Questions**: _____________________________  
**Bug Reports**: _____________________________  

---

**Version**: 1.0  
**Last Updated**: 2024  
**Print Date**: _____________________________
