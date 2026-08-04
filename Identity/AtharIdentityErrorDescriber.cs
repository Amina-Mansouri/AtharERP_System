using Microsoft.AspNetCore.Identity;

namespace AtharERP_System.Identity
{
    public class AtharIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
            => new() { Code = nameof(DefaultError), Description = "حدث خطأ غير متوقع" };

        public override IdentityError ConcurrencyFailure()
            => new() { Code = nameof(ConcurrencyFailure), Description = "فشلت العملية، تم تعديل البيانات من مستخدم آخر. أعد المحاولة" };

        public override IdentityError PasswordMismatch()
            => new() { Code = nameof(PasswordMismatch), Description = "كلمة المرور غير صحيحة" };

        public override IdentityError InvalidToken()
            => new() { Code = nameof(InvalidToken), Description = "الرمز غير صالح" };

        public override IdentityError LoginAlreadyAssociated()
            => new() { Code = nameof(LoginAlreadyAssociated), Description = "هذا الحساب مرتبط بمستخدم آخر بالفعل" };

        public override IdentityError InvalidUserName(string? userName)
            => new() { Code = nameof(InvalidUserName), Description = $"اسم المستخدم '{userName}' غير صالح" };

        public override IdentityError InvalidEmail(string? email)
            => new() { Code = nameof(InvalidEmail), Description = $"البريد الإلكتروني '{email}' غير صالح" };

        public override IdentityError DuplicateUserName(string userName)
            => new() { Code = nameof(DuplicateUserName), Description = $"اسم المستخدم '{userName}' مستخدم بالفعل" };

        public override IdentityError DuplicateEmail(string email)
            => new() { Code = nameof(DuplicateEmail), Description = $"البريد الإلكتروني '{email}' مستخدم بالفعل" };

        public override IdentityError InvalidRoleName(string? role)
            => new() { Code = nameof(InvalidRoleName), Description = $"اسم الدور '{role}' غير صالح" };

        public override IdentityError DuplicateRoleName(string role)
            => new() { Code = nameof(DuplicateRoleName), Description = $"اسم الدور '{role}' مستخدم بالفعل" };

        public override IdentityError UserAlreadyHasPassword()
            => new() { Code = nameof(UserAlreadyHasPassword), Description = "المستخدم لديه كلمة مرور مسجلة بالفعل" };

        public override IdentityError UserLockoutNotEnabled()
            => new() { Code = nameof(UserLockoutNotEnabled), Description = "تعطيل الحساب المؤقت غير مفعّل لهذا المستخدم" };

        public override IdentityError UserAlreadyInRole(string role)
            => new() { Code = nameof(UserAlreadyInRole), Description = $"المستخدم لديه الدور '{role}' بالفعل" };

        public override IdentityError UserNotInRole(string role)
            => new() { Code = nameof(UserNotInRole), Description = $"المستخدم لا يملك الدور '{role}'" };

        public override IdentityError PasswordTooShort(int length)
            => new() { Code = nameof(PasswordTooShort), Description = $"يجب أن تحتوي كلمة المرور على {length} حروف على الأقل" };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
            => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"يجب أن تحتوي كلمة المرور على {uniqueChars} حروف مختلفة على الأقل" };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل (مثل ! @ # $)" };

        public override IdentityError PasswordRequiresDigit()
            => new() { Code = nameof(PasswordRequiresDigit), Description = "يجب أن تحتوي كلمة المرور على رقم واحد على الأقل" };

        public override IdentityError PasswordRequiresLower()
            => new() { Code = nameof(PasswordRequiresLower), Description = "يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل (a-z)" };

        public override IdentityError PasswordRequiresUpper()
            => new() { Code = nameof(PasswordRequiresUpper), Description = "يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل (A-Z)" };

        public override IdentityError RecoveryCodeRedemptionFailed()
            => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "فشل استخدام رمز الاسترداد" };
    }
}