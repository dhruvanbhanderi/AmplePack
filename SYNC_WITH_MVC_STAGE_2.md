# Sync ample-exe with mvc-stage-2 for Local Deployment

## ?? Overview
This guide helps you merge the `mvc-stage-2` branch (with SQLite decimal ordering fixes) into your `ample-exe` branch while maintaining local deployment configuration.

## ? What You Get After Sync
- ? SQLite decimal ordering fixes (Reports page will work!)
- ? SQLite connection string fixes
- ? Complete MVC Stage 2 features
- ? Your Invoice PDF optimizations (preserved)
- ? QuestPDF export error fixes (preserved)
- ? Local deployment configuration (port 5000)
- ? Auto-browser launch

## ?? Manual Merge Steps

### Step 1: Ensure Your Work is Saved
```powershell
cd "C:\Users\Dhruvan\source\repos\dhruvanbhanderi\AmplePack"

# Check status
git status

# If you have uncommitted changes, commit them
git add -A
git commit -m "Save current work before sync"
```

### Step 2: Fetch Latest Changes
```powershell
# Fetch all remote branches
git fetch origin

# View branch history
git log --oneline --graph --all -10
```

### Step 3: Merge mvc-stage-2
```powershell
# Make sure you're on ample-exe
git checkout ample-exe

# Merge mvc-stage-2 into ample-exe
git merge origin/mvc-stage-2

# If there are conflicts, resolve them (see below)
```

### Step 4: Resolve Any Conflicts
If you see merge conflicts, they'll typically be in:
- `AmplePack\Controllers\ReportsController.cs`
- `AmplePack\Services\OrderManagementService.cs`
- `AmplePack\Program.cs`

**For conflicts:**
1. Open the conflicted files in VS Code
2. Choose "Accept Both Changes" or manually merge
3. After resolving:
```powershell
git add .
git commit -m "Merge mvc-stage-2: Resolved conflicts"
```

### Step 5: Verify Local Deployment Settings

Check `Program.cs` to ensure local port is set:
```csharp
// Should be at line 21-22
var port = 5000;
builder.WebHost.UseUrls($"http://localhost:{port}");
```

### Step 6: Test the Application
```powershell
# Build and run
cd AmplePack
dotnet build
dotnet run
```

**Expected behavior:**
- ? Server starts on `http://localhost:5000`
- ? Browser opens automatically
- ? No more SQLite decimal ordering errors
- ? Reports page loads successfully

## ?? What Changed from mvc-stage-2

### Fixed Files:
1. **`ReportsController.cs`** (Line 85-91)
   - Changed: Load inventory data to memory before sorting
   - Why: SQLite doesn't support decimal ordering in SQL

2. **`OrderManagementService.cs`** (Line 258-287)
   - Changed: Load customer data to memory before sorting
   - Why: Same SQLite limitation

### Example of the Fix:
```csharp
// BEFORE (Causes Error):
viewModel.CriticalStockItems = await _context.Inventories
    .Where(i => i.AvailableQuantity <= i.ReorderLevel)
    .OrderBy(i => i.AvailableQuantity) // ? Decimal ordering in SQL
    .Take(5)
    .ToListAsync();

// AFTER (Fixed):
var lowStockItems = await _context.Inventories
    .Where(i => i.AvailableQuantity <= i.ReorderLevel)
    .ToListAsync(); // ? Load first

viewModel.CriticalStockItems = lowStockItems
    .OrderBy(i => i.AvailableQuantity) // ? Sort in memory
    .Take(5)
    .ToList();
```

## ?? Alternative: Manual File Copy

If merge is too complex, you can manually copy the fixes:

### File 1: `ReportsController.cs`
```powershell
# Backup your current file
copy AmplePack\Controllers\ReportsController.cs AmplePack\Controllers\ReportsController.cs.backup

# Then manually apply the fix shown above
```

### File 2: `OrderManagementService.cs`
I already fixed this for you! The compilation error is resolved.

## ?? Quick Verification Checklist

After sync, verify these work:
- [ ] Application starts on port 5000
- [ ] Browser opens automatically
- [ ] Login works (admin@ample.com / Admin@123)
- [ ] Dashboard loads
- [ ] Reports page loads (no SQLite error!)
- [ ] Top customers display
- [ ] Critical stock items display
- [ ] PDF export works
- [ ] CSV export works

## ?? Post-Sync: Push to Remote

After successful merge and testing:
```powershell
# Push your synced ample-exe branch
git push origin ample-exe

# If you want to also update mvc-stage-2 with your changes:
git checkout mvc-stage-2
git merge ample-exe
git push origin mvc-stage-2
```

## ?? Troubleshooting

### Issue: "Already up to date" message
Your branches are already synced! No action needed.

### Issue: Merge conflicts in Program.cs
Keep your local port settings:
```csharp
var port = 5000; // Keep this from ample-exe
builder.WebHost.UseUrls($"http://localhost:{port}"); // Keep this
```

### Issue: SQLite error still appears
1. Stop the application
2. Delete `AmplePack.sqlite`
3. Restart application (database will regenerate)

### Issue: Application doesn't auto-open browser
Check `Program.cs` has the `OpenBrowser` method and the Task.Run block around line 145-150.

## ?? Success Indicators

You'll know the sync worked when:
1. ? No compilation errors
2. ? Application runs on localhost:5000
3. ? Reports page shows data without errors
4. ? Top customers sorted by revenue (no crash)
5. ? Low stock items sorted by quantity (no crash)

## ?? Related Documentation
- `README_LOCAL_DEPLOYMENT.md` - Local deployment guide
- `START_AMBLEPACK.bat` - Quick start script
- `BUILD_EXECUTABLE.ps1` - Build standalone .exe

---

**Ready to Deploy Locally?**
Just run: `START_AMBLEPACK.bat`

**Need Help?**
Check the commit history:
```powershell
git log --oneline --graph --all -20
```
