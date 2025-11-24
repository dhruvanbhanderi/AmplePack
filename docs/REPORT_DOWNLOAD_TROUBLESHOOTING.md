# Report Download Issue - Troubleshooting Guide

## Issue: Downloaded Report Files Not Opening

### Common Causes & Solutions

#### 1. **Browser Download Settings**

**Problem**: Browser is blocking or corrupting PDF downloads
**Solution**:
```
- Check browser download settings
- Disable popup blockers for localhost
- Clear browser cache (Ctrl+Shift+Del)
- Try downloading in incognito/private mode
```

#### 2. **PDF Viewer Issues**

**Problem**: Default PDF viewer cannot open the file
**Solution**:
```
- Right-click downloaded file ? "Open with" ? Adobe Reader/Chrome
- Update your PDF viewer software
- Try opening with different PDF reader
- Check file size - if 0 bytes, generation failed
```

#### 3. **File Corruption During Download**

**Problem**: Network interruption or server error during download
**Solution**:
```
- Check browser's Developer Tools (F12) for console errors
- Look for network errors in Network tab
- Retry download after clearing browser cache
- Check file extension is .pdf
```

#### 4. **Server-Side Issues**

**Problem**: QuestPDF library or memory issues
**Solution**:
```
- Check application logs for errors
- Restart the application
- Ensure QuestPDF license is properly configured
- Check available memory on server
```

### Quick Debugging Steps

1. **Open Browser Developer Tools (F12)**
   - Go to Console tab
   - Try downloading a report
   - Look for JavaScript errors in red

2. **Check Network Tab**
   - Refresh page
   - Try downloading report
   - Look for failed requests or error responses

3. **Verify File Download**
   ```
   - Check Downloads folder
   - Look at file size (should be > 0 bytes)
   - Check file name includes .pdf extension
   - Try opening with different PDF viewers
   ```

4. **Test Different Report Types**
   ```
   - Try Orders report
   - Try Customers report  
   - Try Inventory report
   - If one works, issue is data-specific
   ```

### Advanced Troubleshooting

#### Enable Detailed Logging
Add this to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AmplePack.Controllers.ReportsController": "Debug",
      "AmplePack.Services.EnhancedReportService": "Debug"
    }
  }
}
```

#### Check for Common Errors

1. **"PDF generation failed" Error**
   - QuestPDF license issue
   - Memory limitations
   - Invalid data causing PDF generation to fail

2. **"File is empty" Error**
   - No data matching filter criteria
   - Database connection issue
   - Service method returning null

3. **"Download failed" Error**
   - Network connectivity issues
   - Server timeout
   - Large dataset causing memory issues

### Browser-Specific Issues

#### Chrome
- Disable "Ask where to save each file before downloading"
- Check Downloads section in Settings
- Clear site data for localhost

#### Firefox  
- Check Downloads preferences
- Ensure PDF viewer is enabled
- Try with tracking protection disabled

#### Edge
- Check download settings
- Ensure PDFs open in default viewer
- Clear browsing data

### Data-Related Issues

#### Large Reports
- Limit date range to reduce data size
- Use specific filters to reduce record count
- Current limit: 100 records per PDF

#### Empty Reports
- Verify data exists for selected filters
- Check date range settings
- Ensure database has sample data

### Contact Support

If issue persists:
1. **Collect Information**:
   - Browser type and version
   - Operating system
   - Error messages from console
   - Steps to reproduce

2. **Check Logs**:
   - Application logs in Debug output
   - Browser console errors
   - Network tab responses

3. **Test Environment**:
   - Try on different browser
   - Try on different computer
   - Test with different report types

---

## Quick Fixes Summary

1. **Clear browser cache and try again**
2. **Use different PDF viewer to open file**
3. **Check file size in Downloads folder**
4. **Try downloading different report type**
5. **Open browser Developer Tools for errors**
6. **Try incognito/private browsing mode**

---

*Last Updated: January 2025*