using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DAL.SeedData
{
    public static class EgyptianLawSeedData
    {
        public static List<EgyptianLaw> GetInitialLaws()
        {
            return new List<EgyptianLaw>
            {
                // ===== 🇪🇬 دستور جمهورية مصر العربية 2014 =====
                new EgyptianLaw
                {
                    Id = 1,
                    LawNumber = "دستور 2014",
                    TitleAr = "دستور جمهورية مصر العربية",
                    Year = 2014,
                    Category = LawCategory.Constitutional,
                    Status = LawStatus.Active,
                    Description = "دستور جمهورية مصر العربية الصادر في 18 يناير 2014",
                    PublishedAt = new DateTime(2014, 1, 18),
                    Articles = GetConstitutionArticles(),
                    Amendments = GetConstitutionAmendments(),
                    Keywords = GetConstitutionKeywords()
                },

                // ===== 1. القانون المدني =====
                new EgyptianLaw
                {
                    Id = 2,
                    LawNumber = "قانون 131 لسنة 1948",
                    TitleAr = "القانون المدني",
                    Year = 1948,
                    Category = LawCategory.Civil,
                    Status = LawStatus.Amended,
                    Description = "القانون المدني المصري",
                    PublishedAt = new DateTime(1948, 7, 29),
                    LastAmendedAt = new DateTime(2022, 1, 1),
                    Articles = GetCivilLawArticles(),
                    Amendments = GetCivilLawAmendments(),
                    Keywords = GetCivilLawKeywords()
                },
                
                // ===== 2. قانون العقوبات =====
                new EgyptianLaw
                {
                    Id = 3,
                    LawNumber = "قانون 58 لسنة 1937",
                    TitleAr = "قانون العقوبات",
                    Year = 1937,
                    Category = LawCategory.Criminal,
                    Status = LawStatus.Amended,
                    Description = "قانون العقوبات المصري",
                    PublishedAt = new DateTime(1937, 8, 5),
                    LastAmendedAt = new DateTime(2021, 5, 15),
                    Articles = GetCriminalLawArticles(),
                    Amendments = GetCriminalLawAmendments(),
                    Keywords = GetCriminalLawKeywords()
                },
                
                // ===== 3. قانون الإجراءات الجنائية =====
                new EgyptianLaw
                {
                    Id = 4,
                    LawNumber = "قانون 150 لسنة 1950",
                    TitleAr = "قانون الإجراءات الجنائية",
                    Year = 1950,
                    Category = LawCategory.Procedure,
                    Status = LawStatus.Amended,
                    Description = "قانون الإجراءات الجنائية المصري",
                    PublishedAt = new DateTime(1950, 9, 15),
                    LastAmendedAt = new DateTime(2020, 8, 10),
                    Articles = GetCriminalProcedureArticles(),
                    Amendments = GetCriminalProcedureAmendments(),
                    Keywords = GetCriminalProcedureKeywords()
                },
                
                // ===== 4. قانون العمل =====
                new EgyptianLaw
                {
                    Id = 5,
                    LawNumber = "قانون 12 لسنة 2003",
                    TitleAr = "قانون العمل",
                    Year = 2003,
                    Category = LawCategory.Labor,
                    Status = LawStatus.Amended,
                    Description = "قانون العمل المصري",
                    PublishedAt = new DateTime(2003, 4, 7),
                    LastAmendedAt = new DateTime(2021, 12, 20),
                    Articles = GetLaborLawArticles(),
                    Amendments = GetLaborLawAmendments(),
                    Keywords = GetLaborLawKeywords()
                },
                
                // ===== 5. قانون الشركات =====
                new EgyptianLaw
                {
                    Id = 6,
                    LawNumber = "قانون 159 لسنة 1981",
                    TitleAr = "قانون شركات المساهمة",
                    Year = 1981,
                    Category = LawCategory.Commercial,
                    Status = LawStatus.Amended,
                    Description = "قانون شركات المساهمة",
                    PublishedAt = new DateTime(1981, 6, 15),
                    LastAmendedAt = new DateTime(2022, 3, 1),
                    Articles = GetCompaniesLawArticles(),
                    Amendments = GetCompaniesLawAmendments(),
                    Keywords = GetCompaniesLawKeywords()
                },
                
                // ===== 6. قانون الأحوال الشخصية =====
                new EgyptianLaw
                {
                    Id = 7,
                    LawNumber = "قانون 25 لسنة 1929",
                    TitleAr = "قانون الأحوال الشخصية",
                    Year = 1929,
                    Category = LawCategory.Family,
                    Status = LawStatus.Amended,
                    Description = "قانون الأحوال الشخصية للمسلمين",
                    PublishedAt = new DateTime(1929, 3, 10),
                    LastAmendedAt = new DateTime(2020, 2, 5),
                    Articles = GetFamilyLawArticles(),
                    Amendments = GetFamilyLawAmendments(),
                    Keywords = GetFamilyLawKeywords()
                },
                
                // ===== 7. قانون الضريبة على الدخل =====
                new EgyptianLaw
                {
                    Id = 8,
                    LawNumber = "قانون 91 لسنة 2005",
                    TitleAr = "قانون الضريبة على الدخل",
                    Year = 2005,
                    Category = LawCategory.Tax,
                    Status = LawStatus.Amended,
                    Description = "قانون الضريبة على الدخل",
                    PublishedAt = new DateTime(2005, 6, 9),
                    LastAmendedAt = new DateTime(2022, 2, 15),
                    Articles = GetTaxLawArticles(),
                    Amendments = GetTaxLawAmendments(),
                    Keywords = GetTaxLawKeywords()
                },
                
                // ===== 8. قانون البناء =====
                new EgyptianLaw
                {
                    Id = 9,
                    LawNumber = "قانون 119 لسنة 2008",
                    TitleAr = "قانون البناء",
                    Year = 2008,
                    Category = LawCategory.RealEstate,
                    Status = LawStatus.Amended,
                    Description = "قانون البناء الموحد",
                    PublishedAt = new DateTime(2008, 6, 4),
                    LastAmendedAt = new DateTime(2021, 9, 30),
                    Articles = GetConstructionLawArticles(),
                    Amendments = GetConstructionLawAmendments(),
                    Keywords = GetConstructionLawKeywords()
                },

                // ===== 9. قانون الإثبات ===== (NEW)
                new EgyptianLaw
                {
                    Id = 10,
                    LawNumber = "قانون 25 لسنة 1968",
                    TitleAr = "قانون الإثبات",
                    Year = 1968,
                    Category = LawCategory.Procedure,
                    Status = LawStatus.Amended,
                    Description = "قانون الإثبات في المواد المدنية والتجارية",
                    PublishedAt = new DateTime(1968, 5, 15),
                    LastAmendedAt = new DateTime(2020, 3, 10),
                    Articles = GetEvidenceLawArticles(),
                    Amendments = GetEvidenceLawAmendments(),
                    Keywords = GetEvidenceLawKeywords()
                },

                // ===== 10. قانون التجارة ===== (NEW)
                new EgyptianLaw
                {
                    Id = 11,
                    LawNumber = "قانون 17 لسنة 1999",
                    TitleAr = "قانون التجارة",
                    Year = 1999,
                    Category = LawCategory.Commercial,
                    Status = LawStatus.Amended,
                    Description = "قانون التجارة الجديد",
                    PublishedAt = new DateTime(1999, 5, 17),
                    LastAmendedAt = new DateTime(2021, 11, 5),
                    Articles = GetCommercialLawArticles(),
                    Amendments = GetCommercialLawAmendments(),
                    Keywords = GetCommercialLawKeywords()
                },

                // ===== 11. قانون البنك المركزي والجهاز المصرفي ===== (NEW)
                new EgyptianLaw
                {
                    Id = 12,
                    LawNumber = "قانون 194 لسنة 2020",
                    TitleAr = "قانون البنك المركزي والجهاز المصرفي",
                    Year = 2020,
                    Category = LawCategory.Financial,
                    Status = LawStatus.Active,
                    Description = "قانون تنظيم عمل البنك المركزي والجهاز المصرفي",
                    PublishedAt = new DateTime(2020, 9, 15),
                    Articles = GetBankingLawArticles(),
                    Amendments = new List<LawAmendment>(), // جديد ولم يعدل بعد
                    Keywords = GetBankingLawKeywords()
                },

                // ===== 12. قانون الاستثمار ===== (NEW)
                new EgyptianLaw
                {
                    Id = 13,
                    LawNumber = "قانون 72 لسنة 2017",
                    TitleAr = "قانون الاستثمار",
                    Year = 2017,
                    Category = LawCategory.Investment,
                    Status = LawStatus.Amended,
                    Description = "قانون الاستثمار الجديد والحوافز الاستثمارية",
                    PublishedAt = new DateTime(2017, 5, 31),
                    LastAmendedAt = new DateTime(2020, 12, 20),
                    Articles = GetInvestmentLawArticles(),
                    Amendments = GetInvestmentLawAmendments(),
                    Keywords = GetInvestmentLawKeywords()
                },

                // ===== 13. قانون الطفل ===== (NEW)
                new EgyptianLaw
                {
                    Id = 14,
                    LawNumber = "قانون 12 لسنة 1996",
                    TitleAr = "قانون الطفل",
                    Year = 1996,
                    Category = LawCategory.Social,
                    Status = LawStatus.Amended,
                    Description = "قانون الطفل وحقوقه ورعايته",
                    PublishedAt = new DateTime(1996, 4, 3),
                    LastAmendedAt = new DateTime(2021, 6, 10),
                    Articles = GetChildLawArticles(),
                    Amendments = GetChildLawAmendments(),
                    Keywords = GetChildLawKeywords()
                },

                // ===== 14. قانون تنظيم الجامعات ===== (NEW)
                new EgyptianLaw
                {
                    Id = 15,
                    LawNumber = "قانون 49 لسنة 1972",
                    TitleAr = "قانون تنظيم الجامعات",
                    Year = 1972,
                    Category = LawCategory.Educational,
                    Status = LawStatus.Amended,
                    Description = "قانون تنظيم الجامعات المصرية",
                    PublishedAt = new DateTime(1972, 6, 23),
                    LastAmendedAt = new DateTime(2019, 9, 15),
                    Articles = GetUniversitiesLawArticles(),
                    Amendments = GetUniversitiesLawAmendments(),
                    Keywords = GetUniversitiesLawKeywords()
                },

                // ===== 15. قانون الإجراءات الضريبية الموحد ===== (NEW)
                new EgyptianLaw
                {
                    Id = 16,
                    LawNumber = "قانون 206 لسنة 2020",
                    TitleAr = "قانون الإجراءات الضريبية الموحد",
                    Year = 2020,
                    Category = LawCategory.Tax,
                    Status = LawStatus.Active,
                    Description = "قانون توحيد إجراءات تحصيل الضرائب",
                    PublishedAt = new DateTime(2020, 10, 25),
                    Articles = GetTaxProcedureLawArticles(),
                    Amendments = new List<LawAmendment>(), // جديد
                    Keywords = GetTaxProcedureLawKeywords()
                },

                // ===== 16. قانون التموين ===== (NEW)
                new EgyptianLaw
                {
                    Id = 17,
                    LawNumber = "قانون 95 لسنة 1945",
                    TitleAr = "قانون التموين",
                    Year = 1945,
                    Category = LawCategory.Economic,
                    Status = LawStatus.Amended,
                    Description = "قانون تنظيم التموين والمواد الغذائية",
                    PublishedAt = new DateTime(1945, 8, 16),
                    LastAmendedAt = new DateTime(2020, 1, 30),
                    Articles = GetSupplyLawArticles(),
                    Amendments = GetSupplyLawAmendments(),
                    Keywords = GetSupplyLawKeywords()
                },

                // ===== 17. قانون المرافعات المدنية والتجارية ===== (NEW)
                new EgyptianLaw
                {
                    Id = 18,
                    LawNumber = "قانون 13 لسنة 1968",
                    TitleAr = "قانون المرافعات المدنية والتجارية",
                    Year = 1968,
                    Category = LawCategory.Procedure,
                    Status = LawStatus.Amended,
                    Description = "قانون المرافعات المدنية والتجارية",
                    PublishedAt = new DateTime(1968, 5, 15),
                    LastAmendedAt = new DateTime(2021, 4, 20),
                    Articles = GetCivilProcedureLawArticles(),
                    Amendments = GetCivilProcedureLawAmendments(),
                    Keywords = GetCivilProcedureLawKeywords()
                },

                // ===== 18. قانون التصالح في بعض مخالفات البناء ===== (NEW)
                new EgyptianLaw
                {
                    Id = 19,
                    LawNumber = "قانون 17 لسنة 2019",
                    TitleAr = "قانون التصالح في بعض مخالفات البناء",
                    Year = 2019,
                    Category = LawCategory.RealEstate,
                    Status = LawStatus.Amended,
                    Description = "قانون التصالح في مخالفات البناء وتقنين الأوضاع",
                    PublishedAt = new DateTime(2019, 2, 13),
                    LastAmendedAt = new DateTime(2021, 10, 10),
                    Articles = GetReconciliationLawArticles(),
                    Amendments = GetReconciliationLawAmendments(),
                    Keywords = GetReconciliationLawKeywords()
                },

                // ===== 19. قانون الرياضة ===== (NEW)
                new EgyptianLaw
                {
                    Id = 20,
                    LawNumber = "قانون 71 لسنة 2017",
                    TitleAr = "قانون الرياضة",
                    Year = 2017,
                    Category = LawCategory.Social,
                    Status = LawStatus.Active,
                    Description = "قانون الرياضة وتنظيم الهيئات الرياضية",
                    PublishedAt = new DateTime(2017, 5, 21),
                    Articles = GetSportsLawArticles(),
                    Amendments = GetSportsLawAmendments(),
                    Keywords = GetSportsLawKeywords()
                },

                // ===== 20. قانون الهجرة غير الشرعية ===== (NEW)
                new EgyptianLaw
                {
                    Id = 21,
                    LawNumber = "قانون 82 لسنة 2016",
                    TitleAr = "قانون مكافحة الهجرة غير الشرعية",
                    Year = 2016,
                    Category = LawCategory.Criminal,
                    Status = LawStatus.Active,
                    Description = "قانون مكافحة الهجرة غير الشرعية وتهريب المهاجرين",
                    PublishedAt = new DateTime(2016, 11, 7),
                    Articles = GetMigrationLawArticles(),
                    Amendments = new List<LawAmendment>(),
                    Keywords = GetMigrationLawKeywords()
                }
            };
        }

        // ===== 1. مواد الدستور المصري =====
        private static List<LawArticle> GetConstitutionArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 1, ArticleNumber = 1, Content = "جمهورية مصر العربية دولة نظامها ديمقراطي، تقوم على المواطنة، والشعب المصري جزء من الأمة العربية." },
                new LawArticle { Id = 2, ArticleNumber = 2, Content = "الإسلام دين الدولة، واللغة العربية لغتها الرسمية، ومبادئ الشريعة الإسلامية المصدر الرئيسي للتشريع." },
                new LawArticle { Id = 3, ArticleNumber = 3, Content = "للمسيحيين واليهود الحق في الاحتكام لشرائعهم في الأحوال الشخصية." },
                new LawArticle { Id = 4, ArticleNumber = 4, Content = "السيادة للشعب وحده، وهو مصدر السلطات." },
                new LawArticle { Id = 5, ArticleNumber = 8, Content = "تلتزم الدولة بتكافؤ الفرص بين المواطنين." },
                new LawArticle { Id = 6, ArticleNumber = 9, Content = "الأسرة أساس المجتمع." }
            };
        }

        // ===== 2. مواد القانون المدني =====
        private static List<LawArticle> GetCivilLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 7, ArticleNumber = 1, Content = "تسري النصوص القانونية على جميع المسائل التي تتناولها في لفظها أو في فحواها." },
                new LawArticle { Id = 8, ArticleNumber = 2, Content = "لا يجوز لأحد أن يحتج بجهالة القانون." },
                new LawArticle { Id = 9, ArticleNumber = 3, Content = "القانون يحكم المسائل محل النص، ولا يسري على ما وقع قبل نفاذه إلا بنص خاص." },
                new LawArticle { Id = 10, ArticleNumber = 4, Content = "إذا لم يوجد نص قانوني يمكن تطبيقه، حكم القاضي بمقتضى العرف." },
                new LawArticle { Id = 11, ArticleNumber = 5, Content = "كل تعاقد لا يكون صحيحاً إذا كان مخالفاً للنظام العام أو الآداب." }
            };
        }

        // ===== 3. مواد قانون العقوبات =====
        private static List<LawArticle> GetCriminalLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 12, ArticleNumber = 1, Content = "لا جريمة ولا عقوبة إلا بناء على قانون." },
                new LawArticle { Id = 13, ArticleNumber = 2, Content = "يعاقب على الشروع في الجريمة إذا كان الشروع بالبدء في التنفيذ." },
                new LawArticle { Id = 14, ArticleNumber = 3, Content = "تطبق القوانين على جميع الأشخاص المقيمين في مصر." },
                new LawArticle { Id = 15, ArticleNumber = 6, Content = "السرقة هي أخذ مال منقول مملوك لغير الفاعل دون رضاه." },
                new LawArticle { Id = 16, ArticleNumber = 9, Content = "القتل العمد يعاقب عليه بالإعدام." }
            };
        }

        // ===== 4. مواد قانون الإجراءات الجنائية =====
        private static List<LawArticle> GetCriminalProcedureArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 17, ArticleNumber = 1, Content = "لا تجوز محاكمة أحد في جريمة إلا بدعوى من النيابة العامة." },
                new LawArticle { Id = 18, ArticleNumber = 2, Content = "النيابة العامة هي التي تباشر الدعوى الجنائية." },
                new LawArticle { Id = 19, ArticleNumber = 3, Content = "للمتهم الحق في أن يوكل محامياً للدفاع عنه." },
                new LawArticle { Id = 20, ArticleNumber = 4, Content = "القبض لا يكون إلا بأمر من النيابة العامة." },
                new LawArticle { Id = 21, ArticleNumber = 5, Content = "التفتيش لا يكون إلا بأمر قضائي." }
            };
        }

        // ===== 5. مواد قانون العمل =====
        private static List<LawArticle> GetLaborLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 22, ArticleNumber = 1, Content = "يُقصد بالعامل كل شخص طبيعي يعمل لقاء أجر لدى صاحب عمل." },
                new LawArticle { Id = 23, ArticleNumber = 2, Content = "مدة العمل لا تجاوز ثماني ساعات في اليوم." },
                new LawArticle { Id = 24, ArticleNumber = 3, Content = "يستحق العامل إجازة سنوية مدتها 21 يوماً." },
                new LawArticle { Id = 25, ArticleNumber = 4, Content = "يحظر تشغيل الأطفال قبل بلوغ سن 15 سنة." }
            };
        }

        // ===== 6. مواد قانون الشركات =====
        private static List<LawArticle> GetCompaniesLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 26, ArticleNumber = 1, Content = "شركة المساهمة هي شركة رأس المال." },
                new LawArticle { Id = 27, ArticleNumber = 2, Content = "لا يقل رأس مال شركة المساهمة عن 250 ألف جنيه." },
                new LawArticle { Id = 28, ArticleNumber = 3, Content = "يتكون مجلس الإدارة من 3 أعضاء على الأقل." },
                new LawArticle { Id = 29, ArticleNumber = 4, Content = "الشركة ذات المسئولية المحدودة لا يقل عدد شركائها عن 2." }
            };
        }

        // ===== 7. مواد قانون الأحوال الشخصية =====
        private static List<LawArticle> GetFamilyLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 30, ArticleNumber = 1, Content = "يشترط لعقد الزواج توثيقه في وثيقة رسمية." },
                new LawArticle { Id = 31, ArticleNumber = 2, Content = "للزوجة حق طلب التطليق للضرر." },
                new LawArticle { Id = 32, ArticleNumber = 3, Content = "مدة الحضانة 15 سنة." },
                new LawArticle { Id = 33, ArticleNumber = 4, Content = "نفقة الزوجة واجبة على زوجها." }
            };
        }

        // ===== 8. مواد قانون الضريبة على الدخل =====
        private static List<LawArticle> GetTaxLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 34, ArticleNumber = 1, Content = "تفرض ضريبة سنوية على صافي دخل الأشخاص الطبيعيين والاعتباريين." },
                new LawArticle { Id = 35, ArticleNumber = 2, Content = "معدل الضريبة 20% على دخل الأشخاص الاعتبارية." },
                new LawArticle { Id = 36, ArticleNumber = 3, Content = "يُعفى من الضريبة حد الإعفاء الشخصي." }
            };
        }

        // ===== 9. مواد قانون البناء =====
        private static List<LawArticle> GetConstructionLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 37, ArticleNumber = 1, Content = "لا يجوز البدء في أعمال البناء إلا بعد الحصول على ترخيص." },
                new LawArticle { Id = 38, ArticleNumber = 2, Content = "يحدد قانون البناء الاشتراطات البنائية." },
                new LawArticle { Id = 39, ArticleNumber = 3, Content = "المخالفات البنائية تخضع للعقوبات." }
            };
        }

        // ===== 10. مواد قانون الإثبات ===== (NEW)
        private static List<LawArticle> GetEvidenceLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 40, ArticleNumber = 1, Content = "على الدائن إثبات الالتزام، وعلى المدين إثبات التخلص منه." },
                new LawArticle { Id = 41, ArticleNumber = 2, Content = "الأصل في الدفوع الإجرائية أنها جوازية ما لم ينص القانون على وجوبها." },
                new LawArticle { Id = 42, ArticleNumber = 3, Content = "الكتابة الرسمية حجة بما دون فيها ما لم يطعن فيها بالتزوير." },
                new LawArticle { Id = 43, ArticleNumber = 4, Content = "القرائن القانونية تعفي من يتمسك بها من أي إثبات آخر." }
            };
        }

        // ===== 11. مواد قانون التجارة ===== (NEW)
        private static List<LawArticle> GetCommercialLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 44, ArticleNumber = 1, Content = "العمل التجاري هو كل عمل يتعلق بالتداول والوساطة بهدف الربح." },
                new LawArticle { Id = 45, ArticleNumber = 2, Content = "يعد تاجراً كل شخص طبيعي أو اعتباري يزاول عملاً تجارياً." },
                new LawArticle { Id = 46, ArticleNumber = 3, Content = "يلتزم التاجر بمسك الدفاتر التجارية المنتظمة." },
                new LawArticle { Id = 47, ArticleNumber = 4, Content = "الأوراق التجارية (الشيك، الكمبيالة، السند لأمر) تخضع لأحكام هذا القانون." }
            };
        }

        // ===== 12. مواد قانون البنك المركزي ===== (NEW)
        private static List<LawArticle> GetBankingLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 48, ArticleNumber = 1, Content = "يهدف البنك المركزي إلى تحقيق استقرار الأسعار وسلامة النظام المصرفي." },
                new LawArticle { Id = 49, ArticleNumber = 2, Content = "البنك المركزي هو بنك الحكومة ومستشارها المالي." },
                new LawArticle { Id = 50, ArticleNumber = 3, Content = "يحظر مزاولة النشاط المصرفي إلا بعد الترخيص من البنك المركزي." },
                new LawArticle { Id = 51, ArticleNumber = 4, Content = "يلتزم سرية حسابات العملاء ولا يجوز الإفصاح عنها إلا بحكم قضائي." }
            };
        }

        // ===== 13. مواد قانون الاستثمار ===== (NEW)
        private static List<LawArticle> GetInvestmentLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 52, ArticleNumber = 1, Content = "يهدف القانون إلى تهيئة مناخ الاستثمار وجذب رؤوس الأموال." },
                new LawArticle { Id = 53, ArticleNumber = 2, Content = "تنشأ الهيئة العامة للاستثمار لتولي تنفيذ أحكام هذا القانون." },
                new LawArticle { Id = 54, ArticleNumber = 3, Content = "تمنح الشركات المستثمرة حوافز خاصة حسب المنطقة الجغرافية." },
                new LawArticle { Id = 55, ArticleNumber = 4, Content = "ضمان عدم مصادرة الاستثمارات إلا بحكم قضائي." }
            };
        }

        // ===== 14. مواد قانون الطفل ===== (NEW)
        private static List<LawArticle> GetChildLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 56, ArticleNumber = 1, Content = "يقصد بالطفل كل من لم يبلغ الثامنة عشرة من عمره." },
                new LawArticle { Id = 57, ArticleNumber = 2, Content = "للطفل الحق في الحياة والبقاء والنمو في أسرة." },
                new LawArticle { Id = 58, ArticleNumber = 3, Content = "التعليم حق لجميع الأطفال، والدولة تلتزم بتوفيره." },
                new LawArticle { Id = 59, ArticleNumber = 4, Content = "يحظر تشغيل الأطفال قبل بلوغ سن الإتمام." }
            };
        }

        // ===== 15. مواد قانون تنظيم الجامعات ===== (NEW)
        private static List<LawArticle> GetUniversitiesLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 60, ArticleNumber = 1, Content = "الجامعات مؤسسات علمية ثقافية تهدف لنشر العلم والبحث العلمي." },
                new LawArticle { Id = 61, ArticleNumber = 2, Content = "لكل جامعة شخصية اعتبارية واستقلال مالي وأكاديمي." },
                new LawArticle { Id = 62, ArticleNumber = 3, Content = "يتولى إدارة الجامعة رئيس الجامعة ومجلس الجامعة." },
                new LawArticle { Id = 63, ArticleNumber = 4, Content = "تنقسم الدراسة في الجامعات إلى مرحلتي الليسانس/البكالوريوس والدراسات العليا." }
            };
        }

        // ===== 16. مواد قانون الإجراءات الضريبية ===== (NEW)
        private static List<LawArticle> GetTaxProcedureLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 64, ArticleNumber = 1, Content = "يلتزم كل ممول بتقديم إقرار ضريبي سنوي." },
                new LawArticle { Id = 65, ArticleNumber = 2, Content = "لمصلحة الضرائب حق فحص الإقرارات والتحقق من صحتها." },
                new LawArticle { Id = 66, ArticleNumber = 3, Content = "يتم الطعن على قرارات اللجنة الداخلية أمام لجنة الطعن." },
                new LawArticle { Id = 67, ArticleNumber = 4, Content = "لا تقام الدعوى الجنائية في الجرائم الضريبية إلا بناء على شكوى من المصلحة." }
            };
        }

        // ===== 17. مواد قانون التموين ===== (NEW)
        private static List<LawArticle> GetSupplyLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 68, ArticleNumber = 1, Content = "يهدف القانون إلى تأمين احتياجات المواطنين من السلع الأساسية." },
                new LawArticle { Id = 69, ArticleNumber = 2, Content = "يحظر حجب السلع التموينية عن التداول بقصد رفع الأسعار." },
                new LawArticle { Id = 70, ArticleNumber = 3, Content = "لجهاز حماية المستهلك حق الرقابة على الأسواق." },
                new LawArticle { Id = 71, ArticleNumber = 4, Content = "يعاقب بالحبس والغرامة كل من غش السلع أو غذّلها." }
            };
        }

        // ===== 18. مواد قانون المرافعات ===== (NEW)
        private static List<LawArticle> GetCivilProcedureLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 72, ArticleNumber = 1, Content = "لا تقبل أي دعوى لا تتوفر فيها شروط المصلحة." },
                new LawArticle { Id = 73, ArticleNumber = 2, Content = "تختص المحكمة التي يقع في دائرتها موطن المدعى عليه." },
                new LawArticle { Id = 74, ArticleNumber = 3, Content = "يعلن الخصوم بصحيفة الدعوى قبل الجلسة المحددة." },
                new LawArticle { Id = 75, ArticleNumber = 4, Content = "الأحكام تكون قابلة للطعن فيها بالاستئناف." }
            };
        }

        // ===== 19. مواد قانون التصالح في مخالفات البناء ===== (NEW)
        private static List<LawArticle> GetReconciliationLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 76, ArticleNumber = 1, Content = "يجوز التصالح في مخالفات البناء التي تمت قبل تاريخ العمل بهذا القانون." },
                new LawArticle { Id = 77, ArticleNumber = 2, Content = "يتم تقديم طلب التصالح للجهة الإدارية المختصة." },
                new LawArticle { Id = 78, ArticleNumber = 3, Content = "يتم سداد مقابل التصالح وفقاً للقواعد المحددة." },
                new LawArticle { Id = 79, ArticleNumber = 4, Content = "يترتب على التصالح انقضاء الدعوى الجنائية." }
            };
        }

        // ===== 20. مواد قانون الرياضة ===== (NEW)
        private static List<LawArticle> GetSportsLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 80, ArticleNumber = 1, Content = "تهدف الرياضة إلى تكوين مواطن سوي بدنياً ونفسياً." },
                new LawArticle { Id = 81, ArticleNumber = 2, Content = "للهيئات الرياضية الشخصية الاعتبارية والاستقلال المالي." },
                new LawArticle { Id = 82, ArticleNumber = 3, Content = "تلتزم الأندية بقواعد الحوكمة والشفافية المالية." },
                new LawArticle { Id = 83, ArticleNumber = 4, Content = "تحظر التعصب الرياضي ويعاقب عليه." }
            };
        }

        // ===== 21. مواد قانون الهجرة غير الشرعية ===== (NEW)
        private static List<LawArticle> GetMigrationLawArticles()
        {
            return new List<LawArticle>
            {
                new LawArticle { Id = 84, ArticleNumber = 1, Content = "يهدف القانون إلى مكافحة تهريب المهاجرين وحماية الضحايا." },
                new LawArticle { Id = 85, ArticleNumber = 2, Content = "يعاقب بالسجن المشدد كل من ارتكب جريمة تهريب المهاجرين." },
                new LawArticle { Id = 86, ArticleNumber = 3, Content = "لا تقع المسؤولية الجنائية على ضحايا الهجرة غير الشرعية." },
                new LawArticle { Id = 87, ArticleNumber = 4, Content = "تلتزم الدولة بحماية شهود الإثبات والمبلغين." }
            };
        }

        // ===== التعديلات =====
        private static List<LawAmendment> GetConstitutionAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 1, AmendmentNumber = "تعديل 2019", Title = "التعديلات الدستورية 2019", AmendmentDate = new DateTime(2019, 4, 23), EffectiveDate = new DateTime(2019, 4, 23), Description = "تعديل بعض مواد الدستور المصري", AffectedArticles = new int[] { 102, 103, 104 } }
            };
        }

        private static List<LawAmendment> GetCivilLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 2, AmendmentNumber = "قانون 15 لسنة 2010", Title = "تعديل بعض أحكام القانون المدني", AmendmentDate = new DateTime(2010, 5, 12), EffectiveDate = new DateTime(2010, 6, 1), Description = "تعديل المواد الخاصة بالالتزامات والعقود", AffectedArticles = new int[] { 125, 126, 127 } },
                new LawAmendment { Id = 3, AmendmentNumber = "قانون 8 لسنة 2022", Title = "تعديل بعض أحكام القانون المدني", AmendmentDate = new DateTime(2022, 1, 1), EffectiveDate = new DateTime(2022, 2, 1), Description = "تعديل المواد الخاصة بالتعاقد الإلكتروني", AffectedArticles = new int[] { 95, 96, 97 } }
            };
        }

        private static List<LawAmendment> GetCriminalLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 4, AmendmentNumber = "قانون 95 لسنة 2003", Title = "تعديل بعض أحكام قانون العقوبات", AmendmentDate = new DateTime(2003, 6, 15), EffectiveDate = new DateTime(2003, 7, 1), Description = "تعديل العقوبات في جرائم الرشوة", AffectedArticles = new int[] { 103, 104, 105 } },
                new LawAmendment { Id = 5, AmendmentNumber = "قانون 22 لسنة 2021", Title = "تعديل بعض أحكام قانون العقوبات", AmendmentDate = new DateTime(2021, 5, 15), EffectiveDate = new DateTime(2021, 6, 1), Description = "تعديل العقوبات في جرائم العنف ضد المرأة", AffectedArticles = new int[] { 242, 243, 244 } }
            };
        }

        private static List<LawAmendment> GetCriminalProcedureAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 6, AmendmentNumber = "قانون 145 لسنة 2006", Title = "تعديل بعض أحكام قانون الإجراءات الجنائية", AmendmentDate = new DateTime(2006, 7, 20), EffectiveDate = new DateTime(2006, 8, 1), Description = "تعديل أحكام الحبس الاحتياطي", AffectedArticles = new int[] { 12, 13, 14 } },
                new LawAmendment { Id = 7, AmendmentNumber = "قانون 15 لسنة 2020", Title = "تعديل بعض أحكام قانون الإجراءات الجنائية", AmendmentDate = new DateTime(2020, 8, 10), EffectiveDate = new DateTime(2020, 9, 1), Description = "تعديل أحكام المحاكمات عن بعد", AffectedArticles = new int[] { 85, 86, 87 } }
            };
        }

        private static List<LawAmendment> GetLaborLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 8, AmendmentNumber = "قانون 48 لسنة 2010", Title = "تعديل بعض أحكام قانون العمل", AmendmentDate = new DateTime(2010, 6, 20), EffectiveDate = new DateTime(2010, 7, 1), Description = "تعديل أحكام إنهاء عقد العمل", AffectedArticles = new int[] { 110, 111, 112 } },
                new LawAmendment { Id = 9, AmendmentNumber = "قانون 23 لسنة 2021", Title = "تعديل بعض أحكام قانون العمل", AmendmentDate = new DateTime(2021, 12, 20), EffectiveDate = new DateTime(2022, 1, 1), Description = "تعديل أحكام تشغيل النساء", AffectedArticles = new int[] { 88, 89, 90 } }
            };
        }

        private static List<LawAmendment> GetCompaniesLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 10, AmendmentNumber = "قانون 14 لسنة 2014", Title = "تعديل بعض أحكام قانون الشركات", AmendmentDate = new DateTime(2014, 7, 15), EffectiveDate = new DateTime(2014, 8, 1), Description = "تعديل أحكام حوكمة الشركات", AffectedArticles = new int[] { 45, 46, 47 } },
                new LawAmendment { Id = 11, AmendmentNumber = "قانون 8 لسنة 2022", Title = "تعديل بعض أحكام قانون الشركات", AmendmentDate = new DateTime(2022, 3, 1), EffectiveDate = new DateTime(2022, 4, 1), Description = "تعديل أحكام الشركات ذات المسئولية المحدودة", AffectedArticles = new int[] { 102, 103, 104 } }
            };
        }

        private static List<LawAmendment> GetFamilyLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 12, AmendmentNumber = "قانون 1 لسنة 2000", Title = "قانون تنظيم بعض أوضاع الأحوال الشخصية", AmendmentDate = new DateTime(2000, 1, 29), EffectiveDate = new DateTime(2000, 2, 15), Description = "تنظيم أحكام الخلع والتطليق", AffectedArticles = new int[] { 10, 11, 12 } },
                new LawAmendment { Id = 13, AmendmentNumber = "قانون 5 لسنة 2020", Title = "تعديل بعض أحكام قانون الأحوال الشخصية", AmendmentDate = new DateTime(2020, 2, 5), EffectiveDate = new DateTime(2020, 3, 1), Description = "تعديل أحكام الحضانة والرؤية", AffectedArticles = new int[] { 15, 16, 17 } }
            };
        }

        private static List<LawAmendment> GetTaxLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 14, AmendmentNumber = "قانون 11 لسنة 2013", Title = "تعديل بعض أحكام قانون الضريبة على الدخل", AmendmentDate = new DateTime(2013, 5, 20), EffectiveDate = new DateTime(2013, 6, 1), Description = "تعديل شرائح الضريبة", AffectedArticles = new int[] { 8, 9, 10 } },
                new LawAmendment { Id = 15, AmendmentNumber = "قانون 9 لسنة 2022", Title = "تعديل بعض أحكام قانون الضريبة على الدخل", AmendmentDate = new DateTime(2022, 2, 15), EffectiveDate = new DateTime(2022, 3, 1), Description = "تعديل أحكام الضريبة على الدخل", AffectedArticles = new int[] { 21, 22, 23 } }
            };
        }

        private static List<LawAmendment> GetConstructionLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 16, AmendmentNumber = "قانون 17 لسنة 2019", Title = "قانون التصالح في مخالفات البناء", AmendmentDate = new DateTime(2019, 2, 13), EffectiveDate = new DateTime(2019, 3, 1), Description = "تنظيم التصالح في مخالفات البناء", AffectedArticles = new int[] { 101, 102, 103 } },
                new LawAmendment { Id = 17, AmendmentNumber = "قانون 12 لسنة 2021", Title = "تعديل بعض أحكام قانون البناء", AmendmentDate = new DateTime(2021, 9, 30), EffectiveDate = new DateTime(2021, 10, 15), Description = "تعديل أحكام الاشتراطات البنائية", AffectedArticles = new int[] { 8, 9, 10 } }
            };
        }

        // ===== تعديلات القوانين الجديدة ===== (NEW)
        private static List<LawAmendment> GetEvidenceLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 18, AmendmentNumber = "قانون 15 لسنة 2014", Title = "تعديل بعض أحكام قانون الإثبات", AmendmentDate = new DateTime(2014, 7, 20), EffectiveDate = new DateTime(2014, 8, 10), Description = "تعديل أحكام الإثبات الإلكتروني", AffectedArticles = new int[] { 15, 16 } },
                new LawAmendment { Id = 19, AmendmentNumber = "قانون 10 لسنة 2020", Title = "تعديل بعض أحكام قانون الإثبات", AmendmentDate = new DateTime(2020, 3, 10), EffectiveDate = new DateTime(2020, 4, 1), Description = "تعديل أحكام الكتابة الرسمية", AffectedArticles = new int[] { 3, 4, 5 } }
            };
        }

        private static List<LawAmendment> GetCommercialLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 20, AmendmentNumber = "قانون 12 لسنة 2008", Title = "تعديل بعض أحكام قانون التجارة", AmendmentDate = new DateTime(2008, 6, 10), EffectiveDate = new DateTime(2008, 7, 1), Description = "تعديل أحكام الأوراق التجارية", AffectedArticles = new int[] { 110, 111, 112 } },
                new LawAmendment { Id = 21, AmendmentNumber = "قانون 7 لسنة 2021", Title = "تعديل بعض أحكام قانون التجارة", AmendmentDate = new DateTime(2021, 11, 5), EffectiveDate = new DateTime(2021, 12, 1), Description = "تعديل أحكام التجارة الإلكترونية", AffectedArticles = new int[] { 8, 9, 10 } }
            };
        }

        private static List<LawAmendment> GetInvestmentLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 22, AmendmentNumber = "قانون 10 لسنة 2018", Title = "تعديل بعض أحكام قانون الاستثمار", AmendmentDate = new DateTime(2018, 5, 20), EffectiveDate = new DateTime(2018, 6, 10), Description = "تعديل الحوافز الاستثمارية", AffectedArticles = new int[] { 21, 22, 23 } },
                new LawAmendment { Id = 23, AmendmentNumber = "قانون 15 لسنة 2020", Title = "تعديل بعض أحكام قانون الاستثمار", AmendmentDate = new DateTime(2020, 12, 20), EffectiveDate = new DateTime(2021, 1, 15), Description = "تعديل أحكام إجراءات تأسيس الشركات", AffectedArticles = new int[] { 4, 5, 6 } }
            };
        }

        private static List<LawAmendment> GetChildLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 24, AmendmentNumber = "قانون 126 لسنة 2008", Title = "تعديل بعض أحكام قانون الطفل", AmendmentDate = new DateTime(2008, 6, 15), EffectiveDate = new DateTime(2008, 7, 1), Description = "تعديل سن الطفل وحماية الأطفال المعرضين للخطر", AffectedArticles = new int[] { 1, 2, 3 } },
                new LawAmendment { Id = 25, AmendmentNumber = "قانون 4 لسنة 2021", Title = "تعديل بعض أحكام قانون الطفل", AmendmentDate = new DateTime(2021, 6, 10), EffectiveDate = new DateTime(2021, 7, 1), Description = "تعديل أحكام حماية الأطفال مجهولي النسب", AffectedArticles = new int[] { 15, 16 } }
            };
        }

        private static List<LawAmendment> GetUniversitiesLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 26, AmendmentNumber = "قانون 85 لسنة 2012", Title = "تعديل بعض أحكام قانون تنظيم الجامعات", AmendmentDate = new DateTime(2012, 7, 10), EffectiveDate = new DateTime(2012, 8, 1), Description = "تعديل أحكام مجالس الجامعات", AffectedArticles = new int[] { 25, 26 } },
                new LawAmendment { Id = 27, AmendmentNumber = "قانون 8 لسنة 2019", Title = "تعديل بعض أحكام قانون تنظيم الجامعات", AmendmentDate = new DateTime(2019, 9, 15), EffectiveDate = new DateTime(2019, 10, 1), Description = "تعديل أحكام الدراسة والامتحانات", AffectedArticles = new int[] { 100, 101, 102 } }
            };
        }

        private static List<LawAmendment> GetSupplyLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 28, AmendmentNumber = "قانون 111 لسنة 1980", Title = "تعديل بعض أحكام قانون التموين", AmendmentDate = new DateTime(1980, 8, 5), EffectiveDate = new DateTime(1980, 9, 1), Description = "تعديل العقوبات في جرائم التموين", AffectedArticles = new int[] { 20, 21, 22 } },
                new LawAmendment { Id = 29, AmendmentNumber = "قانون 10 لسنة 2020", Title = "تعديل بعض أحكام قانون التموين", AmendmentDate = new DateTime(2020, 1, 30), EffectiveDate = new DateTime(2020, 2, 15), Description = "تعديل أحكام الرقابة على الأسواق", AffectedArticles = new int[] { 8, 9 } }
            };
        }

        private static List<LawAmendment> GetCivilProcedureLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 30, AmendmentNumber = "قانون 78 لسنة 1998", Title = "تعديل بعض أحكام قانون المرافعات", AmendmentDate = new DateTime(1998, 6, 15), EffectiveDate = new DateTime(1998, 7, 1), Description = "تعديل أحكام المواعيد والإعلان", AffectedArticles = new int[] { 5, 6, 7 } },
                new LawAmendment { Id = 31, AmendmentNumber = "قانون 5 لسنة 2021", Title = "تعديل بعض أحكام قانون المرافعات", AmendmentDate = new DateTime(2021, 4, 20), EffectiveDate = new DateTime(2021, 5, 10), Description = "تعديل أحكام الطعن في الأحكام", AffectedArticles = new int[] { 210, 211, 212 } }
            };
        }

        private static List<LawAmendment> GetReconciliationLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 32, AmendmentNumber = "قانون 1 لسنة 2020", Title = "تعديل بعض أحكام قانون التصالح", AmendmentDate = new DateTime(2020, 1, 15), EffectiveDate = new DateTime(2020, 2, 1), Description = "مدد التصالح في مخالفات البناء", AffectedArticles = new int[] { 1, 2 } },
                new LawAmendment { Id = 33, AmendmentNumber = "قانون 12 لسنة 2021", Title = "تعديل بعض أحكام قانون التصالح", AmendmentDate = new DateTime(2021, 10, 10), EffectiveDate = new DateTime(2021, 11, 1), Description = "تعديل أحكام سعر التصالح", AffectedArticles = new int[] { 3, 4 } }
            };
        }

        private static List<LawAmendment> GetSportsLawAmendments()
        {
            return new List<LawAmendment>
            {
                new LawAmendment { Id = 34, AmendmentNumber = "قانون 5 لسنة 2018", Title = "تعديل بعض أحكام قانون الرياضة", AmendmentDate = new DateTime(2018, 7, 15), EffectiveDate = new DateTime(2018, 8, 1), Description = "تعديل أحكام الانتخابات الرياضية", AffectedArticles = new int[] { 12, 13 } },
                new LawAmendment { Id = 35, AmendmentNumber = "قانون 10 لسنة 2021", Title = "تعديل بعض أحكام قانون الرياضة", AmendmentDate = new DateTime(2021, 3, 20), EffectiveDate = new DateTime(2021, 4, 10), Description = "تعديل أحكام الرقابة المالية للأندية", AffectedArticles = new int[] { 25, 26 } }
            };
        }

        // ===== الكلمات المفتاحية (بدون Id) =====
        private static List<LawKeyword> GetEvidenceLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "إثبات", Weight = 10 },
                new LawKeyword { Keyword = "قرائن", Weight = 8 },
                new LawKeyword { Keyword = "كتابة رسمية", Weight = 9 }
            };
        }

        private static List<LawKeyword> GetCommercialLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "تجارة", Weight = 10 },
                new LawKeyword { Keyword = "تاجر", Weight = 9 },
                new LawKeyword { Keyword = "أوراق تجارية", Weight = 9 },
                new LawKeyword { Keyword = "كمبيالة", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetBankingLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "بنك", Weight = 10 },
                new LawKeyword { Keyword = "مركزي", Weight = 9 },
                new LawKeyword { Keyword = "مصرفي", Weight = 9 },
                new LawKeyword { Keyword = "ائتمان", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetInvestmentLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "استثمار", Weight = 10 },
                new LawKeyword { Keyword = "حوافز", Weight = 8 },
                new LawKeyword { Keyword = "منطقة حرة", Weight = 7 }
            };
        }

        private static List<LawKeyword> GetChildLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "طفل", Weight = 10 },
                new LawKeyword { Keyword = "رعاية", Weight = 9 },
                new LawKeyword { Keyword = "حضانة", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetUniversitiesLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "جامعة", Weight = 10 },
                new LawKeyword { Keyword = "تعليم عالي", Weight = 9 },
                new LawKeyword { Keyword = "كلية", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetTaxProcedureLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "إجراءات ضريبية", Weight = 10 },
                new LawKeyword { Keyword = "إقرار ضريبي", Weight = 9 },
                new LawKeyword { Keyword = "فحص", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetSupplyLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "تموين", Weight = 10 },
                new LawKeyword { Keyword = "سلع", Weight = 9 },
                new LawKeyword { Keyword = "دعم", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetCivilProcedureLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "مرافعات", Weight = 10 },
                new LawKeyword { Keyword = "دعوى", Weight = 9 },
                new LawKeyword { Keyword = "حكم", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetReconciliationLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "تصالح", Weight = 10 },
                new LawKeyword { Keyword = "مخالفات بناء", Weight = 9 },
                new LawKeyword { Keyword = "تقنين أوضاع", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetSportsLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "رياضة", Weight = 10 },
                new LawKeyword { Keyword = "نادي", Weight = 9 },
                new LawKeyword { Keyword = "اتحاد رياضي", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetMigrationLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "هجرة غير شرعية", Weight = 10 },
                new LawKeyword { Keyword = "تهريب", Weight = 9 },
                new LawKeyword { Keyword = "مهاجرين", Weight = 8 }
            };
        }

        // ===== الكلمات المفتاحية للقوانين الموجودة (بدون Id) =====
        private static List<LawKeyword> GetConstitutionKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "دستور", Weight = 10 },
                new LawKeyword { Keyword = "حقوق", Weight = 9 },
                new LawKeyword { Keyword = "حريات", Weight = 9 }
            };
        }

        private static List<LawKeyword> GetCivilLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "مدني", Weight = 10 },
                new LawKeyword { Keyword = "عقد", Weight = 9 },
                new LawKeyword { Keyword = "التزام", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetCriminalLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "جريمة", Weight = 10 },
                new LawKeyword { Keyword = "عقوبة", Weight = 9 },
                new LawKeyword { Keyword = "سرقة", Weight = 7 }
            };
        }

        private static List<LawKeyword> GetCriminalProcedureKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "إجراءات", Weight = 10 },
                new LawKeyword { Keyword = "محاكمة", Weight = 9 },
                new LawKeyword { Keyword = "نيابة عامة", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetLaborLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "عمل", Weight = 10 },
                new LawKeyword { Keyword = "عامل", Weight = 9 },
                new LawKeyword { Keyword = "أجر", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetCompaniesLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "شركة", Weight = 10 },
                new LawKeyword { Keyword = "مساهمة", Weight = 9 },
                new LawKeyword { Keyword = "تأسيس", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetFamilyLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "زواج", Weight = 10 },
                new LawKeyword { Keyword = "طلاق", Weight = 9 },
                new LawKeyword { Keyword = "نفقة", Weight = 9 }
            };
        }

        private static List<LawKeyword> GetTaxLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "ضريبة", Weight = 10 },
                new LawKeyword { Keyword = "دخل", Weight = 9 },
                new LawKeyword { Keyword = "إقرار", Weight = 8 }
            };
        }

        private static List<LawKeyword> GetConstructionLawKeywords()
        {
            return new List<LawKeyword> {
                new LawKeyword { Keyword = "بناء", Weight = 10 },
                new LawKeyword { Keyword = "ترخيص", Weight = 9 },
                new LawKeyword { Keyword = "مخالفة", Weight = 8 }
            };
        }
    }
}