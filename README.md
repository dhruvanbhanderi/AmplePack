# AmplePack - Packaging Solutions Management System

A comprehensive .NET 9 web application for managing packaging business operations including orders, customers, inventory, and reports.

## ?? Features

- **Order Management**: Create, track, and manage customer orders with detailed workflow
- **Customer Management**: Maintain customer database with contact information and order history
- **Inventory Management**: Track packaging materials, quantities, and reorder levels
- **Customer Products**: Manage customer-specific product catalogs and pricing
- **Reports & Analytics**: Comprehensive reporting dashboard with revenue and inventory insights
- **User Management**: Role-based access control (Admin, Manager, Staff)
- **PDF Invoice Generation**: Professional invoice generation with QuestPDF

## ??? Technology Stack

- **Framework**: ASP.NET Core 9.0 (MVC)
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI Framework**: AdminLTE 3.2 with Bootstrap 4
- **PDF Generation**: QuestPDF
- **Icons**: Font Awesome 6

## ?? Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- SQLite (included with EF Core)

## ?? Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/dhruvanbhanderi/AmplePack.git
   cd AmplePack
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - Use default admin credentials: `Admin@ample.com` / `Admin@123`

## ?? User Roles & Permissions

| Feature | Admin | Manager | Staff |
|---------|-------|---------|-------|
| View Orders | ? | ? | ? |
| Create/Edit Orders | ? | ? | ? |
| Delete Orders | ? | ? | ? |
| Customer Management | ? | ? | ? |
| Inventory Management | ? | ? | ? |
| Reports Access | ? | ? | ? |
| User Management | ? | ? | ? |

## ?? Project Structure

```
AmplePack/
??? Controllers/           # MVC Controllers
??? Models/               # Data models
??? ViewModels/           # View models for forms
??? Views/                # Razor views
??? Data/                 # Database context and seeders
??? Services/             # Business logic services
??? Helpers/              # Utility classes
??? Migrations/           # EF Core migrations
??? wwwroot/              # Static files
??? Program.cs            # Application entry point
```

## ?? Configuration

### Database Connection
The application uses SQLite by default. Connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AmplePack.sqlite"
  }
}
```

### Authentication
- Cookie-based authentication with 60-minute sliding expiration
- Role-based authorization
- Secure password requirements

## ?? Key Features Detail

### Order Management
- Complete order workflow from creation to completion
- Customer product integration
- PDF invoice generation
- Order status tracking

### Inventory Management
- Stock level monitoring
- Reorder level alerts
- Inventory valuation
- Low stock reporting

### Reporting Dashboard
- Revenue analytics
- Customer insights
- Inventory reports
- Monthly revenue charts

## ?? Security Features

- Role-based access control
- Anti-forgery token protection
- Secure authentication cookies
- Input validation and sanitization

## ?? License

This project is licensed under the MIT License.

## ?? Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## ?? Support

For support and questions, please contact the development team.

---

**AmplePack** - Your trusted partner for packaging solutions management! ??