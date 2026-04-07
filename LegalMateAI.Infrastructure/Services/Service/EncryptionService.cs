using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using LegalMateAI.Infrastructure.Services.IService;
using System.IO;

namespace LegalMateAI.Infrastructure.Services.Service
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration config)
        {
            string keyString = config["Security:AES:Key"]
                ?? throw new ArgumentNullException("AES Key missing from configuration");

            string ivString = config["Security:AES:IV"]
                ?? throw new ArgumentNullException("AES IV missing from configuration");

            // تأمين طول المفتاح والـ IV بشكل ثابت
            // _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
            // _iv = Encoding.UTF8.GetBytes(ivString.PadRight(16).Substring(0, 16));

            _key = Convert.FromBase64String(keyString);
            _iv = Convert.FromBase64String(ivString);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC; // تحديد النمط لضمان التوافق
            aes.Padding = PaddingMode.PKCS7; // تحديد الحشو

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // استخدام CryptoStream مع StreamWriter لضمان كتابة النص بشكل صحيح
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                // تنظيف النص من أي مسافات زائدة قد تأتي من قاعدة البيانات (خاصة لو العمود nchar)
                var buffer = Convert.FromBase64String(cipherText.Trim());

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                // طباعة الخطأ في الـ Console لمعرفة سبب الفشل (مثلاً Key mismatch)
                Console.WriteLine($"[EncryptionService] Decryption failed: {ex.Message}");

                // إرجاع النص كما هو في حالة الفشل (قد يكون نصاً عادياً غير مشفر)
                return cipherText;
            }
        }

        public string? SafeDecrypt(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Decrypt(value);
        }
    }
}