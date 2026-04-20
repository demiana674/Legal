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
using Microsoft.Extensions.FileProviders;

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
        Description = "منصة المساعدة القانونية الذكية",
        Contact = new OpenApiContact
        {
            Name = "LegalMate Team",
            Email = "support@legalmate.com"
        }
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
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    c.OrderActionsBy((apiDesc) => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

// ===== HTTP Client with API Key =====
builder.Services.AddHttpClient("PythonAI", client =>
{
    var apiKey = builder.Configuration["PythonAI:ApiKey"] ?? "legalmate-ai-secret-key-2024";
    client.BaseAddress = new Uri(builder.Configuration["PythonAI:Url"] ?? "http://localhost:8000/api/v1");
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
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"success\":false,\"message\":\"غير مصرح لك بالدخول. الرجاء تسجيل الدخول.\"}");
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
builder.Services.AddScoped<ILawService, LawService>();
builder.Services.AddScoped<ILawyerBranchService, LawyerBranchService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ILawyerService, LawyerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ILawyerProfileService, LawyerProfileService>();
builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IPredefinedContractService, PredefinedContractService>();
builder.Services.AddScoped<ICaseService, CaseService>();
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

// ===== Static Files =====
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads")),
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
        // تأكد من إنشاء قاعدة البيانات
        await context.Database.EnsureCreatedAsync();

        // Seed Governorates
        var governorates = EgyptData.GetGovernoratesWithCities();
        foreach (var governorate in governorates)
        {
            var existingGovernorate = await context.Governorates
                .FirstOrDefaultAsync(g => g.Id == governorate.Id);
            if (existingGovernorate == null)
                await context.Governorates.AddAsync(governorate);
        }
        await context.SaveChangesAsync();
        logger.LogInformation($"✅ Seeded governorates");

 
    

        // Seed Admins
        var admins = AdminSeedData.GetDefaultAdmins(configuration, encryption);
        foreach (var admin in admins)
        {
            var existingAdmin = await context.Admins.FirstOrDefaultAsync(a => a.Email == admin.Email);
            if (existingAdmin == null) 
                await context.Admins.AddAsync(admin);
        }                                                                               
        await context.SaveChangesAsync();
        logger.LogInformation($"✅ Seeded admins");
        
        // Seed Lawyer Specialties - بدون تحديد Id
        if (!await context.LawyerSpecialties.AnyAsync())
        {
            var specialties = LawyerSpecialty.EgyptianLawyerSpecialties();
            await context.LawyerSpecialties.AddRangeAsync(specialties);
            await context.SaveChangesAsync();
            logger.LogInformation($"✅ Added {specialties.Count} lawyer specialties");
        }
        else
        {
            logger.LogInformation($"⏭️ Lawyer specialties already exist");
        }

        // Seed Contract Templates
        var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        await ContractTemplateSeeder.SeedTemplatesAsync(context, webRootPath, logger);
        
        logger.LogInformation("✅ Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding database");
        // مش هتوقف البرنامج - هيكمل عادي
    }
}

// ===== Configure pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "🏛️ LegalMate AI v1.0");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "LegalMate AI";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

// Root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();