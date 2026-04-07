using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LegalMateAI.DAL.DBContext
{
    public class LegalMateDbContextFactory : IDesignTimeDbContextFactory<LegalMateDbContext>
    {
        public LegalMateDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LegalMateDbContext>();

            // ضع هنا نفس connection string اللي في API
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Legal;Trusted_Connection=True;TrustServerCertificate=True");

            return new LegalMateDbContext(optionsBuilder.Options);
        }
    }
}



