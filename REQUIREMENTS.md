# AtharERP System Requirements

## Project Overview
- Company: Athar (أثر) - Engineering & Construction
- Total Budget: 95,000 DZD
- Timeline: 18 weeks
- Tech Stack: ASP.NET Core MVC, .NET 10, PostgreSQL, EF Core 9.0.3, Npgsql 9.0.3
- Architecture: No ViewModels - pass Models directly to Views. Use [Bind] in Controllers.
- UI: RTL Arabic interface with Athar brand colors (amber/brown theme)

## Authentication & Authorization
- Custom Identity: ApplicationUser extends IdentityUser, ApplicationRole extends IdentityRole
- 3 Template Roles (cannot delete): Admin (مدير النظام), Design Engineer (مهندس تصميم), Quality Engineer (مهندس جودة)
- Dynamic Roles: Admin can create/edit/delete custom roles
- Dynamic Permissions: Every system function = one Permission record. Can enable/disable per role.
- Engineer Rank: Enum (None=0, Junior=1, MidLevel=2, Senior=3, Lead=4, Principal=5)
- GPS Attendance: Each user has ExpectedLocationName, ExpectedLatitude, ExpectedLongitude, AllowedRadiusMeters (default 100m)

## Module 1: Identity & Authentication (Weeks 1-2) - 25%
- Login/Logout with remember me
- Register new users (Admin only)
- User management: list, edit, toggle active/inactive
- Role management: list, create, edit, delete (template roles protected)
- Permission management: 20+ permissions covering all modules
- Dashboard with stats (total users, active users, roles, permissions)

## Module 2: Project Management (Weeks 3-5) - 12.5%
- Client: basic data brought forward from Module 6/7 (Name, CompanyName, Phone, Email, Address,
  TaxNumber, Notes). One Client → many Projects. NO portal, approvals, or comments yet — those
  stay in Module 6/7.
- Projects: hierarchical (parent/child), code format PRJ-YYYY-NNN (auto-generated), linked to a
  Client (nullable), a Project Manager, and assigned engineers.
- Project Stages: ordered, each with a Weight (% of total project), a Cost, an assigned engineer,
  and status (New, In Progress, Client Review, Completed). CompletionPercentage is auto-calculated,
  not manually set.
- Project Steps: belong to a Stage, each with a Weight (% within the stage, fixed after creation —
  cannot be edited once created) and a status. System blocks a stage's step weights from exceeding
  100% in total.
- Project Tasks: belong to a Stage, assigned to an engineer, with Priority (Low/Medium/High/
  Critical), Status (NotStarted/InProgress/Completed/Blocked), DueDate, BonusAmount, PenaltyAmount,
  and an independent CompletionPercentage (not part of the stage weight calculation).
- Task Dependencies: a task cannot be moved to InProgress until all tasks it depends on are
  Completed. Direct circular dependencies (A depends on B, B depends on A) are blocked; longer
  cycles (A→B→C→A) are not yet detected — flagged as a known gap for a future pass.

### Business rules (auto-calculation)
- Stage Completion % = sum of completed steps' weights ÷ sum of all steps' weights in that stage.
- Project Completion % = weighted average of all stages' completion %, weighted by each stage's
  Weight.
- A step's Weight is immutable after creation; only Status and ActualCost can be edited afterward.

### Permission model
- Projects.View / Projects.Create / Projects.Edit / Projects.Delete gate all project CRUD.
- Clients.View / Clients.Create / Clients.Edit / Clients.Delete gate all client CRUD (new
  permissions, added in this module).
- Stage/Step/Task/Dependency management reuses Projects.Edit — no separate permissions for these
  sub-resources at this stage.
- All permission checks are enforced server-side via a reusable `[RequirePermission("X")]` filter,
  not just hidden UI buttons.

### Explicitly postponed to Module 6/7
- Client portal login
- Client stage approval / rejection workflow
- Client revision counter (3 free + paid)
- Client comments on stages
- Meeting logs
- Full CRM workflow

## Module 3: Site Management (Weeks 3-5) - included
- Sites linked to projects
- Site Operations: quality checks, safety checks, contractor visits
- Daily reports with photos
- GPS location for sites

## Module 4: Supply Management (Weeks 6-8) - 12.5%
- Suppliers database
- Supply Requests linked to projects
- Supply Items with photos, dimensions, quantities
- Approval workflow (pending, approved, rejected)
- Delivery tracking

## Module 5: Financial Management (Weeks 6-8) - included
- Financial Records: types (Cost, Revenue, Expense, Bonus, Loan)
- Linked to projects, users, supply requests
- Payment tracking
- Financial reports by project/date range

## Module 6: CRM / Public Relations (Weeks 9-12) - 50%
- Clients database: basic CRUD already delivered in Module 2. This module adds the client-facing
  portal login, stage approval/rejection with revision count limit, client comments on stages, and
  full CRM workflow.
- Stage Approvals: client approval per project stage with revision count limit
- Client feedback and complaints
- Meeting logs

## Module 7: HR Management (Weeks 9-12) - included
- Attendance: check-in/check-out with GPS validation (Haversine formula)
- Attendance history and reports
- Loan Requests: amount, installments, approval status
- Leave requests

## Module 8: Notifications & Reports (Weeks 13-16)
- In-app notifications for all system events
- Email notifications
- Reports: project progress, financial summary, attendance reports, supply status
- Export to PDF/Excel

## Payment Schedule
- Weeks 1-2 (Module 1): 25% = 23,750 DZD
- Weeks 3-5 (Modules 2-3): 12.5% = 11,875 DZD
- Weeks 6-8 (Modules 4-5): 12.5% = 11,875 DZD
- Weeks 9-12 (Modules 6-7): 50% = 47,500 DZD
- Weeks 13-18 (Module 8 + Testing): remaining

## Database Notes
- PostgreSQL schema: identity tables in "identity" schema
- All entities use Arabic [Display] attributes
- SeedData creates default admin: admin@athar.ly / Athar@Admin2026