# 🏠 Household Manager
A web application for managing household chores and distributing responsibilities among residents.

## 📋 Overview
Household Manager is an ASP.NET Core MVC application designed to help families and roommates organize their living spaces, assign tasks, and track completion of household chores. The system supports multiple households, role-based permissions, and task assignment.

## ✨ Features

### Core Functionality
- **Multi-household support** - Users can create, join, and manage multiple households
- **Room management** - Organize your home by rooms with photos
- **Task scheduling** - Create one-time or recurring weekly tasks
- **Execution tracking** - Record task completion with photos and notes
- **Role-based access** - Owner and Member roles with different permissions

### User Management
- **Secure authentication** - ASP.NET Core Identity integration
- **User profiles** - Personal information and activity statistics
- **System administration** - Admin panel for user management
- **Invite system** - Unique codes for joining households

### Technical Features
- **Responsive design** - Bootstrap 5 for mobile-friendly interface
- **Data tables** - Advanced filtering and sorting with DataTables.js
- **File uploads** - Photo support for rooms and task executions
- **Modern UI** - Font Awesome icons and gradient buttons

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC 9.0
- **Architecture**: MVC (Model-View-Controller) Pattern
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **View Engine**: Razor
- **CSS Framework**: Bootstrap 5
- **JavaScript Libraries**: jQuery, DataTables.js
- **Icons**: Font Awesome 6
- **Testing**: NUnit 3 with Moq for unit testing

## 📦 Prerequisites

- .NET 9.0 SDK
- MS SQL Server

## 🚀 Setup Steps

1. **Clone the repository**
```bash
git clone https://github.com/rsukhaniuk/univ-info-networks-lab1.git
cd HouseholdManager
```

2. **Update connection string** (if needed)

Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HouseholdManagerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

3. **Create database**
```bash
dotnet ef database update
```

4. **Run the application**
```bash
dotnet run
```

5. **Access the app**

Navigate to `https://localhost:7047` (or the port shown in console)

## 👤 Admin Account

- Email: `admin@example.com`
- Password: `Admin123$`

## 🧪 Testing

The project includes comprehensive unit tests using **NUnit 3** testing framework with **Moq** for dependency mocking.

### Run Tests
```bash
# Navigate to test project
cd HouseholdManager.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~HouseholdServiceTests"
```
