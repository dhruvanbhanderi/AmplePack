# ?? **AmplePack Mobile Deployment Guide**

## ?? **Quick Deploy Options for Mobile Access**

### **Option 1: Railway (Easiest - 1-Click Deploy)**

1. **Visit**: https://railway.app
2. **Sign up** with GitHub
3. **Connect your AmplePack repository**
4. **Add Environment Variables**:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=Host=db.qhxspucpkewmeaztfvve.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=Ample@123;SSL Mode=Require;Trust Server Certificate=true
   ```
5. **Deploy automatically** - Railway detects .NET and builds/deploys
6. **Access on mobile**: `https://your-app-name.railway.app`

---

### **Option 2: Heroku**

1. **Install Heroku CLI**
2. **Login**: `heroku login`
3. **Create app**: `heroku create amplepack-app`
4. **Set buildpack**: `heroku buildpacks:set https://github.com/jincod/dotnetcore-buildpack`
5. **Add config vars**:
   ```bash
   heroku config:set ASPNETCORE_ENVIRONMENT=Production
   heroku config:set ConnectionStrings__DefaultConnection="Host=db.qhxspucpkewmeaztfvve.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=Ample@123;SSL Mode=Require;Trust Server Certificate=true"
   ```
6. **Deploy**: `git push heroku mvc-conversion:main`
7. **Access**: `https://amplepack-app.herokuapp.com`

---

### **Option 3: Render**

1. **Visit**: https://render.com
2. **Connect GitHub repository**
3. **Create Web Service**
4. **Configure**:
   - Build Command: `dotnet restore && dotnet build -c Release`
   - Start Command: `dotnet AmplePack.dll`
   - Environment: Add connection string
5. **Deploy automatically**
6. **Access**: `https://amplepack.onrender.com`

---

### **Option 4: Azure App Service**

1. **Azure Portal**: Create App Service
2. **Runtime**: .NET 9
3. **Deploy via**:
   - Visual Studio (Right-click ? Publish)
   - GitHub Actions (Auto-deploy on push)
   - Azure CLI: `az webapp up --name amplepack --resource-group myResourceGroup`
4. **Access**: `https://amplepack.azurewebsites.net`

---

## ?? **Mobile Access Instructions**

Once deployed, users can access your app on mobile devices:

### **?? For End Users:**
1. **Open mobile browser** (Chrome, Safari, Firefox)
2. **Navigate to your app URL**
3. **Add to Home Screen**:
   - **iPhone**: Safari ? Share ? Add to Home Screen
   - **Android**: Chrome ? Menu ? Add to Home Screen

### **?? Default Login Credentials:**
- **Email**: `Admin@ample.com`
- **Password**: `Admin@123`

---

## ?? **Local Testing Before Deploy**

Test with PostgreSQL locally:

```bash
# Update appsettings.json to use Supabase connection
# Run the application
dotnet run

# Test on mobile (same WiFi):
# Find your PC IP: ipconfig (Windows) or ifconfig (Mac/Linux)
# Mobile browser: http://YOUR-PC-IP:5000
```

---

## ?? **Features Working on Mobile:**

? **Responsive Design** - Works on all screen sizes  
? **Touch-Friendly Interface** - Optimized for mobile interaction  
? **PWA Capabilities** - Can be installed as an app  
? **Full Functionality** - All features available on mobile  
? **Authentication** - Secure login system  
? **Real-time Data** - Connected to Supabase PostgreSQL  

---

## ?? **Troubleshooting**

### **Database Connection Issues:**
- Verify Supabase credentials
- Check network connectivity
- Ensure SSL certificate settings

### **Mobile Display Issues:**
- Clear browser cache
- Check responsive CSS
- Test different browsers

### **Performance:**
- Use CDN for static assets
- Enable compression
- Optimize images

---

## ?? **Monitoring Your App**

- **Railway**: Built-in metrics and logs
- **Heroku**: Heroku CLI logs
- **Azure**: Application Insights
- **Render**: Real-time logs

Your AmplePack application will be fully functional on mobile devices once deployed! ??