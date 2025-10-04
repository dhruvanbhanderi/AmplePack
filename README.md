# AmplePack - Corrugated Box Manufacturing Management System

## Overview

AmplePack is a comprehensive web application built with ASP.NET Core MVC and .NET 9 for managing corrugated box manufacturing operations. The system provides tools for pricing, order management, customer relationship management, and business analytics.

## Features

### ?? **Box Price Calculator**
- Advanced pricing calculations for corrugated boxes
- Material cost optimization
- Sheet layout efficiency analysis
- Real-time price breakdowns with GST calculations
- Support for various board types and specifications

### ?? **Order Management**
- Complete order lifecycle management
- Customer order tracking
- Status updates and notifications
- Export capabilities (CSV, PDF)

### ?? **Customer Management**
- Customer database with contact information
- Customer-specific product catalogs
- Order history and analytics

### ?? **Inventory Management**
- Stock tracking and management
- Material requirements planning
- Supplier management

### ?? **Business Analytics**
- Sales and revenue reporting
- Customer analytics
- Production efficiency metrics
- Export and reporting tools

## Technical Stack

- **Framework**: ASP.NET Core MVC (.NET 9)
- **Database**: SQLite (development), SQL Server (production ready)
- **UI Framework**: AdminLTE 3.2 with Bootstrap 4
- **Authentication**: ASP.NET Core Identity
- **PDF Generation**: QuestPDF
- **Charts**: Chart.js
- **Icons**: Font Awesome 6

## Quick Start

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/dhruvanbhanderi/AmplePack.git
   cd AmplePack
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   cd AmplePack
   dotnet run
   ```

4. **Access the application**
   - Open your browser and navigate to `https://localhost:5001`
   - Default login credentials:
     - Username: `admin@ample.com`
     - Password: `Admin@123`

## Project Structure

```
AmplePack/
??? AmplePack/                 # Main application
?   ??? Controllers/           # MVC Controllers
?   ??? Models/               # Data models and DTOs
?   ??? Services/             # Business logic services
?   ??? Views/                # Razor views
?   ??? wwwroot/              # Static files (CSS, JS, images)
?   ??? Data/                 # Database context and migrations
??? docs/                     # Documentation
??? README.md                 # This file
```

## Key Components

### **Box Price Calculator**
The core feature that calculates accurate pricing for corrugated boxes based on:
- Box dimensions and specifications
- Material costs and waste percentages
- Production costs (printing, die-cutting, labor)
- Business parameters (overhead, profit margins)
- Sheet layout optimization for efficiency

### **Order Management System**
Complete order processing from creation to delivery:
- Order creation with customer selection
- Multi-item orders with different box specifications
- Status tracking and updates
- Invoice generation and export

### **Customer Relationship Management**
- Customer database with detailed profiles
- Order history and analytics
- Customer-specific pricing and products
- Communication tracking

## Configuration

### **Database Configuration**
The application uses SQLite by default for development. To configure SQL Server:

1. Update the connection string in `appsettings.json`
2. Run migrations: `dotnet ef database update`

### **Authentication**
The application uses ASP.NET Core Identity with role-based access control:
- **Admin**: Full system access
- **Manager**: Order and customer management
- **User**: Basic access to orders and calculator

## Features in Detail

### **Box Price Calculator**
- **Dimensions**: Length, width, height in inches
- **Materials**: Board type, GSM, compression ratio
- **Costs**: Material rates, printing, die-cutting, labor
- **Business Logic**: Overhead, profit margins, discounts, GST
- **Optimization**: Sheet layout efficiency analysis

### **Export Capabilities**
- **CSV Export**: Order data with proper formatting
- **PDF Export**: Professional reports and invoices
- **Indian Currency Support**: Proper Rs. formatting

### **Security Features**
- User authentication and authorization
- Role-based access control
- Secure session management
- Input validation and sanitization

## Development

### **Adding New Features**
1. Create models in `Models/` folder
2. Add business logic in `Services/`
3. Create controllers in `Controllers/`
4. Add views in `Views/`
5. Update database if needed

### **Running Tests**
```bash
dotnet test
```

### **Database Migrations**
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Deployment

### **Production Deployment**
1. Configure SQL Server connection string
2. Set up IIS or Kestrel server
3. Configure SSL certificates
4. Set environment variables
5. Run database migrations

### **Docker Deployment**
```bash
docker build -t amplepack .
docker run -p 80:80 amplepack
```

## Troubleshooting

### **Common Issues**

1. **Currency Symbol Issues**
   - Clear browser cache (Ctrl + Shift + R)
   - Ensure UTF-8 encoding is properly configured

2. **Database Connection Issues**
   - Check connection string in appsettings.json
   - Ensure database server is running
   - Run migrations if needed

3. **Authentication Issues**
   - Check user roles and permissions
   - Verify Identity configuration

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support and questions:
- Create an issue on GitHub
- Contact: [Your Contact Information]

## Changelog

### Version 1.0.0
- Initial release with core features
- Box price calculator with advanced algorithms
- Order management system
- Customer relationship management
- Export capabilities (CSV, PDF)
- User authentication and authorization

---

**AmplePack** - Streamlining corrugated box manufacturing management.