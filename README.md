# FGI CRM 

## Overview
This repository contains the complete **Role-based User Profile & Settings System** for the **FGI CRM application**.

The system provides:
- Role-based dashboards (Admin, Marketing, Sales)
- KPI-driven profile views (read-only)
- Secure account management via a dedicated Settings module
- Clean separation between profile visualization and account editing

---

## Supported Roles
- **Admin**
- **Marketing**
- **Sales**

Each role has its own KPIs, activities, and UI layout.

---

## System Architecture
User
├── Profile (Read-only)
│ ├── KPIs
│ ├── Recent Activities
│ └── Role-based Dashboard
│
└── Settings (Editable)
├── Full Name
├── Email Username
└── Password

---

## View Models (`FGI/ViewModels/`)

### BaseProfileViewModel
Common profile data for all roles.

**Properties**
- UserId  
- FullName  
- Email  
- Role  
- CreatedAt  
- LastLoginAt  
- TotalDaysInSystem  
- AccountStatus  

---

### MarketingProfileViewModel
Marketing KPIs and activities.

**KPIs**
- LeadsCreated  
- UnitsAdded  
- FeedbacksGiven  
- LeadToUnitConversionRate  
- ActiveLeads  
- CompletedLeads  

**Activities**
- RecentLeads (Last 5)

---

### SalesProfileViewModel
Sales KPIs and activities.

**KPIs**
- AssignedLeads  
- LeadsConvertedToUnits  
- UnitsSold  
- ActiveTasks  
- CompletedTasks  
- LeadConversionRate  
- UnitSalesRate  

**Activities**
- RecentTasks (Last 5)

---

### AdminProfileViewModel
System-wide dashboard for administrators.

**KPIs**
- TotalUsers  
- TotalProjects  
- TotalUnits  
- TotalLeads  
- AvailableUnits  
- SoldUnits  
- SystemUtilizationRate  
- UnassignedLeads  

**Additional Data**
- Role breakdown (Admin / Marketing / Sales)
- RecentUsers
- RecentAssignments

---

### UserSettingsViewModel
Used in the Settings module.

**Properties**
- UserId  
- FullName  
- EmailUsername  
- Role  
- CreatedAt  
- LastLoginAt  

---

## Services Layer

### ProfileService (`FGI/Services/ProfileService.cs`)
Read-only service responsible for:
- Role-based profile loading
- KPI calculations
- Recent activity retrieval
- Error handling and logging

**Key Methods**
- GetBaseProfileAsync  
- GetMarketingProfileAsync  
- GetSalesProfileAsync  
- GetAdminProfileAsync  

✅ Fixed Admin profile reload issue with safe error handling.

---

### SettingsService (`FGI/Services/SettingsService.cs`)
Handles all user account modifications.

**Features**
- Update full name
- Update email username
- Update password securely

**Security**
- Current password verification
- Email domain enforcement
- Username uniqueness validation

---

## Controllers

### ProfileController
- Displays role-based profiles
- Read-only access
- AJAX profile data loading

### SettingsController
- Centralized account management
- Secure update endpoints
- Authentication required

---

## Views

### Profile Views (`FGI/Views/Profile/`)
- MarketingProfile.cshtml
- SalesProfile.cshtml
- AdminProfile.cshtml
- BaseProfile.cshtml

**Characteristics**
- Dashboard-style UI
- KPI cards
- Recent activity tables
- Settings navigation link

---

### Settings View (`FGI/Views/Settings/Index.cshtml`)
Central location for:
- Full name editing
- Email username editing
- Password updates
- Account info display

Includes client-side and server-side validation with toast notifications.

---
## KPI Calculations

### Marketing
LeadsCreated = COUNT(Leads.CreatedById)
UnitsAdded = COUNT(Units.CreatedById)
ConversionRate = (UnitsAdded / LeadsCreated) * 100


### Sales
AssignedLeads = COUNT(Leads.AssignedToId)
LeadsConverted = COUNT(Leads WITH UnitId)
ConversionRate = (LeadsConverted / AssignedLeads) * 100


### Admin
SystemUtilizationRate = (SoldUnits / TotalUnits) * 100


## Security

- Authentication required for all profile and settings routes
- Users can only access and modify their own accounts
- Email domain fixed to `@role.fgi`
- CSRF protection enabled
- SQL Injection protection via Entity Framework
- XSS protection via Razor encoding

---

## Performance Considerations

- Optimized queries
- Limited recent activity results
- Indexed foreign keys
- On-demand KPI calculation

Caching can be added later (e.g., Redis).

---

## Navigation

- User dropdown:
  - My Profile
  - Settings
- Clear separation:
  - Profile = View only
  - Settings = Edit only

---

## Files Added

- Interfaces/ISettingsService.cs
- Services/SettingsService.cs
- Controllers/SettingsController.cs
- ViewModels/UserSettingsViewModel.cs
- Views/Settings/Index.cshtml

---

## Files Modified

- Services/ProfileService.cs
- Controllers/ProfileController.cs
- Views/Profile/*
- Views/Shared/_Layout.cshtml
- Program.cs

---

## Testing Recommendations

### Functional Testing
- Profile loading for all roles
- Settings updates
- Email validation
- Password change flow

### Security Testing
- Unauthorized access attempts
- CSRF validation
- Email domain enforcement

### UI/UX Testing
- Responsive layouts
- Navigation flow
- Validation feedback

---

## Future Enhancements

- Redis caching
- SignalR real-time KPIs
- Password strength rules
- Two-factor authentication
- Profile picture upload
- Audit trail
- Export to PDF / CSV

---

## Conclusion

The FGI CRM Profile & Settings System delivers:
- Role-based dashboards
- Secure account management
- Clean architecture
- Improved UI/UX
- Production-ready structure

---

**Author:** Belal Fawzy Abdelmaksoud  
**System Version:** 3.0  
**Refactoring Version:** 2.0  
**Status:** Complete ✅  
**Last Updated:** 29/1/2026
