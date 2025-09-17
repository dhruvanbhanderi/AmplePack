# Deploy AmplePack to Azure App Service

## Prerequisites
- Azure account (free tier available)
- Azure CLI installed or use Azure portal

## Step 1: Update Connection String for Production

Update your `appsettings.Production.json` to use Supabase PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:[YOUR-PASSWORD]@db.qhxspucpkewmeaztfvve.supabase.co:5432/postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Step 2: Create Azure App Service

### Using Azure Portal:
1. Go to portal.azure.com
2. Create new "App Service"
3. Configure:
   - Resource Group: Create new
   - Name: `amplepack-app` (must be globally unique)
   - Runtime: .NET 9
   - OS: Linux (cheaper)
   - Region: Choose closest to you
   - Pricing: Free F1 or Basic B1

## Step 3: Deploy Your App

### Method A: Visual Studio Publish
1. Right-click project ? Publish
2. Target: Azure
3. Specific target: Azure App Service (Linux)
4. Select your app service
5. Publish

### Method B: GitHub Actions (Recommended)
1. Connect your GitHub repo to Azure App Service
2. Azure will auto-create GitHub Actions workflow
3. Every push to main branch auto-deploys

## Step 4: Configure Environment
In Azure App Service ? Configuration ? Application Settings:
- Add connection string
- Set ASPNETCORE_ENVIRONMENT = Production

## Step 5: Access on Mobile
Your app will be available at:
`https://amplepack-app.azurewebsites.net`

Access this URL from any mobile browser!