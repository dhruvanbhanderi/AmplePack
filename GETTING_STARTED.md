# AmplePack - Getting Started Guide

## Quick Setup (5 minutes)

### Prerequisites
- .NET 9 SDK ([Download here](https://dotnet.microsoft.com/download))
- Git
- A code editor (Visual Studio, VS Code, or any text editor)

### 1. Clone and Setup
```bash
# Clone the repository
git clone https://github.com/dhruvanbhanderi/AmplePack.git
cd AmplePack

# Restore packages and build
dotnet restore
dotnet build
```

### 2. Run the Application
```bash
# Navigate to the main project
cd AmplePack

# Run the application
dotnet run
```

### 3. Access the Application
Open your browser and go to: `https://localhost:7153` or `http://localhost:5216`

### 4. Default Login
- **Email**: `admin@ample.com`
- **Password**: `Admin@123`

## Features Overview

### ?? Box Price Calculator
- Advanced pricing calculations for corrugated boxes
- Material cost optimization with waste factor calculations
- Sheet layout efficiency analysis
- Real-time price breakdowns with GST
- Support for various board types (3-ply, 5-ply, 7-ply)

### ?? Order Management
- Complete order lifecycle from creation to completion
- Multi-item orders with different specifications
- Customer selection and order tracking
- Status updates (Pending ? In Progress ? Completed)
- Export capabilities (CSV, PDF invoices)

### ?? Customer Management
- Comprehensive customer database
- Contact information and order history
- Customer-specific product catalogs
- Order analytics per customer

### ?? Inventory Management
- Stock tracking and management
- Material requirements planning
- Low stock alerts and reorder points
- Supplier management

### ?? Business Analytics & Reports
- Sales and revenue reporting
- Customer analytics and insights
- Production efficiency metrics
- Exportable reports (CSV, PDF)
- Interactive charts and dashboards

## Project Structure

```
AmplePack/
??? AmplePack/                 # Main web application
?   ??? Controllers/           # MVC Controllers
?   ?   ??? HomeController.cs      # Dashboard and home page
?   ?   ??? OrdersController.cs    # Order management
?   ?   ??? CustomersController.cs # Customer management
?   ?   ??? InventoryController.cs # Inventory management
?   ?   ??? ReportsController.cs   # Business reports
?   ?   ??? BoxCalculatorController.cs # Price calculator
?   ??? Models/                # Data models and DTOs
?   ?   ??? Order.cs              # Order entities
?   ?   ??? Customer.cs           # Customer entities
?   ?   ??? Inventory.cs          # Inventory entities
?   ?   ??? BoxRateCalculatorModels.cs # Calculator models
?   ??? Services/              # Business logic services
?   ?   ??? OrderManagementService.cs # Order processing
?   ?   ??? InvoiceService.cs        # Invoice generation
?   ?   ??? EnhancedReportService.cs # Report generation
?   ?   ??? AdvancedBoxRateCalculatorService.cs # Pricing logic
?   ??? Views/                 # Razor views
?   ?   ??? Home/                  # Dashboard views
?   ?   ??? Orders/                # Order management views
?   ?   ??? Customers/             # Customer management views
?   ?   ??? Inventory/             # Inventory views
?   ?   ??? Reports/               # Report views
?   ??? Data/                  # Database context and seeding
?   ?   ??? AppDbContext.cs        # Entity Framework context
?   ?   ??? DbSeeder.cs            # Sample data seeding
?   ?   ??? IdentitySeeder.cs      # User and role seeding
?   ??? wwwroot/               # Static files (CSS, JS, images)
??? AmplePack.Tests/           # Unit and integration tests
??? docs/                      # Documentation
??? README.md                  # Main documentation
```

## Key Features Explained

### Box Price Calculator Algorithm
The calculator uses sophisticated algorithms to determine accurate pricing:

1. **Material Calculations**:
   - Board type selection (3-ply, 5-ply, 7-ply)
   - GSM (Grams per Square Meter) considerations
   - Waste factor calculations (typically 8-12%)

2. **Sheet Optimization**:
   - Calculates optimal sheet layout
   - Maximizes material utilization
   - Considers cutting patterns and efficiency

3. **Cost Breakdown**:
   - Material costs (board, adhesive)
   - Production costs (printing, die-cutting, labor)
   - Business overhead and profit margins
   - GST calculations (18% in India)

4. **Advanced Features**:
   - Compression ratio calculations
   - Edge crush test considerations
   - Volume discounts
   - Customer-specific pricing

### Order Management Workflow
1. **Order Creation**: Select customer, add products with specifications
2. **Processing**: Track order through production stages
3. **Status Updates**: Real-time status tracking
4. **Completion**: Generate invoices and delivery notes
5. **Export**: CSV and PDF export capabilities

### Inventory Management Features
- **Stock Tracking**: Real-time inventory levels
- **Reorder Alerts**: Automatic low stock notifications
- **Material Planning**: Calculate material requirements for orders
- **Supplier Management**: Track supplier information and costs

## Configuration

### Database Configuration
The application uses SQLite by default for development. For production with SQL Server:

1. Update connection string in `appsettings.Production.json`
2. Run migrations: `dotnet ef database update`

### Authentication Setup
- Uses ASP.NET Core Identity
- Role-based access control (Admin, Manager, User)
- Default admin user is created automatically

### Customization
- Modify pricing parameters in `BoxRateCalculatorModels.cs`
- Adjust business rules in service classes
- Customize UI themes in `wwwroot/css/`

## API Endpoints

The application provides REST API endpoints:

- `GET /api/orders` - Get all orders
- `GET /api/customers` - Get all customers
- `POST /api/calculate-box-rate` - Calculate box pricing
- `GET /api/reports/sales` - Get sales reports

## Testing

Run the comprehensive test suite:
```bash
dotnet test
```

Test categories:
- Unit tests for business logic
- Integration tests for controllers
- Database tests
- API endpoint tests

## Deployment Options

### 1. Traditional IIS Deployment
```bash
# Use the deployment script
./deploy.bat  # Windows
./deploy.sh   # Linux/Mac
```

### 2. Docker Deployment
```bash
# Build and run with Docker
docker build -t amplepack .
docker run -p 80:80 amplepack

# Or use docker-compose
docker-compose up
```

### 3. Cloud Deployment
- Azure App Service
- AWS Elastic Beanstalk
- Google Cloud Run

## Troubleshooting

### Common Issues

1. **Currency Symbol Not Displaying**
   - Clear browser cache (Ctrl + Shift + R)
   - Check UTF-8 encoding configuration

2. **Database Connection Issues**
   - Verify connection string in appsettings.json
   - Ensure database server is running
   - Run database migrations

3. **Authentication Problems**
   - Check user roles and permissions
   - Verify Identity configuration
   - Clear browser cookies

### Getting Help
1. Check the documentation in the `docs/` folder
2. Review the test files for usage examples
3. Check the GitHub issues page
4. Contact the development team

## Next Steps
1. Explore the dashboard features
2. Try creating a customer and order
3. Use the box price calculator
4. Generate some reports
5. Customize the application for your needs

---

Welcome to **AmplePack** - Your complete packaging business management solution!