# Task Manager - ASP.NET Core MVC

A full-featured task management application built with ASP.NET Core MVC (.NET 7).

## Features

- 🔐 **Authentication**: Login, Register, Forgot Password, Reset Password
- 📊 **Dashboard**: Overview of tasks with statistics
- ✅ **Task Management**: Create, Read, Update, Delete tasks
- 🏷️ **Categories & Priorities**: Organize tasks with categories and priority levels
- 🔍 **Filtering**: Filter tasks by status, priority, and category
- 📱 **Responsive Design**: Works on desktop and mobile devices

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 7)
- **Database**: MySQL with Entity Framework Core
- **ORM**: Entity Framework Core 7.0 with Pomelo MySQL Provider
- **Authentication**: Cookie-based authentication
- **Password Hashing**: BCrypt.Net
- **Frontend**: Bootstrap 5, Bootstrap Icons

## Project Structure

```
TaskManagerMVC/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs    # Authentication
│   ├── DashboardController.cs  # Dashboard
│   ├── HomeController.cs       # Home/Error
│   └── TasksController.cs      # Task CRUD
├── Data/
│   └── AppDbContext.cs   # EF Core DbContext
├── Models/               # Entity Models
│   ├── User.cs
│   ├── TaskItem.cs
│   └── Lookups.cs        # Category, Priority, Status
├── Services/             # Business Logic
│   ├── AuthService.cs
│   └── TaskService.cs
├── ViewModels/           # View Models
│   ├── AccountVM.cs
│   └── TaskVM.cs
├── Views/                # Razor Views
│   ├── Account/
│   ├── Dashboard/
│   ├── Shared/
│   └── Tasks/
├── wwwroot/              # Static files
│   ├── css/
│   └── js/
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- MySQL Server (or MariaDB)

## Database Setup

1. Create the MySQL database:
```sql
CREATE DATABASE task_manager_db;
```

2. Run the database schema from `database/COMPLETE_DATABASE_SETUP.sql`
3. Run the stored procedures from `database/stored_procedures.sql`

3. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=task_manager_db;User=root;Password=your_password;"
  }
}
```

## Running the Application

### Development
```bash
cd TaskManagerMVC
dotnet restore
dotnet run
```

The application will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Production Build
```bash
dotnet publish -c Release
```

## API Endpoints (MVC Routes)

| Route | Method | Description |
|-------|--------|-------------|
| `/Account/Login` | GET/POST | User login |
| `/Account/Register` | GET/POST | User registration |
| `/Account/ForgotPassword` | GET/POST | Password reset request |
| `/Account/ResetPassword` | GET/POST | Password reset |
| `/Account/Logout` | POST | User logout |
| `/Dashboard` | GET | Dashboard view |
| `/Tasks` | GET | Task list |
| `/Tasks/Create` | GET/POST | Create task |
| `/Tasks/Edit/{id}` | GET/POST | Edit task |
| `/Tasks/Details/{id}` | GET | Task details |
| `/Tasks/Delete/{id}` | POST | Delete task |

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development/Production |
| `ConnectionStrings__DefaultConnection` | Database connection string |

## Security Features

- Password hashing with BCrypt
- Cookie-based authentication with configurable expiration
- Session management
- CSRF protection (built-in with ASP.NET Core)
- Authorization attributes on controllers

## License

MIT License
