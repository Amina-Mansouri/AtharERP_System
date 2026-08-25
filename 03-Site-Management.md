---
module: "Site Management"
module_id: "03"
priority: "High"
duration: "3 weeks (Weeks 3-5) - Parallel with Project Management"
budget_pct: 12.5
budget_amount: "Included in 12.5%"
dependencies: ["01-Identity-Authentication", "02-Project-Management"]
rtl: true
theme_colors: ["Brown", "Amber"]
company: "Athar"
---

# Module 03: Site Management (إدارة المواقع والعمليات الميدانية)

## 1. Executive Overview

| Attribute | Value |
|-----------|-------|
| **Priority** | High |
| **Duration** | 3 weeks (Weeks 3-5) — Parallel with Project Management |
| **Budget** | Included in 12.5% |
| **Dependencies** | Module 01 (Identity), Module 02 (Projects) |

## 2. Database Schema (DBML)

### 2.1 Site Entity

```dbml
Table Site {
  id int [pk, increment]
  name varchar(255) [not null, note: 'Site name']
  description text [null]
  project_id int [not null, ref: > Project.id, note: 'Each site linked to ONE project']
  address text [null]
  latitude double [null, note: 'GPS latitude for attendance verification']
  longitude double [null, note: 'GPS longitude for attendance verification']
  allowed_radius_meters int [not null, default: 100, note: 'Allowed radius in meters']
  status varchar(50) [not null, default: 'Active', note: 'Active=1, OnHold=2, Completed=3']
  start_date date [null]
  expected_end_date date [null]
  actual_end_date date [null]
  is_active boolean [not null, default: true]
  created_at datetime [not null, default: `now()`]
}
```

### 2.2 Site Operation (Work Phases)

```dbml
Table SiteOperation {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  name varchar(255) [not null, note: 'Phase name']
  description text [null]
  sequence int [not null, note: 'Display order']
  status varchar(50) [not null, default: 'NotStarted', note: 'NotStarted=1, InProgress=2, Completed=3, Delayed=4']
  planned_start_date date [null]
  planned_end_date date [null]
  actual_start_date date [null]
  actual_end_date date [null]
  completion_percentage decimal(5,2) [not null, default: 0]
  responsible_id string [null, ref: > ApplicationUser.id, note: 'Responsible person']
  notes text [null]
}
```

### 2.3 Site Daily Report

```dbml
Table SiteDailyReport {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  report_date date [not null]
  weather varchar(100) [null]
  workers_count int [not null, default: 0]
  work_completed text [null]
  issues text [null, note: 'Problems/obstacles']
  materials_used text [null]
  equipment_used text [null]
  visits text [null]
  notes text [null]
  created_by_id string [not null, ref: > ApplicationUser.id]
  created_at datetime [not null, default: `now()`]
}
```

### 2.4 Site Daily Report Photo

```dbml
Table SiteDailyReportPhoto {
  id int [pk, increment]
  daily_report_id int [not null, ref: > SiteDailyReport.id]
  file_path varchar(500) [not null]
  description text [null]
  uploaded_at datetime [not null, default: `now()`]
}
```

### 2.5 Site Quality Check

```dbml
Table SiteQualityCheck {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  quality_type varchar(50) [not null, default: 'Site', note: 'Technical=1, Site=2, Financial=3, Administrative=4']
  check_type varchar(255) [not null, note: 'Type of check']
  description text [null]
  result varchar(50) [not null, default: 'Pending', note: 'Pending=1, Pass=2, Fail=3, NeedsReview=4']
  notes text [null]
  check_date datetime [not null, default: `now()`]
  checked_by_id string [not null, ref: > ApplicationUser.id]
  is_approved boolean [not null, default: false]
  approved_at datetime [null]
  approved_by_id string [null, ref: > ApplicationUser.id]
}
```

### 2.6 Site Safety Check

```dbml
Table SiteSafetyCheck {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  check_type varchar(255) [not null]
  description text [null]
  result varchar(50) [not null, default: 'Safe', note: 'Safe=1, Warning=2, Danger=3']
  notes text [null]
  check_date datetime [not null, default: `now()`]
  checked_by_id string [not null, ref: > ApplicationUser.id]
  is_approved boolean [not null, default: false]
}
```

### 2.7 Site Contractor

```dbml
Table SiteContractor {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  name varchar(255) [not null]
  company_name varchar(255) [null]
  phone varchar(50) [null]
  specialty varchar(255) [null]
  start_date date [null]
  end_date date [null]
  status varchar(50) [not null, default: 'Active', note: 'Active=1, Completed=2, Cancelled=3']
  notes text [null]
}
```

### 2.8 Site Maintenance

```dbml
Table SiteMaintenance {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  maintenance_type varchar(255) [not null]
  description text [null]
  status varchar(50) [not null, default: 'Pending', note: 'Pending=1, InProgress=2, Completed=3']
  request_date datetime [not null, default: `now()`]
  completion_date datetime [null]
  cost decimal(18,2) [null]
  responsible_id string [null, ref: > ApplicationUser.id]
  notes text [null]
}
```

### 2.9 Site Document

```dbml
Table SiteDocument {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  document_type varchar(50) [not null, note: 'ApprovedMap=1, ContractorContract=2, QualityReport=3, SafetyReport=4, DailyReport=5, Other=6']
  file_name varchar(255) [not null]
  file_path varchar(500) [not null]
  description text [null]
  uploaded_at datetime [not null, default: `now()`]
}
```

### 2.10 Site Supply Request

```dbml
Table SiteSupplyRequest {
  id int [pk, increment]
  site_id int [not null, ref: > Site.id]
  project_id int [not null, ref: > Project.id, note: 'Linked to site project']
  material_name varchar(255) [not null]
  dimensions varchar(255) [null]
  quantity decimal(18,2) [not null]
  unit varchar(50) [not null]
  notes text [null]
  status varchar(50) [not null, default: 'Pending', note: 'Pending=1, Approved=2, Delivered=3, Rejected=4']
  request_date datetime [not null, default: `now()`]
  requested_by_id string [not null, ref: > ApplicationUser.id]
}
```

## 3. Enums (C# Style)

### 3.1 SiteStatus
```csharp
public enum SiteStatus
{
    [Display(Name = "نشط")] Active = 1,
    [Display(Name = "متوقف")] OnHold = 2,
    [Display(Name = "مكتمل")] Completed = 3
}
```

### 3.2 OperationStatus
```csharp
public enum OperationStatus
{
    [Display(Name = "لم تبدأ")] NotStarted = 1,
    [Display(Name = "قيد التنفيذ")] InProgress = 2,
    [Display(Name = "مكتملة")] Completed = 3,
    [Display(Name = "متأخرة")] Delayed = 4
}
```

### 3.3 QualityType
```csharp
public enum QualityType
{
    [Display(Name = "جودة فنية")] Technical = 1,
    [Display(Name = "جودة موقع")] Site = 2,
    [Display(Name = "جودة مالية")] Financial = 3,
    [Display(Name = "جودة إدارية")] Administrative = 4
}
```

### 3.4 QualityResult
```csharp
public enum QualityResult
{
    [Display(Name = "معلق")] Pending = 1,
    [Display(Name = "مطابق")] Pass = 2,
    [Display(Name = "غير مطابق")] Fail = 3,
    [Display(Name = "يحتاج مراجعة")] NeedsReview = 4
}
```

### 3.5 SafetyResult
```csharp
public enum SafetyResult
{
    [Display(Name = "آمن")] Safe = 1,
    [Display(Name = "تحذير")] Warning = 2,
    [Display(Name = "خطير")] Danger = 3
}
```

### 3.6 ContractorStatus
```csharp
public enum ContractorStatus
{
    [Display(Name = "نشط")] Active = 1,
    [Display(Name = "منتهي")] Completed = 2,
    [Display(Name = "ملغى")] Cancelled = 3
}
```

### 3.7 MaintenanceStatus
```csharp
public enum MaintenanceStatus
{
    [Display(Name = "معلق")] Pending = 1,
    [Display(Name = "قيد التنفيذ")] InProgress = 2,
    [Display(Name = "مكتمل")] Completed = 3
}
```

### 3.8 SiteDocumentType
```csharp
public enum SiteDocumentType
{
    [Display(Name = "خريطة معتمدة")] ApprovedMap = 1,
    [Display(Name = "عقد مقاول")] ContractorContract = 2,
    [Display(Name = "تقرير جودة")] QualityReport = 3,
    [Display(Name = "تقرير سلامة")] SafetyReport = 4,
    [Display(Name = "تقرير يومي")] DailyReport = 5,
    [Display(Name = "أخرى")] Other = 6
}
```

### 3.9 SiteSupplyStatus
```csharp
public enum SiteSupplyStatus
{
    [Display(Name = "معلق")] Pending = 1,
    [Display(Name = "تمت الموافقة")] Approved = 2,
    [Display(Name = "تم التسليم")] Delivered = 3,
    [Display(Name = "مرفوض")] Rejected = 4
}
```

## 4. Quality Check Types (4 Types per Org Structure)

| # | Type | Arabic Name | Description |
|---|------|-------------|-------------|
| 1 | Technical | جودة فنية | Design and engineering plan inspections |
| 2 | Site | جودة موقع | Field work and execution inspections |
| 3 | Financial | جودة مالية | Cost and budget reviews |
| 4 | Administrative | جودة إدارية | Procedure and document reviews |

## 5. Business Rules (قواعد العمل)

1. **One Project per Site**: Every site is linked to exactly ONE project.
2. **GPS Coordinates**: Site has GPS coordinates for attendance verification.
3. **Daily Reports Mandatory**: Daily reports are REQUIRED for active sites.
4. **Periodic Quality/Safety**: Quality and safety checks recorded periodically.
5. **Supply Requests Linked**: Site supply requests linked to site's project.
6. **Document Organization**: Site documents organized by type (maps, contracts, reports...).
7. **Auto-Copy to Main**: Site operations data rolls up to project level.

## 6. Features Checklist

### 6.1 Site Management
- [x] Add/edit/delete sites
- [x] Link site to project
- [x] Set GPS coordinates and allowed radius
- [x] Track status: Active / OnHold / Completed
- [x] Set start and end dates

### 6.2 Site Operations (Work Phases)
- [x] Add phases to each site
- [x] Set sequence and dates
- [x] Track completion percentage
- [x] Assign responsible person
- [x] Status: NotStarted → InProgress → Completed / Delayed

### 6.3 Daily Reports
- [x] Add daily report per site
- [x] Upload multiple photos per report
- [x] Record worker count, work completed, issues
- [x] Record materials and equipment used
- [x] Record visits
- [x] Archive daily reports
- [x] Search and filter by date and site

### 6.4 Quality Checks
- [x] 4 quality types supported (Technical, Site, Financial, Administrative)
- [x] Record check type, description, result
- [x] Approval workflow (checked_by → approved_by)
- [x] Track check date and notes

### 6.5 Safety Checks
- [x] Record check type and description
- [x] Result: Safe / Warning / Danger
- [x] Approval workflow
- [x] Track check date

### 6.6 Contractor Management
- [x] Add/edit/delete contractors per site
- [x] Track contractor status
- [x] Link contractor to site and project
- [x] Contractor visit reports
- [x] Fields: Name, Company, Phone, Specialty, Dates, Status

### 6.7 Maintenance & Pledges
- [x] Add maintenance requests
- [x] Track status: Pending → InProgress → Completed
- [x] Record cost and completion date
- [x] Assign responsible person

### 6.8 Site Documents
- [x] Upload documents by type
- [x] Types: ApprovedMap, ContractorContract, QualityReport, SafetyReport, DailyReport, Other
- [x] Track upload date

### 6.9 Site Supply Requests
- [x] Create supply requests from site
- [x] Link to project
- [x] Fields: MaterialName, Dimensions, Quantity, Unit
- [x] Status: Pending → Approved → Delivered / Rejected

## 7. Reports

### 7.1 Site Reports

| # | Report Name | Arabic Name | Permission |
|---|-------------|-------------|------------|
| 1 | Site Status Report | تقرير حالة المواقع | Sites.View |
| 2 | Daily Work Completed | تقرير الأعمال المنجزة يومياً | Sites.Reports |
| 3 | Contractor Visits | تقرير زيارات المقاولين | Sites.View |
| 4 | Quality Checks | تقرير فحوصات الجودة | Quality.View |
| 5 | Safety Checks | تقرير فحوصات السلامة | Quality.View |
| 6 | Supply Requests | تقرير طلبات التوريد | Supply.View |
| 7 | Maintenance | تقرير الصيانة | Sites.View |

## 8. Notifications

| Event | Recipients | Module Source |
|-------|------------|---------------|
| Daily report added | Project manager + Admin | Module 03 |
| Quality check failed | Site manager + Quality engineer | Module 03 |
| Safety check danger | Site manager + Admin | Module 03 |
| Site supply request | Supply department | Module 03 |
| Delayed site phase | Project manager | Module 03 |

## 9. Implementation Notes

- **No ViewModels**: Pass Models directly to Views.
- **Use `[Bind]` in Controllers**.
- **RTL**: All pages Arabic right-to-left.
- **Colors**: Brown/Amber matching Athar company identity.
- **GPS**: Use Haversine formula for distance calculation (same as HR module).
- **Reports**: Export PDF/Excel.
