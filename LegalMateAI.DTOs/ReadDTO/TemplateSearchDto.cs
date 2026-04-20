// LegalMateAI.DTOs/ReadDTO/TemplateSearchDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    /// <summary>
    /// نموذج البحث عن قوالب العقود
    /// يُستخدم في استقبال معاملات البحث من Query String
    /// </summary>
    public class TemplateSearchDto
    {
        /// <summary>
        /// كلمة البحث (تبحث في الاسم، الوصف، الكلمات المفتاحية)
        /// </summary>
        public string? SearchTerm { get; set; }
        
        /// <summary>
        /// نوع العقد (فلترة حسب النوع)
        /// </summary>
        public ContractType? ContractType { get; set; }
        
        /// <summary>
        /// إظهار القوالب المميزة فقط
        /// </summary>
        public bool FeaturedOnly { get; set; }
        
        /// <summary>
        /// إظهار القوالب الأكثر استخداماً فقط
        /// </summary>
        public bool PopularOnly { get; set; }
        
        /// <summary>
        /// رقم الصفحة (للتقسيم)
        /// </summary>
        private int _page = 1;
        public int Page 
        { 
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }
        
        /// <summary>
        /// عدد العناصر في الصفحة
        /// </summary>
        private int _pageSize = 20;
        public int PageSize 
        { 
            get => _pageSize;
            set => _pageSize = value < 1 ? 20 : (value > 100 ? 100 : value);
        }
    }
}