using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using BCrypt.Net;

namespace LegalMateAI.Tests.Helpers;

public static class TestDataFactory
{
    private static int _counter = 0;
    private static readonly Random _random = new();

    public static User CreateTestUser(
        Guid? id = null,
        string? email = null,
        UserRole role = UserRole.User,
        AccountStatus status = AccountStatus.Active)
    {
        _counter++;
        return new User
        {
            UserID = id ?? Guid.NewGuid(),
            FirstName = $"Test{_counter}",
            LastName = "User",
            Email = email ?? $"test{_counter}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Phone = "01000000000",
            NationalId = $"1234567890{_counter:D4}",
            Role = role,
            IsActive = status == AccountStatus.Active,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            JoinDate = DateTime.UtcNow,
            EmailVerified = true
        };
    }

    public static LawyerProfile CreateTestLawyerProfile(Guid userId, LawyerVerificationStatus status = LawyerVerificationStatus.Active)
    {
        return new LawyerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LicenseNumber = $"LAW-{DateTime.Now:yyyy}{_random.Next(1000, 9999)}",
            BarAssociation = "نقابة المحامين المصرية",
            YearsOfExperience = _random.Next(1, 30),
            VerificationStatus = status,
            PracticeDegree = "محامي أول",
            OfficeAddress = "شارع النصر، القاهرة",
            City = "مدينة نصر",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Appointment CreateTestAppointment(Guid userId, Guid lawyerId, AppointmentStatus status = AppointmentStatus.Pending)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentNumber = $"APT-{DateTime.Now:yyyyMMdd}-{_random.Next(1000, 9999)}",
            UserID = userId,
            LawyerId = lawyerId,
            AppointmentType = "استشارة قانونية",
            Date = DateTime.UtcNow.AddDays(3),
            Time = "10:00 AM",
            DurationMinutes = 60,
            Location = "المكتب الرئيسي - وسط البلد",
            Notes = "مراجعة عقد إيجار",
            Status = status,
            RequestedAt = DateTime.UtcNow,
            IsUrgent = false
        };
    }

    public static Case CreateTestCase(Guid clientId, Guid? lawyerId = null)
    {
        return new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = $"CS-{DateTime.Now:yyyyMMdd}-{_random.Next(1000, 9999)}",
            Title = "نزاع ملكية عقارية",
            Description = "خلاف على ملكية عقار في مدينة نصر",
            ClientId = clientId,
            LawyerId = lawyerId,
            Court = "محكمة القاهرة الابتدائية",
            NextHearingDate = DateTime.UtcNow.AddDays(14),
            Status = CaseStatus.Active,
            Priority = CasePriority.High,
            CaseType = "مدني",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Contract CreateTestContract(Guid userId, Guid? lawyerId = null, ContractStatus status = ContractStatus.Draft)
    {
        return new Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = $"CNT-{DateTime.Now:yyyyMMdd}-{_random.Next(1000, 9999)}",
            UserId = userId,
            LawyerId = lawyerId,
            Title = "عقد إيجار شقة سكنية",
            Type = ContractType.Rental,
            Content = "هذا عقد إيجار بين الطرف الأول والطرف الثاني...",
            PartyName = "شركة العقارية للتطوير",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            Value = "24000 ج.م",
            MonetaryValue = 24000,
            Status = status,
            ProgressPercentage = status == ContractStatus.Active ? 50 : 0,
            CreatedAt = DateTime.UtcNow,
            IsGeneratedByAI = false
        };
    }

    public static EgyptianLaw CreateTestLaw()
    {
        return new EgyptianLaw
        {
            Id = 1,
            LawNumber = "قانون 131 لسنة 1948",
            TitleAr = "القانون المدني",
            Title = "Civil Law",
            Year = 1948,
            Category = LawCategory.Civil,
            Status = LawStatus.Active,
            Description = "القانون المدني المصري",
            PublishedAt = new DateTime(1948, 7, 29),
            ViewCount = 0
        };
    }

    // ✅ دالة CreateTestArticle
    public static LawArticle CreateTestArticle(int lawId, int articleNumber = 1)
    {
        return new LawArticle
        {
            Id = articleNumber,
            LawId = lawId,
            ArticleNumber = articleNumber,
            Title = $"المادة {articleNumber}",
            Content = "تسري النصوص القانونية على جميع المسائل التي تتناولها في لفظها أو في فحواها.",
            Summary = "ملخص المادة",
            IsActive = true
        };
    }

    public static CaseDocument CreateTestCaseDocument(Guid caseId, Guid uploadedBy)
    {
        return new CaseDocument
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            FileName = "case_document.pdf",
            FileUrl = $"/uploads/cases/{caseId}/doc.pdf",
            FileType = "application/pdf",
            FileSize = 2048,
            Description = "مستند مهم للقضية",
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            IsVerified = false
        };
    }

    public static CaseNote CreateTestCaseNote(Guid caseId, Guid writtenBy, bool isPrivate = false)
    {
        return new CaseNote
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Content = "تم استلام المستندات المطلوبة من الموكل",
            WrittenBy = writtenBy,
            CreatedAt = DateTime.UtcNow,
            IsPrivate = isPrivate
        };
    }
}