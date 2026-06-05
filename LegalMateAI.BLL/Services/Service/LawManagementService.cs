using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;


namespace LegalMateAI.BLL.Services.Service
{
    public class LawManagementService : ILawManagementService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;        
        private readonly ILogger<LawManagementService> _logger;

        public LawManagementService(
            LegalMateDbContext context,
            IWebHostEnvironment env,           
            ILogger<LawManagementService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }


        public async Task<Guid?> AddLawAsync(Guid adminId, CreateLawDto request)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return null;

            string? pdfPath = null;
            if (request.PdfFile != null)
            {
                pdfPath = await StoreDocumentFileAsync(request.PdfFile, request.Category, request.Name, request.LawNumber, request.Year);
            }
            else if (!string.IsNullOrEmpty(request.PdfFileUrl))
            {
                pdfPath = request.PdfFileUrl;
            }

            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LawNumber = request.LawNumber,
                Year = request.Year,
                Category = request.Category,
                Description = request.Description,
                PdfFileUrl = pdfPath,
                SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords,
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow,
                AddedByAdminId = adminId
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();            

            _logger.LogInformation("Law added. AdminId: {AdminId}, LawId: {LawId}, Name: {Name}", adminId, law.Id, law.Name);

            return law.Id;
        }


        public async Task<bool> RemoveLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            await RemoveDocumentFileAsync(law.PdfFileUrl);

            _context.Laws.Remove(law);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Law removed. AdminId: {AdminId}, LawId: {LawId}", adminId, lawId);
            return true;
        }
       


        private string BuildDocumentFileName(string lawName, string? lawNumber, int? year)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(lawName.Where(c => !invalidChars.Contains(c)).ToArray());
            safeName = Regex.Replace(safeName, @"\s+", "_");

            if (!string.IsNullOrEmpty(lawNumber)) safeName = $"{safeName}_رقم_{lawNumber}";
            if (year.HasValue) safeName = $"{safeName}_لسنة_{year}";
            if (safeName.Length > 100) safeName = safeName[..100];

            return $"{safeName}.pdf";
        }


        private async Task<string?> StoreDocumentFileAsync(IFormFile? pdfFile, LawCategory category, string lawName, string? lawNumber, int? year)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return null;

            var extension = Path.GetExtension(pdfFile.FileName);
            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                return null;

            var folderName = GetCategoryFolderName(category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);

            if (!Directory.Exists(lawsFolder))
                Directory.CreateDirectory(lawsFolder);

            var safeName = BuildDocumentFileName(lawName, lawNumber, year);
            var filePath = Path.Combine(lawsFolder, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await pdfFile.CopyToAsync(stream);

            return $"/uploads/laws/{folderName}/{safeName}";
        }

        private string GetCategoryFolderName(LawCategory category) => category switch
        {
            LawCategory.Constitutional => "constitutional",
            LawCategory.Civil => "civil",
            LawCategory.Criminal => "criminal",
            LawCategory.Commercial => "commercial",
            LawCategory.Labor => "labor",
            LawCategory.Tax => "tax",
            LawCategory.Family => "family",
            LawCategory.Procedure => "procedure",
            LawCategory.RealEstate => "real_estate",
            LawCategory.Financial => "financial",
            LawCategory.Investment => "investment",
            LawCategory.Social => "social",
            LawCategory.Educational => "educational",
            LawCategory.Economic => "economic",
            LawCategory.Maritime => "maritime",
            LawCategory.Administrative => "administrative",
            LawCategory.International => "international",
            _ => "other"
        };

        private Task RemoveDocumentFileAsync(string? pdfFileUrl)
        {
            if (string.IsNullOrEmpty(pdfFileUrl) || !pdfFileUrl.StartsWith("/uploads/"))
                return Task.CompletedTask;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, pdfFileUrl.TrimStart('/'));

            if (File.Exists(filePath))
                File.Delete(filePath);                
            
            return Task.CompletedTask;
        }


    }
}