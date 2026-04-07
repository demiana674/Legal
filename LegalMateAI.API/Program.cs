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
using DinkToPdf;
using DinkToPdf.Contracts;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===== Add services =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== Swagger with JWT support =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "LegalMateAI API", 
        Version = "v1.0",
        Description = "AI-Powered Legal Assistance Platform",
        Contact = new OpenApiContact
        {
            Name = "LegalMate Team",
            Email = "support@legalmate.com"
        }
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ===== HTTP Client with API Key =====
builder.Services.AddHttpClient("PythonAI", client =>
{
    var apiKey = builder.Configuration["PythonAI:ApiKey"] ?? "legalmate-ai-secret-key-2024";
    client.BaseAddress = new Uri(builder.Configuration["PythonAI:Url"] ?? "http://192.168.1.14:8000/api/v1");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("PythonAI:TimeoutSeconds", 90));
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
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
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration2024")),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ===== Database =====
builder.Services.AddDbContext<LegalMateDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
           .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// ===== Repositories =====
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// ===== Services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ILegalService, LegalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ILawyerService, LawyerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ILawyerProfileService, LawyerProfileService>();
builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAdminLawyerService, AdminLawyerService>();

// ===== AI Service =====
builder.Services.AddScoped<IAIService, PythonAIService>();
builder.Services.AddScoped<PdfGenerationService>();

// ===== HttpContextAccessor =====
builder.Services.AddHttpContextAccessor();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
QuestPDF.Settings.License = LicenseType.Community;

// ===== Seed Data =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LegalMateDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    try
    {
        await context.Database.EnsureCreatedAsync();

        // ===== Seed Governorates and Cities =====
        var governorates = EgyptData.GetGovernoratesWithCities();
        foreach (var governorate in governorates)
        {
            var existingGovernorate = await context.Governorates
                .Include(g => g.Cities)
                .FirstOrDefaultAsync(g => g.Id == governorate.Id);

            if (existingGovernorate == null)
                await context.Governorates.AddAsync(governorate);
            else
            {
                bool needsUpdate = false;
                if (existingGovernorate.Name != governorate.Name) { existingGovernorate.Name = governorate.Name; needsUpdate = true; }
                foreach (var seedCity in governorate.Cities)
                {
                    var existingCity = existingGovernorate.Cities.FirstOrDefault(c => c.Id == seedCity.Id);
                    if (existingCity == null) { seedCity.GovernorateId = existingGovernorate.Id; existingGovernorate.Cities.Add(seedCity); needsUpdate = true; }
                    else if (existingCity.Name != seedCity.Name) { existingCity.Name = seedCity.Name; needsUpdate = true; }
                }
                if (needsUpdate) context.Governorates.Update(existingGovernorate);
            }
        }
        await context.SaveChangesAsync();

        // ===== Seed Egyptian Laws =====
        var laws = EgyptianLawSeedData.GetInitialLaws();
        foreach (var law in laws)
        {
            var existingLaw = await context.EgyptianLaws.FirstOrDefaultAsync(l => l.Id == law.Id);
            if (existingLaw == null) await context.EgyptianLaws.AddAsync(law);
            else
            {
                bool needsUpdate = false;
                if (existingLaw.Title != law.Title) { existingLaw.Title = law.Title; needsUpdate = true; }
                if (existingLaw.TitleAr != law.TitleAr) { existingLaw.TitleAr = law.TitleAr; needsUpdate = true; }
                if (existingLaw.Description != law.Description) { existingLaw.Description = law.Description; needsUpdate = true; }
                if (existingLaw.Category != law.Category) { existingLaw.Category = law.Category; needsUpdate = true; }
                if (existingLaw.Status != law.Status) { existingLaw.Status = law.Status; needsUpdate = true; }
                if (needsUpdate) context.EgyptianLaws.Update(existingLaw);
            }
        }
        await context.SaveChangesAsync();

        // ===== Seed Admins =====
        var admins = AdminSeedData.GetDefaultAdmins(configuration, encryption);
        foreach (var admin in admins)
        {
            var existingAdmin = await context.Admins.FirstOrDefaultAsync(a => a.Email == admin.Email);
            if (existingAdmin == null) await context.Admins.AddAsync(admin);
            else
            {
                bool needsUpdate = false;
                if (existingAdmin.FullName != admin.FullName) { existingAdmin.FullName = admin.FullName; needsUpdate = true; }
                if (existingAdmin.PhoneNumber != admin.PhoneNumber) { existingAdmin.PhoneNumber = admin.PhoneNumber; needsUpdate = true; }
                if (existingAdmin.PasswordHash != admin.PasswordHash) { existingAdmin.PasswordHash = admin.PasswordHash; needsUpdate = true; }
                if (needsUpdate) context.Admins.Update(existingAdmin);
            }
        }
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding database");
    }
}

// ===== Configure pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

app.Run();
