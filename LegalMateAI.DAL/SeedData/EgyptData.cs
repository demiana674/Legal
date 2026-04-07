using LegalMateAI.Domain.Entities;
public static class EgyptData
{
    public static List<Governorate> GetGovernoratesWithCities()
    {
        return new List<Governorate>
        {
            // القاهرة
            new Governorate 
            { 
                Id = 1, 
                Name = "القاهرة",
                Cities = new List<City>
                {
                    new City { Id = 101, Name = "مدينة نصر", GovernorateId = 1 },
                    new City { Id = 102, Name = "مصر الجديدة", GovernorateId = 1 },
                    new City { Id = 103, Name = "المعادي", GovernorateId = 1 },
                    new City { Id = 104, Name = "المقطم", GovernorateId = 1 },
                    new City { Id = 105, Name = "الزمالك", GovernorateId = 1 },
                    new City { Id = 106, Name = "وسط البلد", GovernorateId = 1 },
                    new City { Id = 107, Name = "شبرا", GovernorateId = 1 },
                    new City { Id = 108, Name = "العباسية", GovernorateId = 1 },
                    new City { Id = 109, Name = "المهندسين", GovernorateId = 1 },
                    new City { Id = 110, Name = "الدقي", GovernorateId = 1 },
                    new City { Id = 111, Name = "حلوان", GovernorateId = 1 },
                    new City { Id = 112, Name = "التجمع الخامس", GovernorateId = 1 },
                    new City { Id = 113, Name = "الشروق", GovernorateId = 1 },
                    new City { Id = 114, Name = "الرحاب", GovernorateId = 1 }
                }
            },
            
            // الجيزة
            new Governorate 
            { 
                Id = 2, 
                Name = "الجيزة",
                Cities = new List<City>
                {
                    new City { Id = 201, Name = "الدقي", GovernorateId = 2 },
                    new City { Id = 202, Name = "المهندسين", GovernorateId = 2 },
                    new City { Id = 203, Name = "العجوزة", GovernorateId = 2 },
                    new City { Id = 204, Name = "الهرم", GovernorateId = 2 },
                    new City { Id = 205, Name = "فيصل", GovernorateId = 2 },
                    new City { Id = 206, Name = "أكتوبر", GovernorateId = 2 },
                    new City { Id = 207, Name = "الشيخ زايد", GovernorateId = 2 },
                    new City { Id = 208, Name = "حدائق الأهرام", GovernorateId = 2 },
                    new City { Id = 209, Name = "إمبابة", GovernorateId = 2 },
                    new City { Id = 210, Name = "بولاق الدكرور", GovernorateId = 2 }
                }
            },
            
            // الإسكندرية
            new Governorate 
            { 
                Id = 3, 
                Name = "الإسكندرية",
                Cities = new List<City>
                {
                    new City { Id = 301, Name = "سموحة", GovernorateId = 3 },
                    new City { Id = 302, Name = "محطة الرمل", GovernorateId = 3 },
                    new City { Id = 303, Name = "سيوف", GovernorateId = 3 },
                    new City { Id = 304, Name = "كامب شيزار", GovernorateId = 3 },
                    new City { Id = 305, Name = "لوران", GovernorateId = 3 },
                    new City { Id = 306, Name = "فيكتوريا", GovernorateId = 3 },
                    new City { Id = 307, Name = "سبورتنج", GovernorateId = 3 },
                    new City { Id = 308, Name = "العصافرة", GovernorateId = 3 },
                    new City { Id = 309, Name = "المندرة", GovernorateId = 3 },
                    new City { Id = 310, Name = "أبو قير", GovernorateId = 3 },
                    new City { Id = 311, Name = "برج العرب", GovernorateId = 3 }
                }
            },
            
            // الدقهلية
            new Governorate 
            { 
                Id = 4, 
                Name = "الدقهلية",
                Cities = new List<City>
                {
                    new City { Id = 401, Name = "المنصورة", GovernorateId = 4 },
                    new City { Id = 402, Name = "طلخا", GovernorateId = 4 },
                    new City { Id = 403, Name = "ميت غمر", GovernorateId = 4 },
                    new City { Id = 404, Name = "دكرنس", GovernorateId = 4 },
                    new City { Id = 405, Name = "السنبلاوين", GovernorateId = 4 },
                    new City { Id = 406, Name = "نبروه", GovernorateId = 4 },
                    new City { Id = 407, Name = "تمي الأمديد", GovernorateId = 4 }
                }
            },
            
            // الشرقية
            new Governorate 
            { 
                Id = 5, 
                Name = "الشرقية",
                Cities = new List<City>
                {
                    new City { Id = 501, Name = "الزقازيق", GovernorateId = 5 },
                    new City { Id = 502, Name = "العاشر من رمضان", GovernorateId = 5 },
                    new City { Id = 503, Name = "بلبيس", GovernorateId = 5 },
                    new City { Id = 504, Name = "منيا القمح", GovernorateId = 5 },
                    new City { Id = 505, Name = "أبو حماد", GovernorateId = 5 },
                    new City { Id = 506, Name = "ههيا", GovernorateId = 5 },
                    new City { Id = 507, Name = "فاقوس", GovernorateId = 5 }
                }
            },
            
            // القليوبية
            new Governorate 
            { 
                Id = 6, 
                Name = "القليوبية",
                Cities = new List<City>
                {
                    new City { Id = 601, Name = "بنها", GovernorateId = 6 },
                    new City { Id = 602, Name = "شبرا الخيمة", GovernorateId = 6 },
                    new City { Id = 603, Name = "قليوب", GovernorateId = 6 },
                    new City { Id = 604, Name = "الخانكة", GovernorateId = 6 },
                    new City { Id = 605, Name = "طوخ", GovernorateId = 6 },
                    new City { Id = 606, Name = "القناطر الخيرية", GovernorateId = 6 }
                }
            },
            
            // المنوفية
            new Governorate 
            { 
                Id = 7, 
                Name = "المنوفية",
                Cities = new List<City>
                {
                    new City { Id = 701, Name = "شبين الكوم", GovernorateId = 7 },
                    new City { Id = 702, Name = "منوف", GovernorateId = 7 },
                    new City { Id = 703, Name = "الباجور", GovernorateId = 7 },
                    new City { Id = 704, Name = "أشمون", GovernorateId = 7 },
                    new City { Id = 705, Name = "تلا", GovernorateId = 7 }
                }
            },
            
            // الغربية
            new Governorate 
            { 
                Id = 8, 
                Name = "الغربية",
                Cities = new List<City>
                {
                    new City { Id = 801, Name = "طنطا", GovernorateId = 8 },
                    new City { Id = 802, Name = "المحلة الكبرى", GovernorateId = 8 },
                    new City { Id = 803, Name = "كفر الزيات", GovernorateId = 8 },
                    new City { Id = 804, Name = "زفتى", GovernorateId = 8 },
                    new City { Id = 805, Name = "السنطة", GovernorateId = 8 }
                }
            },
            
            // كفر الشيخ
            new Governorate 
            { 
                Id = 9, 
                Name = "كفر الشيخ",
                Cities = new List<City>
                {
                    new City { Id = 901, Name = "كفر الشيخ", GovernorateId = 9 },
                    new City { Id = 902, Name = "دسوق", GovernorateId = 9 },
                    new City { Id = 903, Name = "فوة", GovernorateId = 9 },
                    new City { Id = 904, Name = "مطوبس", GovernorateId = 9 },
                    new City { Id = 905, Name = "بيلا", GovernorateId = 9 }
                }
            },
            
            // البحيرة
            new Governorate 
            { 
                Id = 10, 
                Name = "البحيرة",
                Cities = new List<City>
                {
                    new City { Id = 1001, Name = "دمنهور", GovernorateId = 10 },
                    new City { Id = 1002, Name = "كفر الدوار", GovernorateId = 10 },
                    new City { Id = 1003, Name = "رشيد", GovernorateId = 10 },
                    new City { Id = 1004, Name = "إدكو", GovernorateId = 10 },
                    new City { Id = 1005, Name = "أبو المطامير", GovernorateId = 10 }
                }
            },
            
            // الإسماعيلية
            new Governorate 
            { 
                Id = 11, 
                Name = "الإسماعيلية",
                Cities = new List<City>
                {
                    new City { Id = 1101, Name = "الإسماعيلية", GovernorateId = 11 },
                    new City { Id = 1102, Name = "فايد", GovernorateId = 11 },
                    new City { Id = 1103, Name = "القنطرة شرق", GovernorateId = 11 },
                    new City { Id = 1104, Name = "القنطرة غرب", GovernorateId = 11 }
                }
            },
            
            // بورسعيد
            new Governorate 
            { 
                Id = 12, 
                Name = "بورسعيد",
                Cities = new List<City>
                {
                    new City { Id = 1201, Name = "بورسعيد", GovernorateId = 12 },
                    new City { Id = 1202, Name = "بورفؤاد", GovernorateId = 12 }
                }
            },
            
            // السويس
            new Governorate 
            { 
                Id = 13, 
                Name = "السويس",
                Cities = new List<City>
                {
                    new City { Id = 1301, Name = "السويس", GovernorateId = 13 },
                    new City { Id = 1302, Name = "عتاقة", GovernorateId = 13 }
                }
            },
            
            // باقي المحافظات بشكل مختصر...
            new Governorate { Id = 14, Name = "دمياط", Cities = new List<City> { new City { Id = 1401, Name = "دمياط", GovernorateId = 14 } } },
            new Governorate { Id = 15, Name = "بني سويف", Cities = new List<City> { new City { Id = 1501, Name = "بني سويف", GovernorateId = 15 } } },
            new Governorate { Id = 16, Name = "الفيوم", Cities = new List<City> { new City { Id = 1601, Name = "الفيوم", GovernorateId = 16 } } },
            new Governorate { Id = 17, Name = "المنيا", Cities = new List<City> { new City { Id = 1701, Name = "المنيا", GovernorateId = 17 } } },
            new Governorate { Id = 18, Name = "أسيوط", Cities = new List<City> { new City { Id = 1801, Name = "أسيوط", GovernorateId = 18 } } },
            new Governorate { Id = 19, Name = "سوهاج", Cities = new List<City> { new City { Id = 1901, Name = "سوهاج", GovernorateId = 19 } } },
            new Governorate { Id = 20, Name = "قنا", Cities = new List<City> { new City { Id = 2001, Name = "قنا", GovernorateId = 20 } } },
            new Governorate { Id = 21, Name = "الأقصر", Cities = new List<City> { new City { Id = 2101, Name = "الأقصر", GovernorateId = 21 } } },
            new Governorate { Id = 22, Name = "أسوان", Cities = new List<City> { new City { Id = 2201, Name = "أسوان", GovernorateId = 22 } } },
            new Governorate { Id = 23, Name = "البحر الأحمر", Cities = new List<City> { new City { Id = 2301, Name = "الغردقة", GovernorateId = 23 } } },
            new Governorate { Id = 24, Name = "الوادي الجديد", Cities = new List<City> { new City { Id = 2401, Name = "الخارجة", GovernorateId = 24 } } },
            new Governorate { Id = 25, Name = "مطروح", Cities = new List<City> { new City { Id = 2501, Name = "مرسى مطروح", GovernorateId = 25 } } },
            new Governorate { Id = 26, Name = "شمال سيناء", Cities = new List<City> { new City { Id = 2601, Name = "العريش", GovernorateId = 26 } } },
            new Governorate { Id = 27, Name = "جنوب سيناء", Cities = new List<City> { new City { Id = 2701, Name = "الطور", GovernorateId = 27 } } }
        };
    }
}