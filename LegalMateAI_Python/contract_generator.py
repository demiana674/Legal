# LegalMateAI_Python/contract_generator.py
from model_loader import model_loader

class ContractGenerator:
    def __init__(self):
        self.model = model_loader
    
    async def generate(self, template_id: str, data: dict) -> str:
        """إنشاء عقد بناءً على القالب والبيانات"""
        
        templates = {
            "rental": """عقد إيجار
            الطرف الأول: {PartyName}
            الطرف الثاني: {TenantName}
            المدة: {Duration}
            القيمة الإيجارية: {RentAmount}
            العنوان: {PropertyAddress}""",
            
            "employment": """عقد عمل
            صاحب العمل: {EmployerName}
            الموظف: {EmployeeName}
            المسمى الوظيفي: {JobTitle}
            الراتب: {Salary}""",
            
            "sale": """عقد بيع
            البائع: {SellerName}
            المشتري: {BuyerName}
            المبيع: {ItemDescription}
            الثمن: {Price}"""
        }
        
        template = templates.get(template_id, templates["rental"])
        
        # ملء البيانات
        contract = template
        for key, value in data.items():
            contract = contract.replace(f"{{{key}}}", str(value))
        
        # تحسين العقد باستخدام النموذج
        improve_prompt = f"""قم بتحسين العقد التالي ليصبح أكثر احترافية وقانونية:
        
        {contract}
        
        العقد المحسن:"""
        
        improved = self.model.generate(improve_prompt, max_tokens=800)
        
        return improved if len(improved) > len(contract) else contract