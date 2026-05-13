using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.DAL.Repositories.Repository;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.SeedData;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.Infrastructure.Services.IService;
using LegalMateAI.Infrastructure.Services.Service;
using Microsoft.OpenApi.Models;
using LegalMateAI.API.Middleware;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using DinkToPdf;
using DinkToPdf.Contracts;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ===== Add services =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// ===== Swagger with JWT support =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🏛️ LegalMate AI",
        Version = "v1.0",
        Description = "منصة المساعدة القانونية الذكية - Powered by Local AI (Ollama/LM Studio)",
        Contact = new OpenApiContact { Name = "LegalMate Team", Email = "support@legalmate.com" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "أدخل JWT Token مع كلمة Bearer: 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ===== HTTP Clients - Local AI Only =====
builder.Services.AddHttpClient<LocalAIService>(client =>
{
    var baseUrl = builder.Configuration["LocalAI:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

// ===== Authentication =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration2024")),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===== Database =====
builder.Services.AddDbContext<LegalMateDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// ===== Repositories =====
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
// ===== Services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
// ❌ builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>(); // شلناها
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IContractService, ContractService>();

// AI Services
builder.Services.AddScoped<LocalAIService>();
builder.Services.AddScoped<IAIService, LocalAIService>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<ILawyerService, LawyerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ILawyerProfileService, LawyerProfileService>();
builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
builder.Services.AddScoped<ILocationService, LocationService>();
// ❌ builder.Services.AddScoped<IPredefinedContractService, PredefinedContractService>(); // شلناها
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<PdfGenerationService>();
builder.Services.AddScoped<ILawService, LawService>();
builder.Services.AddScoped<ILawyerBranchService, LawyerBranchService>();
builder.Services.AddScoped<LawParserService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddHttpContextAccessor();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
QuestPDF.Settings.License = LicenseType.Community;

// ===== Static Files =====
var uploadsPath = Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ===== Seed Data =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LegalMateDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    try
    {
        await context.Database.EnsureCreatedAsync();

        var governorates = EgyptData.GetGovernorates();
        foreach (var gov in governorates)
        {
            if (!await context.Governorates.AnyAsync(g => g.Id == gov.Id))
                await context.Governorates.AddAsync(gov);
        }
        await context.SaveChangesAsync();
        logger.LogInformation($"✅ Seeded {await context.Governorates.CountAsync()} governorates");

        var cities = EgyptData.GetCities();
        foreach (var city in cities)
        {
            if (!await context.Cities.AnyAsync(c => c.Id == city.Id))
                await context.Cities.AddAsync(city);
        }
        await context.SaveChangesAsync();
        logger.LogInformation($"✅ Seeded {await context.Cities.CountAsync()} cities");

        var admins = AdminSeedData.GetDefaultAdmins(configuration, encryption);
        var addedAdmins = new List<Admin>();
        foreach (var admin in admins)
        {
            var existing = await context.Admins.FirstOrDefaultAsync(a => a.Email == admin.Email);
            if (existing == null) { await context.Admins.AddAsync(admin); addedAdmins.Add(admin); logger.LogInformation($"✅ Added admin: {admin.FullName}"); }
            else addedAdmins.Add(existing);
        }
        await context.SaveChangesAsync();

        foreach (var profile in AdminSeedData.GetDefaultAdminProfiles(configuration, encryption, addedAdmins))
        {
            if (!await context.AdminProfiles.AnyAsync(p => p.AdminId == profile.AdminId))
                await context.AdminProfiles.AddAsync(profile);
        }
        await context.SaveChangesAsync();
        logger.LogInformation($"✅ Seeded admin profiles");

        try
        {
            if (!await context.LawyerSpecialties.AnyAsync())
            {
                await context.LawyerSpecialties.AddRangeAsync(LawyerSpecialty.EgyptianLawyerSpecialties());
                await context.SaveChangesAsync();
                logger.LogInformation($"✅ Seeded lawyer specialties");
            }
        }
        catch (Exception ex) { logger.LogWarning($"⚠️ Lawyer specialties: {ex.Message}"); }

        try
        {
            if (!await context.LegalSpecializations.AnyAsync())
            {
                await context.LegalSpecializations.AddRangeAsync(LegalSpecialization.EgyptianSpecializations());
                await context.SaveChangesAsync();
                logger.LogInformation($"✅ Seeded legal specializations");
            }
        }
        catch (Exception ex) { logger.LogWarning($"⚠️ Legal specializations: {ex.Message}"); }

        try
        {
            // var lawsPath = Path.Combine(app.Environment.ContentRootPath, "SeedData", "manshurat_laws_complete.json");
            var lawsPath = Path.Combine(app.Environment.ContentRootPath,  "SeedData", "manshurat_laws_final_clean.json");
            if (File.Exists(lawsPath))
                await LawSeeder.SeedFromJsonFileAsync(context, logger, lawsPath);
            else
                await LawSeeder.SeedEgyptianLawsAsync(context, logger);
        }
        catch (Exception ex) { logger.LogWarning($"⚠️ Laws: {ex.Message}"); }

        try
        {
            var contractsPath = Path.Combine(app.Environment.ContentRootPath, "SeedData", "contracts.json");
            if (File.Exists(contractsPath))
            {
                var contractsJson = await File.ReadAllTextAsync(contractsPath);
                var contractsData = JsonSerializer.Deserialize<List<ContractSeedDto>>(contractsJson);
                if (contractsData?.Any() == true)
                {
                    foreach (var c in contractsData)
                    {
                        var template = new ContractTemplate
                        {
                            Id = Guid.NewGuid(), Name = c.name ?? "عقد",
                            Type = Enum.TryParse<ContractType>(c.type, out var ct) ? ct : ContractType.Other,
                            Description = c.description ?? "",
                            TemplateContent = c.pdfUrl ?? c.sourceUrl ?? "",
                            IsActive = true, CreatedAt = DateTime.UtcNow
                        };
                        await context.ContractTemplates.AddAsync(template);
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex) { logger.LogWarning($"⚠️ Contracts: {ex.Message}"); }

        logger.LogInformation("═══════════════════════════════════════");
        logger.LogInformation("✅ Database seeding completed!");
        logger.LogInformation("👤 Admin 1: admin@legalmate.com / Admin@123");
        logger.LogInformation("👤 Admin 2: verifier@legalmate.com / Verifier@123");
        logger.LogInformation("🤖 AI: Local AI (Ollama/LM Studio)");
        logger.LogInformation("═══════════════════════════════════════");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding database");
    }
}

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "🏛️ LegalMate AI v1.0 - Local AI");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();
app.MapGet("/api/health", () => new { status = "healthy", timestamp = DateTime.UtcNow, version = "1.0.0", ai = "Local AI (Ollama/LM Studio)" });
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public class ContractSeedDto
{
    public string? name { get; set; }
    public string? type { get; set; }
    public string? description { get; set; }
    public string? sourceUrl { get; set; }
    public string? pdfUrl { get; set; }
    public string? searchKeywords { get; set; }
}