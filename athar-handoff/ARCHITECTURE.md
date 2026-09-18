# البنية الفعلية — الكيانات، الصلاحيات، التنقّل، الهوية البصرية

مبني من مراجعة كاملة للكود الفعلي على `origin/main`، وليس من ذاكرة أو خطة قديمة.

---

## ١) الكيانات (`Models/Entities/`, ٧٦ ملفًا)

### الهوية والموظفون
`ApplicationUser` · `ApplicationRole` · `Permission` · `RolePermission` · `Department` · `CareerTrack` · `JobRank` · `ContractHistory` · `EmployeeDocument` · `AuditLog`

### العملاء والمقاولون
`Client` · `Contractor`

### المشاريع
`Project` · `ProjectTeamMember` · `ProjectStage` · `ProjectTask` · `TaskAssignee` · `TaskTodo` · `TaskDependency` · `ProjectAssignment` · `AssignmentEngineer` · `ProjectAssignmentSubtask` · `ProjectDocument` · `ProjectTimeline` · `DesignProposal` · `StageTemplate` · `StageTemplateTask`
⚠️ `StageTaskTemplate` — كيان يبدو مكررًا/سقّالة قديمة بلا أي استخدام (راجع القسم ٥).
(`ProjectStep` وenum `StepStatus` — كانا موجودين بلا أي واجهة إطلاقًا، **حُذفا بالكامل من الكود**.)

### المواقع
`Site` · `SiteOperation` · `SiteContractor` · `SiteDailyReport` · `SiteDailyReportPhoto` · `SiteDocument` · `SiteMaintenance` · `SiteQualityCheck` · `SiteSafetyCheck` · `SiteSupplyRequest`
⚠️ `TechnicalRequest` — كيان بلا أي استخدام حاليًا (سقّالة لميزة "الطلبات الفنية" غير المبنية).

### المالية والموارد البشرية (سقّالة غير مستخدمة بالكامل تقريبًا)
`FinancialRecord` (مُشار إليه فقط عند حذف مشروع بالتتابع) · `FinancialClaim` · `Custody` · `DocumentRevision` — **لا Controller ولا View لأي منها**.

### الإشعارات
`Notification` · `NotificationSetting`

### الـEnums (حسب المجال)
- **المشاريع**: `ProjectStatus`, `ProjectScope`, `ProjectType`, `Priority`, `StageStatus`, `ProjectTaskStatus`, `TaskPriority`, `TaskOutputType`, `DependencyType`, `AssignmentStatus`, `ProposalStatus`, `TeamRole`, `TimelineType`
- **المواقع**: `SiteStatus`, `OperationStatus`, `ContractorStatus`, `SiteDocumentType`, `SiteQualityType`, `QualityCheckResult`, `SafetyResult`, `MaintenanceStatus`, `SiteSupplyStatus`, `TechnicalRequestStatus` (غير مستخدَم)
- **الموظفون**: `EngineerRank` (غير مستخدَم) · `MeasurementUnit` · `SupplierSpecialty` (غير مستخدَم) · `CustodyStatus` (غير مستخدَم)
- **المالية**: `ClaimStatus` (غير مستخدَم)
- **العملاء**: `ClientType`
- **الإشعارات**: `NotificationEventType`
- **أخرى**: `RevisionStatus` (مرتبط بـ`DocumentRevision` غير المستخدَم)

---

## ٢) الكونترولرز (٢٢ كونترولر)

| Controller | الغرض |
|---|---|
| `AccountController` | تسجيل الدخول/الخروج، نسيت/إعادة تعيين كلمة المرور — مصادقة الموظفين الداخليين |
| `AdminController` | كونسول مدير النظام حصرًا: الموظفون، الأدوار والصلاحيات، الإدارات والأقسام، الرتب والمسارات |
| `ClientsController` | العملاء — عرض/إضافة/تعديل/حذف |
| `ContractorPortalController` | بوابة دخول منفصلة للمقاولين الخارجيين (مصادقة ولوحة تحكم خاصة بهم) |
| `ContractorsController` | إدارة حسابات دخول المقاولين من طرف الموظفين الداخليين |
| `DesignProposalsController` | رفع/اعتماد/رفض مقترحات التصميم المرفقة بالمهام — بلا Views خاصة، فقط تصرفات |
| `EmployeeDocumentsController` | رفع/عرض/حذف مستندات موظف |
| `HomeController` | الصفحة الرئيسية + توجيه للوحة حسب الدور (مدير/مهندس) |
| `NotificationsController` | صندوق الإشعارات الشخصي، الفلاتر، تعليم كمقروء، تفضيلات كل نوع حدث |
| `ProfileController` | صفحة الملف الشخصي، تغيير الصورة وكلمة المرور |
| `ProjectAssignmentsController` | تتبّع التكليفات عبر المشاريع — نظرة عامة، تكليفاتي، إدارة المهندسين والبنود الفرعية |
| `ProjectDocumentsController` | رفع/حذف مستندات مشروع — تصرفات فقط، جزء من صفحة تفاصيل المشروع |
| `ProjectStagesController` | إدارة مراحل المشروع، تفعيل قوالب المراحل، خطوات المرحلة |
| `ProjectTasksController` | إدارة المهام، مهامي، المكلَّفون، البنود، الاعتماديات، اعتماد/رفض المهمة |
| `ProjectsController` | إدارة المشاريع الأساسية، القائمة/التفاصيل/الجدول الزمني، إيقاف/إلغاء/إعادة تفعيل/حذف |
| `SiteChecksController` | فحوصات الجودة والسلامة لموقع + اعتمادها |
| `SiteContractorsController` | ربط/فك ربط مقاولين بموقع معيّن |
| `SiteDailyReportsController` | التقارير اليومية للموقع — عرض/تفاصيل/حذف صور |
| `SiteDocumentsController` | مستندات الموقع — رفع/عرض/حذف |
| `SiteMaintenanceController` | طلبات صيانة الموقع |
| `SiteSupplyRequestsController` | طلبات توريد الموقع — عرض وتحديث حالة |
| `SitesController` | إدارة المواقع الأساسية، التفاصيل، مراحل العمل |
| `StageTemplatesController` | قوالب المراحل القابلة لإعادة الاستخدام (اسم + قائمة مهام افتراضية) |

---

## ٣) نموذج الصلاحيات (`Data/SeedData.cs`)

### كل الصلاحيات المعرَّفة (بالوحدة)
- **الموظفون**: `Users.View/Create/Edit/Delete/ToggleActive`, `Employees.Manage`
- **الأدوار**: `Roles.View/Create/Edit/Delete/ManagePermissions`
- **المشاريع**: `Projects.ViewAll/ViewOwn/Create/Edit/Delete`, `Projects.Stages.Manage`, `Projects.Tasks.Manage`, `Projects.Assignments.View/Edit`, `Projects.Proposals.View/Edit`, `Projects.DocControl.Manage`
- **المالية**: `Finance.View`, `Finance.Costs.View/Edit`, `Finance.Sales.View/Edit`, `Finance.Reports`, `Finance.Print`, `Finance.Claims.View/Manage`
- **الموارد البشرية**: `HR.View`, `HR.Attendance.View/Edit`, `HR.Salaries.View/Edit`, `HR.Leaves.Manage`, `HR.Evaluation`, `HR.Custody.Manage`
- **التوريدات**: `Supply.View/Create/Approve`
- **المواقع**: `Sites.View/Reports/Manage`, `Sites.TechnicalRequests.Manage`, `Sites.Maintenance.Manage`
- **العلاقات العامة**: `PR.View`, `PR.Contracts`, `PR.Clients`
- **الجودة**: `Quality.View/Reports/Approve`
- **التقارير والإشعارات**: `Reports.View/Export`, `Notifications.Manage`

⚠️ صلاحيات كثيرة (كل `Finance.*`، `HR.*` عدا `HR.Attendance.View`/`HR.Salaries.View`، `PR.Contracts`، `Sites.TechnicalRequests.Manage`، `Reports.*`) **مزروعة بلا أي Controller/View يستخدمها بعد** — سابقة لوحداتها.

### الأدوار المزروعة
- **مدير النظام**: دور أساسي محمي من الحذف، يملك **كل** الصلاحيات، ومحمي إضافيًا بفحص دور مباشر `[Authorize(Roles = "مدير النظام")]` على `AdminController` (لا يعتمد على جدول الصلاحيات).
- **مهندس تصميم**: `Projects.ViewOwn`, `Projects.Proposals.View/Edit`, `HR.Attendance.View`, `HR.Salaries.View`, `Sites.View`, `Supply.View/Create` — ممنوع صراحة (حسب تعليق الكود) من الميزانيات وبيانات المالك والمالية العامة.
- **مهندس جودة**: `Projects.ViewAll`, `Projects.Create`, `Projects.Stages.Manage`, `Projects.Tasks.Manage`, `Projects.Assignments.View`, `HR.Attendance.View`, `Sites.View`, `Quality.View/Approve/Reports`, `Reports.View`.
- أدوار مخصَّصة قابلة للإنشاء من شاشة "الأدوار" بأي مجموعة صلاحيات. **لا منح صلاحيات فردية لموظف بعينه** — الصلاحية الفعلية = صلاحيات دوره فقط (`PermissionService.HasPermissionAsync`).

### التحقق على مستوى الكونترولر (`[RequirePermission(...)]`)
`ClientsController`→`PR.Clients` · مجموعة مواقع الإدارة (`SitesController` إجراءات الإدارة، `SiteContractorsController`, `SiteMaintenanceController`, إجراءات إدارة `SiteDocumentsController`)→`Sites.Manage`، والعرض فقط→`Sites.View` · `SiteChecksController`→`Quality.View`(عرض)/`Quality.Approve`(اعتماد) · `SiteSupplyRequestsController`→`Supply.View`(عرض)/`Supply.Approve`(تحديث حالة) · `StageTemplatesController`→`Projects.Stages.Manage` · مجموعة المشاريع→مزيج من `Projects.Create/Edit/Delete`, `Projects.Stages.Manage`, `Projects.Tasks.Manage`, `Projects.Assignments.View/Edit` · `AdminController`→فحص دور مباشر لا صلاحية.

---

## ٤) القائمة الجانبية والتنقّل (`Views/Shared/_Layout.cshtml`)

**ثلاثة أنماط عرض**: `HideNav` (صفحات المصادقة، بطاقة موسّطة) · `PlainPage` (بوابة المقاولين، بلا قائمة جانبية) · النمط الكامل الافتراضي (شريط علوي + قائمة جانبية + محتوى).

**أعلام التحكم بالظهور**: `canUsers`/`canRoles`/`canDepartments` = دور "مدير النظام" · `canProjects` = `Projects.ViewOwn` أو `Projects.ViewAll` · `canClients` = `PR.Clients` · `canSites` = `Sites.View` · `canViewAssignments` = `Projects.Assignments.View`.

**تعبير `activeSystem`** (يحدد أي قسم يبقى مفتوحًا حسب الكونترولر الحالي):
- `sys01` ← `Admin`
- `sys02` ← `Projects`, `ProjectTasks`, `ProjectStages`, `ProjectAssignments`, `ProjectDocuments`, `DesignProposals`
- `sys03` ← `Sites`, `SiteContractors`, `Contractors`, `SiteChecks`, `SiteDailyReports`, `SiteDocuments`, `SiteMaintenance`, `SiteSupplyRequests`
- `sys07` ← `Clients`
- لا يوجد أي كونترولر مربوط بـ`sys04`/`sys05`/`sys06`/`sys08` بعد — أقسامها بالكامل بنود معطَّلة.

**"وصول سريع"**: الرئيسية · المشاريع (إن `canProjects`) · المواقع (إن `canSites`) · الموظفون → `Admin/Users` (إن `canUsers`).

**"الإدارات" (بالترتيب)**:
1. **٠١ الصلاحيات والمصادقة** [كاملة] — إدارة الموظفين، إدارة الأدوار والصلاحيات، الإدارات والأقسام، الرتب والمسارات.
2. **٠٢ المشاريع** [كاملة] — المشاريع، مراحل المشروع، تكليفات المشروع (إن `canViewAssignments`).
3. **٠٣ المواقع** [كاملة] — إدارة المواقع، المقاولون، حسابات دخول المقاولين.
4. **٠٤ المالية** [جزئية] — **كل بنودها معطَّلة "قادمة"**: المطالبات المالية، جدول التكاليف العام، جدول البيع النهائي، الرواتب والمصاريف، الأرباح والخسائر.
5. **٠٥ التوريدات** [جزئية] — **كل بنودها معطَّلة**: الموردون، المخازن والأصناف، أوامر الشراء والاستلام.
6. **٠٦ الموارد البشرية** [جزئية] — **كل بنودها معطَّلة**: لوحة الموارد البشرية، الشؤون الإدارية، العهد، الحضور والانصراف، الرواتب والخصومات، الإجازات، التقييم الوظيفي.
7. **٠٧ العلاقات العامة** [جزئية] — العملاء (فعّال، إن `canClients`) + بنود معطَّلة: واجهة الزبون، المقايسات، العقود القانونية، الإعلانات.
8. **٠٨ التقارير** [جزئية] — **كل بنودها معطَّلة**: تقارير المشاريع، الفواتير وسندات القبض، التقارير المالية، أداء الموظفين.

**بلا رابط جانبي مباشر** (تُفتَح فقط من داخل صفحة أخرى): `SiteSupplyRequests`, `SiteChecks`, `SiteDailyReports`, `SiteMaintenance`, `SiteDocuments` (من "روابط سريعة" في تفاصيل الموقع)، `StageTemplates` (من "+ إدارة القوالب" في تفاصيل المشروع).

**الشريط العلوي**: الشعار + اسم النظام · حقل بحث شامل (بلا ربط خلفي فعلي — واجهة فقط) · أيقونة "تكليفاتي" (إن `canProjects`) · جرس الإشعارات بعدّادات حيّة (`NavCounters`: إشعارات غير مقروءة، فحوصات جودة/سلامة معلّقة، طلبات توريد معلّقة — ⚠️ عدّاد "تقارير اليوم الناقصة" ثابت على صفر دائمًا، غير محسوب فعليًا) · شريحة المستخدم → `Profile/Index` · زر الخروج.

---

## ٥) الهوية البصرية (`wwwroot/css/site.css`)

**متغيرات قديمة (لا تزال موجودة، أقل استخدامًا)**:
```
--athar-primary:#7b6bd6  --athar-primary-dark:#6455c4  --athar-primary-light:#9d8df0
--athar-accent:#7b6bd6   --athar-bg:#f8f7fb             --athar-text:#241f33
--athar-border:#ecebf2   --athar-danger:#e35d54          --athar-success:#3fae6f
```

**متغيرات نظام التصميم الحالي (المستخدم فعليًا في كل الشاشات الحديثة)**:
```
--sys-nav-bg:#221837            --sys-nav-active-bg:#342a4c        --sys-nav-active-bar:#8c7ce6
--sys-primary:#7b6bd6           --sys-primary-hover:#6455c4        --sys-primary-text:#6455c4
--sys-success:#3fae6f           --sys-success-text:#2f8a57
--sys-danger:#e35d54            --sys-danger-text:#c94a42
--sys-warning:#ff9130           --sys-warning-text:#c9700f
--sys-info:#5090b0              --sys-info-text:#3f7590
--sys-bg:#f8f7fb                --sys-surface:#ffffff
--sys-text:#241f33              --sys-text-secondary:#5a5370   --sys-text-light:#8a83a0   --sys-text-faint:#c6c1d6
--sys-border:#ecebf2            --sys-row-line:#f1f0f6          --sys-row-line-light:#f6f5fa
--sys-th-bg:#faf9fd             --sys-row-selected:rgba(123,107,214,.05)
--sys-radius-card:11px          --sys-radius-btn:8px
--sys-shadow:0 1px 2px rgba(36,31,51,.04)
--sys-font:'Tajawal','Segoe UI',Tahoma,sans-serif
```

**الوسوم (`.tag-*`)**: `tag-accent` (بنفسجي) · `tag-success` (أخضر) · `tag-danger` (أحمر) · `tag-warning` (برتقالي) · `tag-info` (أزرق) · `tag-muted` (رمادي باهت).

**الأزرار**: `.btn-line` (الشكل القياسي: حدود + خلفية بيضاء) · `.btn-line-accent` (نسخة مصمتة بنفسجية للإجراء الرئيسي). لا توجد `.btn-line-danger`/`.btn-line-warning` كأصناف جاهزة — يُخصَّص اللون عبر `style="color:..."` مباشرة (راجع `CONVENTIONS.md §6`). يوجد أيضًا `.btn-primary` كصنف مصمت منفصل غير مرتبط بعائلة `.btn-line`.

**التقنية**: ASP.NET Core MVC + EF Core + PostgreSQL (`UseNpgsql`). خدمات مسجَّلة: `PermissionService`, `ProjectCalculationService`, `SiteCalculationService`, `AuditService`, `NotificationService`, `FileUploadService`, `IEmailSender→SmtpEmailSender`. خدمتا Hosted Service خلفيتان: `ContractLifecycleHostedService` (إيقاف تلقائي عند انتهاء العقد) و`ProjectDelayMonitorHostedService`. ٨٥ ملف migration (~٤٢ ترحيلة فعلية). واصف أخطاء Identity مخصَّص `AtharIdentityErrorDescriber`. فلتر تفويض مخصَّص `[RequirePermission("code1","code2",...)]` (مطابقة OR لأي كود مذكور، يوجّه لـ`Account/Login` إن لم يسجَّل الدخول أو `Account/AccessDenied` إن رُفض).
