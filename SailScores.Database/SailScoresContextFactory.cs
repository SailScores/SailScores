using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SailScores.Database;

// Allows `dotnet ef` to create the context without the web startup project.
// Used only at design time (migrations); never invoked at runtime.
public class SailScoresContextFactory : IDesignTimeDbContextFactory<SailScoresContext>
{
    public SailScoresContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SailScoresContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=sailscores;User Id=sa;password=P@ssw0rd;" +
            "MultipleActiveResultSets=true;TrustServerCertificate=true");
        return new SailScoresContext(optionsBuilder.Options);
    }
}
