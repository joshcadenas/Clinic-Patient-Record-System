# Clinic Patient Record System - Web + Windows Forms + API + MySQL

This version has three parts:

1. `ClinicApi` - ASP.NET Core C# API with Swagger and MySQL connection.
2. `Web` - HTML/CSS/JavaScript web dashboard.
3. `WindowsClient` - C# Windows Forms dashboard with the same clinic theme and functions.

Both the Web dashboard and the Windows Forms dashboard connect to the same API:

```text
Web Dashboard       -> ASP.NET Core API -> MySQL database
Windows Forms App   -> ASP.NET Core API -> MySQL database
```

Because both clients use the same API and database, changes made in Windows will appear in the Web after refresh, and changes made in the Web will appear in Windows after refreshing/reopening the feature.

## Default accounts

```text
Admin: admin / admin123
User:  user  / user123
```

## Requirements

- Windows OS for the Windows Forms app
- Visual Studio Code
- .NET 9 SDK
- XAMPP or MySQL Server
- Live Server VS Code extension for the web dashboard

## 1. Start MySQL

Open XAMPP and start MySQL.

Create the database in phpMyAdmin:

```sql
CREATE DATABASE IF NOT EXISTS clinic_patient_db;
```

Check the API connection string in:

```text
ClinicApi/appsettings.json
```

For XAMPP with no password, use:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=clinic_patient_db;User=root;Password=;"
```

## 2. Run the API first

In VS Code terminal:

```powershell
cd ClinicPatientRecordSystem\ClinicApi
dotnet restore
dotnet run
```

Open Swagger:

```text
http://localhost:5000/swagger
```

Keep the API terminal open.

## 3. Run the Web dashboard

Open:

```text
Web/index.html
```

Right-click and choose:

```text
Open with Live Server
```

Login using admin/admin123 or user/user123.

## 4. Run the Windows Forms dashboard

Open a new VS Code terminal:

```powershell
cd ClinicPatientRecordSystem\WindowsClient
dotnet restore
dotnet run
```

Login using admin/admin123 or user/user123.

## Important sync note

The system is not using real-time WebSocket auto-refresh. It is synchronized through the shared API and database. If you delete or update data in Windows, refresh the Web feature/page to see it. If you update in Web, reopen or refresh the Windows feature to see it.
