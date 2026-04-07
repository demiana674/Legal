// LegalMateAI.Infrastructure.Services.IService/IEncryptionService.cs
namespace LegalMateAI.Infrastructure.Services.IService
{
    /// <summary>
    /// واجهة لتشفير وفك تشفير البيانات الحساسة
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// تشفير نص عادي
        /// </summary>
        /// <param name="plainText">النص المراد تشفيره</param>
        /// <returns>النص المشفر بصيغة Base64</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// فك تشفير نص مشفر
        /// </summary>
        /// <param name="cipherText">النص المشفر بصيغة Base64</param>
        /// <returns>النص الأصلي بعد فك التشفير</returns>
        string Decrypt(string cipherText);
    }
}