# Role-based User Profile System Documentation

## Overview
This document describes the implementation of a comprehensive Role-based User Profile system for the FGI CRM application. The system provides role-specific KPIs, activities, and profile management for Marketing, Sales, and Admin users.

## Architecture

### 1. View Models (`FGI/ViewModels/`)

#### BaseProfileViewModel
- **Purpose**: Common profile information for all user roles
- **Properties**:
  - `UserId`: Unique identifier
  - `FullName`: User's full name
  - `Email`: User's email address
  - `Role`: User's role (Admin, Marketing, Sales)
  - `CreatedAt`: Account creation date
  - `LastLoginAt`: Last login timestamp
  - `TotalDaysInSystem`: Days since account creation
  - `AccountStatus`: Current account status

#### MarketingProfileViewModel
- **Purpose**: Marketing-specific profile with KPIs and activities
- **KPIs**:
  - `LeadsCreated`: Count of leads created by this user
  - `UnitsAdded`: Count of units added by this user
  - `FeedbacksGiven`: Count of feedbacks provided
  - `LeadToUnitConversionRate`: Percentage of leads converted to units
  - `ActiveLeads`: Currently active leads
  - `CompletedLeads`: Successfully completed leads
- **Activities**: `RecentLeads` - Last 5 leads created

#### SalesProfileViewModel
- **Purpose**: Sales-specific profile with KPIs and activities
- **KPIs**:
  - `AssignedLeads`: Count of leads assigned to this user
  - `LeadsConvertedToUnits`: Count of leads converted to units
  - `UnitsSold`: Count of units sold
  - `ActiveTasks`: Currently active tasks
  - `LeadConversionRate`: Percentage of leads converted
  - `UnitSalesRate`: Percentage of units sold
  - `CompletedTasks`: Successfully completed tasks
- **Activities**: `RecentTasks` - Last 5 assigned tasks

#### AdminProfileViewModel
- **Purpose**: Admin profile with system-wide KPIs and activities
- **KPIs**:
  - `TotalUsers`: Total users in system
  - `TotalProjects`: Total projects
  - `TotalUnits`: Total units
  - `TotalLeads`: Total leads
  - `AvailableUnits`: Available units
  - `SoldUnits`: Sold units
  - `SystemUtilizationRate`: System utilization percentage
  - `UnassignedLeads`: Leads without assignment
- **Role Breakdown**: Marketing, Sales, Admin user counts
- **Activities**: `RecentUsers` and `RecentAssignments`

### 2. Services (`FGI/Services/ProfileService.cs`)

#### IProfileService Interface
- `GetMarketingProfileAsync(int userId)`: Get marketing profile with KPIs
- `GetSalesProfileAsync(int userId)`: Get sales profile with KPIs
- `GetAdminProfileAsync(int userId)`: Get admin profile with system KPIs
- `GetBaseProfileAsync(int userId)`: Get basic profile information
- `UpdateProfileAsync(int userId, string fullName, string email)`: Update profile

#### ProfileService Implementation
- **Dependencies**: `AppDbContext`, `ILogger<ProfileService>`
- **Key Methods**:
  - Calculates role-specific KPIs from database
  - Retrieves recent activities with time calculations
  - Handles profile updates with validation
  - Provides error handling and logging

### 3. Controllers (`FGI/Controllers/ProfileController.cs`)

#### ProfileController
- **Authorization**: Requires authentication
- **Actions**:
  - `Index()`: Displays role-specific profile view
  - `Update(string fullName, string email)`: Updates profile information
  - `GetProfileData()`: Returns profile data as JSON for AJAX

#### Route Mapping
- `/Profile` - Main profile page
- `/Profile/Update` - Profile update endpoint
- `/Profile/GetProfileData` - AJAX data endpoint

### 4. Views (`FGI/Views/Profile/`)

#### MarketingProfile.cshtml
- **Features**:
  - Profile information card with avatar
  - KPI cards with visual indicators
  - Performance metrics display
  - Recent leads activity table
  - Edit profile modal

#### SalesProfile.cshtml
- **Features**:
  - Profile information card with avatar
  - Sales-specific KPI cards
  - Task performance metrics
  - Recent tasks activity table with priority indicators
  - Edit profile modal

#### AdminProfile.cshtml
- **Features**:
  - Profile information card with avatar
  - System-wide KPI cards
  - User role breakdown
  - Tabbed recent activity (Users & Assignments)
  - Edit profile modal

#### BaseProfile.cshtml
- **Features**:
  - Basic profile information
  - Account details
  - Fallback for unknown roles

## Usage

### Accessing Profiles
1. **Navigation**: Click on user dropdown → "My Profile"
2. **Direct URL**: `/Profile`
3. **Role Detection**: Automatically displays appropriate view based on user role

### Profile Features
1. **View KPIs**: Role-specific performance metrics
2. **Recent Activity**: Last 5 relevant activities
3. **Edit Profile**: Update name and email
4. **Real-time Updates**: AJAX-powered profile updates

### KPI Calculations

#### Marketing KPIs
- **LeadsCreated**: `COUNT(Leads WHERE CreatedById = userId)`
- **UnitsAdded**: `COUNT(Units WHERE CreatedById = userId)`
- **FeedbacksGiven**: `COUNT(LeadFeedbacks WHERE SalesId = userId)`
- **ConversionRate**: `(UnitsAdded / LeadsCreated) * 100`

#### Sales KPIs
- **AssignedLeads**: `COUNT(Leads WHERE AssignedToId = userId)`
- **LeadsConvertedToUnits**: `COUNT(Leads WHERE AssignedToId = userId AND UnitId IS NOT NULL)`
- **UnitsSold**: `COUNT(Units WHERE CreatedById = userId AND IsAvailable = false)`
- **ConversionRate**: `(LeadsConvertedToUnits / AssignedLeads) * 100`

#### Admin KPIs
- **TotalUsers**: `COUNT(Users)`
- **TotalProjects**: `COUNT(Projects)`
- **TotalUnits**: `COUNT(Units)`
- **TotalLeads**: `COUNT(Leads)`
- **SystemUtilizationRate**: `(SoldUnits / TotalUnits) * 100`

## Integration Points

### Database Relationships
- **Users** → **Leads** (CreatedBy, AssignedTo)
- **Users** → **Units** (CreatedBy)
- **Users** → **LeadFeedbacks** (SalesId)
- **Leads** → **LeadAssignmentHistories** (Assignment tracking)

### Service Dependencies
- **ProfileService** depends on:
  - `AppDbContext` for data access
  - `ILogger<ProfileService>` for logging
- **ProfileController** depends on:
  - `IProfileService` for business logic
  - `ILogger<ProfileController>` for logging

### Navigation Integration
- **Layout**: Added "My Profile" link to user dropdown
- **Routing**: Profile controller registered in Program.cs
- **Authorization**: Requires authentication for all profile actions

## Error Handling

### Service Level
- Try-catch blocks around database operations
- Detailed error logging with user context
- Graceful fallbacks for missing data

### Controller Level
- User authentication validation
- Input validation for profile updates
- JSON error responses for AJAX calls

### View Level
- User-friendly error messages
- Loading states for AJAX operations
- Form validation feedback

## Security Considerations

### Authentication
- All profile actions require authentication
- User can only access their own profile
- Role-based view rendering

### Data Validation
- Input sanitization for profile updates
- SQL injection prevention through Entity Framework
- XSS prevention through Razor encoding

### Authorization
- Role-based profile views
- Secure profile update endpoints
- CSRF protection for form submissions

## Performance Considerations

### Database Optimization
- Efficient queries with proper includes
- Limited result sets for recent activities
- Indexed foreign key relationships

### Caching Strategy
- Profile data calculated on-demand
- No caching implemented (can be added if needed)
- Efficient data loading patterns

## Future Enhancements

### Potential Improvements
1. **Caching**: Implement profile data caching
2. **Real-time Updates**: SignalR for live KPI updates
3. **Export Features**: Export profile data to PDF/CSV
4. **Advanced Analytics**: Trend analysis and charts
5. **Notifications**: Profile-related notifications
6. **Audit Trail**: Track profile changes

### Scalability Considerations
1. **Database Indexing**: Add indexes for frequently queried fields
2. **Query Optimization**: Optimize complex KPI calculations
3. **Caching Layer**: Implement Redis for profile data
4. **Background Jobs**: Move heavy calculations to background jobs

## Testing Recommendations

### Unit Tests
- ProfileService method testing
- KPI calculation accuracy
- Error handling scenarios

### Integration Tests
- Controller action testing
- Database integration testing
- Role-based view rendering

### Performance Tests
- Large dataset KPI calculations
- Concurrent profile access
- Database query performance

## Maintenance

### Regular Tasks
1. **Monitor Performance**: Track KPI calculation times
2. **Update KPIs**: Add new metrics as business needs change
3. **Database Maintenance**: Ensure proper indexing
4. **Log Analysis**: Review error logs for issues

### Code Maintenance
1. **Service Updates**: Keep ProfileService in sync with data model changes
2. **View Updates**: Maintain responsive design across devices
3. **Security Updates**: Regular security review of profile endpoints
4. **Documentation**: Keep this documentation updated with changes

---

**Created**: 8/25
**Version**: 3.0
**Author**: Belal Fawzy Abdelmaksoud
**Last Updated**: [Current Date]





