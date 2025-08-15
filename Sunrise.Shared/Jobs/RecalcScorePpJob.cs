using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sunrise.Shared.Database;
using Sunrise.Shared.Objects.Sessions;
using Sunrise.Shared.Services;
using Sunrise.Shared.Application;

namespace Sunrise.Shared.Jobs;

public static class RecalcScorePpJob
{
    // Recalculate PP by score hash (idempotent)
    public static async Task ProcessByScoreHash(string scoreHash)
    {
        using var scope = ServicesProviderHolder.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        var calculator = scope.ServiceProvider.GetRequiredService<CalculatorService>();

        var score = await database.Scores.GetScore(scoreHash);
        if (score == null)
            return;

        // Skip if already has PP
        if (score.PerformancePoints > 0)
            return;

        var serverSession = BaseSession.GenerateServerSession();
        var perf = await calculator.CalculateScorePerformance(serverSession, score);
        if (perf.IsFailure)
            return; // Let Hangfire handle retries via its retry policy

        score.PerformancePoints = perf.Value.PerformancePoints;
        await database.Scores.UpdateScore(score);

        // Recalculate stats/grades to reflect new PP
        var user = await database.Users.GetUser(score.UserId);
        if (user == null)
            return;

        var stats = await database.Users.Stats.GetUserStats(score.UserId, score.GameMode);
        if (stats != null)
        {
            await database.Users.Stats.UpdateUserStats(stats, user);
        }

        var grades = await database.Users.Grades.GetUserGrades(score.UserId, score.GameMode);
        if (grades != null)
        {
            await database.Users.Grades.UpdateUserGrades(grades);
        }
    }
}


