# تعديلات المنطق والبيانات المطلوبة

مبدأ عام: **الكيانات الـ٥٧ موجودة والـ Services موجودة** — لا تُنشئ كياناً جديداً
إلا البندين ٣ و١٠-ب أدناه. أغلب البنود استعلامات عرض (read models) وربط بالـ Views.
نفّذ كل بند عند الشاشة التي تحتاجه، لا كلها مقدماً.

---

## ١. طبقة العرض غير موجودة أصلاً — أولوية قصوى
`Views/**/*.cshtml` غير متتبَّعة في المستودع (الموجود فقط `Views/Shared/_Layout.cshtml.css`).
أنشئ: `_Layout.cshtml`، `_ViewStart.cshtml`، `_ViewImports.cshtml`،
والـ Partials المشتركة (§0 في `SCREENS.md`)، ثم View لكل action في الشاشات الـ٢١.
احترم سياسة المستودع: **بلا ViewModels** — مرّر الـ Entities مباشرة، واستعمل
`ViewData`/`ViewBag` للعدّادات والقوائم المنسدلة، و`[Bind]` في الـ Controllers.

## ٢. كيان `Notification` — حقول ناقصة للشاشتين 1t و1u
الحالي: `Id, UserId, Message, Link, IsRead, CreatedAt` فقط. الشاشتان تحتاجان:
```csharp
public NotificationEventType EventType { get; set; }   // enum جديد، ١١ قيمة
public string SourceModule { get; set; }               // "01" | "02" | "03"
public bool RequiresAction { get; set; }               // لتبويب «تحتاج إجراءً»
public string? EntityType { get; set; }                // Project|Task|Site|Cost|Check|Supply
public int? EntityId { get; set; }                     // لفتح السجل مباشرة
```
`NotificationEventType` (النصوص العربية للعرض في عمود «الحدث»):
`TaskAssigned` مهمة مُكلَّفة · `TaskStatusChanged` تغيّر حالة مهمة ·
`StageCompleted` مرحلة مكتملة · `TaskDelayed` مهمة متأخرة ·
`DeliveryApproaching` تسليم يقترب · `CostCompleted` تكلفة مكتملة ·
`SupplyRequestCreated` طلب توريد جديد · `DailyReportAdded` تقرير يومي مضاف ·
`QualityCheckFailed` فحص جودة غير مطابق · `SafetyCheckDanger` فحص سلامة خطير ·
`SitePhaseDelayed` مرحلة موقع متأخرة · (وأضف `PermissionChanged` تغيير صلاحية).
استخدم `[Display(Name=...)]` كما في بقية الـ enums، واقرأها بـ `EnumDisplayHelper` الموجود.

## ٣. جدول جديد: تفضيلات الإشعارات (لوحة المفاتيح في 1t)
```csharp
public class NotificationSetting {
  public int Id { get; set; }
  public string UserId { get; set; }
  public NotificationEventType EventType { get; set; }
  public bool IsEnabled { get; set; } = true;
}
```
أضف `DbSet<NotificationSetting>` + فهرس فريد (UserId, EventType) + بذرة «كل الأحداث مفعّلة»
لكل مستخدم جديد. تعديلها محكوم بصلاحية `Notifications.Manage`.

## ٤. `NotificationService` — إطلاق الأحداث الأحد عشر
اربط الإطلاق بالنقاط الفعلية:
| الحدث | نقطة الإطلاق | المستلمون |
|---|---|---|
| مهمة مُكلَّفة | إضافة `TaskAssignee` | المكلَّف |
| تغيّر حالة مهمة | تعديل `ProjectTask.Status` | مدير المشروع + الفريق |
| مرحلة مكتملة | `ProjectStage.Status = Completed` | مدير النظام + الفريق |
| تكلفة مكتملة | `ProjectCost.Status = Completed` (نفس مكان الترحيل للمالية) | قسم المالية |
| طلب توريد جديد | إنشاء `SiteSupplyRequest` | قسم التوريدات + مدير النظام |
| تقرير يومي مضاف | إنشاء `SiteDailyReport` | مدير المشروع + مدير النظام |
| فحص جودة غير مطابق | `SiteQualityCheck.Result = Fail` | مدير الموقع + مهندس الجودة |
| فحص سلامة خطير | `SiteSafetyCheck.Result = Danger` | مدير الموقع + مدير النظام |
| مهمة متأخرة · تسليم يقترب (٤٨ س) · مرحلة موقع متأخرة | **وظيفة مجدولة** | المسؤول / مدير المشروع |

الأحداث الزمنية الثلاثة تحتاج `IHostedService` (أو `BackgroundService`) يعمل كل ساعة:
يقارن `PlannedEndDate`/`DueDate` بالوقت الحالي، ويحسب المتأخر والذي يستحق خلال ٤٨ ساعة،
ولا يُكرّر إشعاراً لنفس السجل ونفس الحدث في اليوم نفسه (تحقّق قبل الإنشاء).
احترم تفضيلات البند ٣ قبل الإنشاء.

## ٥. `NotificationsController` — إضافات
- `Index`: مرشّحات (الحدث، الوحدة، المشروع، الموقع، الفترة) + ترقيم + عدّادات التبويبات
  في `ViewData` + عدّاد «تحتاج إجراءً».
- `Dropdown()`: يُرجع Partial بآخر ٥ إشعارات + عدد غير المقروء (للجرس، 1u).
- `MarkAsRead` و`MarkAllAsRead`: أضف مساراً يُرجع JSON لاستخدام jQuery بلا إعادة تحميل
  (الحالي يعمل بـ Redirect فقط — احفظه للتوافق).
- `UnreadCount()`: نقطة خفيفة يستدعيها الجرس كل ٦٠ ثانية.

## ٦. إخفاء اسم العميل (`▓▓▓▓`) — 1g و1h و1i
Extension أو TagHelper واحد يُستخدم في كل مكان يظهر فيه اسم العميل:
```csharp
@Html.ClientNameOrMasked(project)   // يعيد الاسم إذا كان لدى المستخدم PR.Clients، وإلا "▓▓▓▓"
```
النص المخفي بلون `rgba(32,31,29,.35)`. **لا تُرسل اسم العميل إلى العميل (المتصفح) أصلاً**
عند نقص الصلاحية — أخفِه في الاستعلام لا في CSS.

## ٧. تحقّق الأوزان ١٠٠٪ — 1i
- تحقّق في الـ Controller قبل الحفظ: مجموع `ProjectStage.Weight` للمشروع = 100،
  ومجموع `ProjectStep.Weight` داخل كل مرحلة = 100 (رسالة خطأ عربية).
- شارة حية في الواجهة: `١٠٠٪ ✓` ذهبية عند التساوي، وعند الاختلاف تُظهر المجموع الحالي
  بصيغة `٩٥٪ ⚠` بلون تحذير.
- إعادة الحساب عبر `ProjectCalculationService` الموجود: إكمال خطوة → نسبة المرحلة →
  نسبة المشروع، وأيام التأخير/التسليم المبكر (موجودة في الحسابات).

## ٨. الصلاحيات الفعلية — 1d
`UserPermissions` موجود. أضف في `PermissionService` (إن لم يوجد):
`GetEffectivePermissions(userId)` = صلاحيات الأدوار ∪ الاستثناءات اليدوية،
مع تعليم مصدر كل صلاحية (دور / يدوي / محجوب) لأن الشاشة تعرض شارة المصدر.
التغيير يُطبَّق فوراً بلا إعادة دخول → أبطِل أي كاش صلاحيات عند الحفظ.

## ٩. التقارير اليومية الناقصة — 1m و1o
استعلام واحد قابل للاستخدام في الشاشتين:
```
للموقع النشط: كل التواريخ من StartDate إلى اليوم، ناقص تواريخ SiteDailyReport الموجودة،
ناقص أيام الجمعة (اجعل يوم/أيام الإجازة قيمة قابلة للتهيئة في appsettings).
```
- في 1m: عمود «تقرير اليوم» = `✓` أو «ناقص»، وبند شريط الأرقام «تقارير اليوم الناقصة».
- في 1o: عمود التواريخ الجانبي يؤطّر الأيام الناقصة بإطار متقطّع ذهبي.

## ١٠. العدّادات والعروض المحفوظة
- **أ) عدّادات التبويبات والمرشّحات**: احسبها بـ `GroupBy(Status)` واحد لكل شاشة
  ومرّرها في `ViewData` — لا استعلام لكل تبويب.
- **ب) (اختياري، مرحلة لاحقة)** جدول `SavedView(Id, UserId, Entity, Name, QueryJson)`
  إن أردت «العروض المحفوظة» في القائمة الجانبية. غير مطلوب لتطابق الشاشات الحالية.

## ١١. الإجراءات الجماعية (مربعات التحديد)
الشاشات 1c و1g و1h و1j و1s فيها عمود تحديد. أضف actions تستقبل `int[] ids`:
تغيير الحالة · تعيين مهندس · أرشفة · تصدير المحدد. أظهر شريط الإجراءات الذهبي
(«تم تحديد ٣ · …») فقط عند وجود تحديد، وسجّل كل إجراء في `AuditService`.

## ١٢. التصدير والطباعة
«تصدير» في كل قائمة و«طباعة» في شاشات التفاصيل و«طباعة مطالبة» في 1k.
استخدم مكتبة واحدة للـ PDF (يجب أن تدعم العربية RTL) وأخرى للـ Excel، واجعل
التصدير يحترم المرشّحات الحالية والصلاحيات (`Finance.Print`, `Reports.Export`).

## ١٣. اعتماد الفحوصات — 1p
`SiteQualityCheck` و`SiteSafetyCheck` يحملان `IsApproved/ApprovedAt/ApprovedById`.
أضف actions: `Approve`, `Recheck`, `NotifySiteManager` (يستدعي `NotificationService`)
محكومة بصلاحية `Quality.Approve`. عمود «الاعتماد» يعرض «معتمد ✓» أو شارة «بانتظار».

## ١٤. البحث الشامل في الشريط العلوي
`SearchController.Global(q)` يبحث في: المشاريع (الكود والاسم) · المواقع · العملاء
(بحسب الصلاحية) · المستندات (`ProjectDocument` + `SiteDocument`) · المستخدمين —
ويعيد نتائج مجمّعة بالنوع، محدودة بصلاحيات المستخدم (مهندس التصميم يرى مشاريعه فقط).

## ١٥. سجل التغييرات — تبويب في شاشات التفاصيل
`AuditLog` و`AuditService` موجودان: تأكّد أن كل إضافة/تعديل/حذف في الـ Controllers
تمرّ عبر `AuditService`، واعرضه في تبويب «سجل التغييرات» (المستخدم، الوقت، الحقل، قبل/بعد).

---

## تنبيهات
- لا تُدخل Tailwind/React/أي حزمة جديدة: Bootstrap 5 وjQuery الموجودان في `wwwroot/lib` كافيان.
- الخطوط: `wwwroot/fonts/tajawal` موجود. إن أردت التطابق التام مع المرجع أضف
  `Noto Naskh Arabic` محلياً (لا CDN إن كان الاستخدام داخلياً/دون إنترنت).
- كل ترحيل EF جديد: اعرض ملف الـ migration قبل `database update`.
- عزل البيانات: مهندس التصميم يرى مشاريعه فقط، ولا يرى المالية ولا بيانات العملاء —
  طبّقه في الاستعلام لا في الواجهة.
