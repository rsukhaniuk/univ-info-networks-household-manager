# 🏠 Household Manager

A full-stack web application for managing household chores, organizing rooms, and distributing responsibilities among residents. Built with Angular SPA and .NET Web API, featuring Auth0 authentication, automatic task assignment with load balancing, and iCalendar export functionality.

## 📋 Overview

Household Manager helps families and roommates organize their living spaces through task management and role-based access control.

### Core Capabilities

- **Multi-household support** - Users can belong to multiple households simultaneously (as owner or member)
- **Authentication & Authorization** - Secure Auth0 integration with JWT tokens
- **Room management** - Organize spaces with photos
- **Task system** - Recurring (RRULE-based) and one-time tasks with priorities
- **Automatic task assignment** - Load-balanced distribution with preview, considering weekly workload
- **Task execution tracking** - Complete tasks with photos and notes, full execution history
- **iCalendar export** - Download .ics files or subscribe via URL with token-based authentication
- **Role-based permissions** - Owner/Member at household level, SystemAdmin/User at system level
- **Profile management** - Edit user data, change password/email via Auth0 Management API

## ✨ Key Features

### 🏘️ Household Management
- Create and manage multiple households with different roles
- **Invitation system** - Generate unique codes (valid for 24 hours)
- **Ownership transfer** - Owner can transfer rights to another member
- **Member management** - Owners can remove members, members can leave voluntarily
- Users can have different roles across different households

### 📅 Task System
- **Two task types**:
  - **Recurring tasks** - Based on RecurrenceRule (RRULE) for flexible scheduling
  - **One-time tasks** - With specific deadlines, auto-deactivate after completion
- **Priority levels** - Organize tasks by importance
- **Automatic assignment** - Smart load balancing algorithm that:
  - Considers active and completed tasks for the week
  - Prevents overloading productive users
  - Provides preview before assignment
- **Task execution** - Complete with photos and notes
- **Execution invalidation** - Owners can invalidate current period executions
- **Complete history** - Track all task completions over time

### 📆 iCalendar Integration
- **Export formats**:
  - Download .ics file directly
  - Subscribe via URL (updates automatically)
- **Token-based authentication** - Secure calendar subscriptions
- Compatible with Google Calendar, Apple Calendar, Outlook, etc.

### 🔐 Security & Authentication
- Auth0 integration for secure authentication
- JWT-based API authorization
- Role-based access control (RBAC):
  - **System level**: SystemAdmin, User
  - **Household level**: Owner, Member
- Password change with mandatory re-authentication
- Email change via Auth0 Management API
- Account deletion with dependency validation

### 👤 Profile Management
- Edit personal information
- Change password (requires re-authentication)
- Change email via Auth0 Management API
- Delete account with validation blockers
- View statistics: tasks created, completed, total executions

## 🛠️ Technology Stack

### Frontend
- **Framework**: Angular 20
- **Forms**: Reactive Forms
- **UI Library**: PrimeNG components
- **CSS**: Bootstrap 5
- **Date/Time**: Flatpickr
- **Recurrence**: rrule.js for RRULE handling
- **Authentication**: Auth0 Angular SDK
- **Language**: TypeScript
- **Reactive Programming**: RxJS
- **Icons**: Font Awesome, PrimeIcons

### Backend
- **Platform**: .NET 9.0
- **Framework**: ASP.NET Core Web API
- **Architecture**: Clean Architecture (Domain, Application, API layers)
- **Authentication**: Auth0 Management API
- **ORM**: Entity Framework Core 9
- **Database**: PostgreSQL
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Calendar**: Ical.Net for iCalendar generation
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI
- **Testing**: NUnit 3, Moq

### Infrastructure & DevOps
- **Containerization**: Docker, Docker Compose
- **Web Server**: nginx (frontend)
- **SSL/TLS**: mkcert for local HTTPS development
- **Cloud Platform**: AWS
  - **App Runner** - Backend API hosting
  - **ECR** - Docker image registry
  - **RDS PostgreSQL** - Managed database
  - **Amplify** - Frontend SPA hosting
- **CI/CD**: GitHub Actions
- **Performance Testing**: k6
- **Security Testing**: OWASP ZAP, npm audit, dotnet vulnerability scanning

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **PostgreSQL 16** - [Download](https://www.postgresql.org/download/)
- **Docker** (optional, for containerized setup) - [Download](https://www.docker.com/)

### Option 1: Docker Compose (Recommended)

```bash
# 1. Clone repository
git clone https://github.com/rsukhaniuk/univ-info-networks-household-manager.git
cd univ-info-networks-household-manager

# 2. Create environment file
cp .env.example .env

# 3. Edit .env and configure Auth0 credentials:
#    - AUTH0_CLIENT_ID
#    - AUTH0_CLIENT_SECRET
#    - AUTH0_MGMT_CLIENT_ID
#    - AUTH0_MGMT_CLIENT_SECRET

# 4. Generate SSL certificates for HTTPS (required, first time only)
# Install mkcert: https://github.com/FiloSottile/mkcert
mkcert -install
mkcert -key-file certs/frontend/localhost+1-key.pem -cert-file certs/frontend/localhost+1.pem localhost 127.0.0.1 ::1

# 5. Start all services (API, Frontend, PostgreSQL)
docker-compose up

# 6. Access the application
# Frontend: https://localhost:4200
# Backend API: https://localhost:7047
# Swagger UI: https://localhost:7047/swagger
```

**Note**: SSL certificates are required for HTTPS. See [certs/README.md](certs/README.md) for detailed certificate setup instructions.

### Option 2: Manual Setup

#### Backend Setup

```bash
cd backend/src/HouseholdManager.Api

# 1. Configure secrets in appsettings.json or via environment variables:
#    - ConnectionStrings.DefaultConnection (PostgreSQL)
#    - Auth0.ClientSecret
#    - Auth0.ManagementApiClientSecret

# 2. Apply database migrations
dotnet ef database update

# 3. Run the API
dotnet run
```

#### Frontend Setup

```bash
cd frontend/household-manager-ui

# 1. Install dependencies
npm install

# 2. Start development server
npm start

# Frontend available at: http://localhost:4200
```

## 🏗️ Project Structure

```
.
├── backend/
│   └── src/
│       ├── HouseholdManager.Api/              # Web API layer (controllers, DTOs)
│       ├── HouseholdManager.Application/      # Business logic (services)
│       ├── HouseholdManager.Domain/           # Domain entities & interfaces
│       └── HouseholdManager.Infrastructure/   # Data access (EF Core, repositories)
├── frontend/
│   └── household-manager-ui/                  # Angular SPA
│       ├── src/
│       │   ├── app/
│       │   │   ├── components/                # UI components
│       │   │   ├── services/                  # API services
│       │   │   ├── models/                    # TypeScript models
│       │   │   ├── guards/                    # Route guards
│       │   │   └── interceptors/              # HTTP interceptors
│       │   └── environments/                  # Environment configs
├── tests/                                     # Test projects
├── performance-tests/                         # k6 load tests
│   ├── smoke-test.js
│   ├── load-test.js
│   ├── stress-test.js
│   └── results/                               # Test results
├── certs/                                     # SSL/TLS certificates (local dev)
│   └── frontend/                              # nginx & backend certificates (*.pem - not in git)
├── security-reports/                          # Security audit reports
├── lab1-mvc-legacy/                           # Lab 1: Legacy MVC version
├── docker-compose.yml                         # Docker services config
├── HouseholdManager.sln                       # .NET solution file
├── .env.example                               # Environment template
└── .env.k6.example                            # k6 test environment template
```

## 🔐 Configuration

### Environment Variables

The application uses environment-specific configuration:

| Environment | Backend Config | Frontend Config | Database |
|------------|---------------|-----------------|----------|
| **Local Development** | `appsettings.json` | `environment.ts` | `localhost:5432` |
| **Docker Compose** | `.env` → env vars | `environment.ts` | `postgres:5432` |
| **AWS Production** | App Runner env vars | `environment.aws.ts` (build-time) | AWS RDS |

### Auth0 Configuration

Required Auth0 settings:
- **Domain**: `household-manager-dev.eu.auth0.com`
- **Client ID**: (public, in frontend config)
- **Client Secret**: (backend only, never in frontend!)
- **Management API Client ID & Secret**: (backend only)

### Configuration Files

Create configuration from examples:

```bash
# Docker environment
cp .env.example .env
# Edit .env and add your Auth0 secrets

# k6 performance tests (optional)
cp .env.k6.example .env.k6
# Edit .env.k6 and add K6_CLOUD_TOKEN and AUTH0_TOKEN
```

**Backend configuration:**
- Edit `backend/src/HouseholdManager.Api/appsettings.json` to add your secrets
- Or use environment variables (recommended for production)

**Important**: Be careful with secrets in `appsettings.json` - consider using environment variables or user secrets for sensitive data

## 🧪 Testing

### Unit Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend/household-manager-ui
npm test
```

### Performance Tests (k6)

```bash
# Setup
cp .env.k6.example .env.k6
# Edit .env.k6 with AUTH0_TOKEN and K6_CLOUD_TOKEN

# Run tests
cd performance-tests
k6 run smoke-test.js      # Quick smoke test
k6 run load-test.js       # Standard load test
k6 run stress-test.js     # Stress test
```

### Security Testing

```bash
# npm audit (frontend)
cd frontend/household-manager-ui
npm audit

# .NET vulnerability scan (backend)
cd backend
dotnet list package --vulnerable

# OWASP ZAP - manual security testing
```

## 📊 Performance Metrics

The application has been performance tested with k6 on both localhost (Docker) and AWS deployments. Test results demonstrate stable performance under various load conditions.

### Test Results Summary

| Environment | Test Type | Avg Response Time (p95) | Requests/sec | Success Rate |
|-------------|-----------|------------------------|--------------|--------------|
| **Localhost (Docker)** | Smoke Test | 5.81ms | 1.99 req/s | 100% |
| **AWS (App Runner)** | Smoke Test | 50.5ms | 1.78 req/s | 100% |

**Detailed Results:**
- [Localhost Docker tests](performance-tests/results/localhost/) - smoke, load, and stress tests
- [AWS deployment tests](performance-tests/results/aws/) - smoke, load, and stress tests

## 🔒 Security Features

Security implementation:
- JWT-based authentication with Auth0
- Role-based authorization (Owner/Member roles)
- Security headers (CSP, HSTS, X-Frame-Options, etc.)
- CORS policy configuration
- Input validation and sanitization
- SQL injection protection (EF Core parameterized queries)
- XSS protection with Angular's built-in sanitization

Security testing performed:
- **OWASP ZAP** - Automated vulnerability scanning
- **npm audit** - Frontend dependency vulnerability checks
- **.NET vulnerability scanning** - Backend NuGet package checks
- Manual penetration testing

## 🌐 Deployment

### AWS Deployment

The application is deployed on AWS using:
- **App Runner** - Backend API hosting with automatic scaling
- **ECR** - Docker image registry
- **RDS PostgreSQL** - Managed database service
- **Amplify** - Frontend SPA hosting with automatic builds
- **GitHub Actions** - CI/CD for backend deployment

### Docker Deployment

```bash
# Build and run all services
docker-compose up --build

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down
```

## 🎓 Academic Context

**This is a study project for the Information Networks course**, integrating all 5 laboratory assignments into a unified full-stack application. Each lab represents a specific aspect of web development:

- **Lab 1** - Backend/MVC (legacy version)
- **Lab 2** - Frontend SPA
- **Lab 3** - Web API/REST services
- **Lab 4** - Deployment & Performance
- **Lab 5** - Security Testing

This project was developed as part of the **Information Networks** course, evolving through multiple laboratory assignments:

### Lab 1: MVC Architecture
**Legacy version in `lab1-mvc-legacy/` folder**

**Technologies:**
- ASP.NET Core MVC 9.0 with Razor Views
- Microsoft SQL Server with Entity Framework Core
- ASP.NET Core Identity for authentication
- Bootstrap 5 for UI
- NUnit 3 + Moq for testing

**Features:** Basic household management, rooms, simple task system, user authentication

---

### Lab 2-3: SPA Migration
**Current architecture**

**Major changes:**
- Migrated from MVC to **Angular 20 SPA**
- Replaced server-side rendering with client-side SPA
- Migrated from SQL Server to **PostgreSQL**
- Replaced ASP.NET Identity with **Auth0**
- Implemented Clean Architecture (Domain, Application, Infrastructure, API layers)
- Added **Swagger/OpenAPI** documentation

**New features:**
- Recurring tasks with RRULE support and one-time tasks with auto-deactivation
- Automatic task assignment with load balancing and preview
- Task execution invalidation capability for household owners
- Ownership transfer between household members
- Time-limited invitation codes (24 hours expiry)
- iCalendar export (download and subscription with token-based auth)
- Enhanced profile management (password/email change via Auth0 Management API)
- Multi-level role-based access control (Owner/Member, SystemAdmin/User)

---

### Lab 4: Performance & Cloud Deployment

**Added technologies:**
- Docker containerization with Docker Compose
- SSL/TLS certificates (mkcert for local dev, ACM for production)
- nginx web server for frontend serving
- AWS deployment (App Runner, RDS, Amplify, ECR)
- GitHub Actions CI/CD pipeline
- k6 performance testing framework

**Focus:** Cloud infrastructure, performance optimization, automated deployment, HTTPS support

---

### Lab 5: Security Testing

**Security tools:**
- OWASP ZAP for vulnerability scanning
- npm audit for frontend security
- dotnet vulnerability scanning for backend

**Focus:** Security audit, vulnerability assessment, secure coding practices

## 👤 Author

**Roman Sukhaniuk**
- GitHub: [@rsukhaniuk](https://github.com/rsukhaniuk)
