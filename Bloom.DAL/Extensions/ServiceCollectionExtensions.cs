using Bloom.Core.Constants;
using Bloom.Core.Interfaces;
using Bloom.DAL.Connections;
using Bloom.DAL.Database;
using Bloom.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bloom.DAL.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Data Access Layer services and repositories into the service collection.
    /// </summary>
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind DatabaseOptions
        services.Configure<DatabaseOptions>(options =>
        {
            var section = configuration.GetSection(DatabaseOptions.SectionName);
            section.Bind(options);

            var connStr = configuration.GetConnectionString(DatabaseConstants.DefaultConnectionStringName);
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                options.BloomConnection = connStr;
            }
        });

        // Register Connection Factory as Singleton
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // Register Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISyllabusRepository, SyllabusRepository>();
        services.AddScoped<ISyllabusProgressRepository, SyllabusProgressRepository>();
        services.AddScoped<IActivityCategoryRepository, ActivityCategoryRepository>();
        services.AddScoped<IActivityRecordRepository, ActivityRecordRepository>();
        services.AddScoped<IStudySessionDetailRepository, StudySessionDetailRepository>();
        services.AddScoped<IPlannedActivityRepository, PlannedActivityRepository>();
        services.AddScoped<IUserTimerStateRepository, UserTimerStateRepository>();
        services.AddScoped<IUserActivityLockRepository, UserActivityLockRepository>();
        services.AddScoped<IRevisionPolicyRepository, RevisionPolicyRepository>();
        services.AddScoped<IRevisionScheduleRepository, RevisionScheduleRepository>();
        services.AddScoped<IRevisionAttemptRepository, RevisionAttemptRepository>();
        services.AddScoped<IUserGoalRepository, UserGoalRepository>();
        services.AddScoped<IPreparationMilestoneRepository, PreparationMilestoneRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionPassageRepository, QuestionPassageRepository>();
        services.AddScoped<IQuestionSetRepository, QuestionSetRepository>();
        services.AddScoped<IUserQuestionBookmarkRepository, UserQuestionBookmarkRepository>();
        services.AddScoped<IPracticeSessionRepository, PracticeSessionRepository>();
        services.AddScoped<IQuestionAttemptRepository, QuestionAttemptRepository>();
        services.AddScoped<IExamTemplateRepository, ExamTemplateRepository>();
        services.AddScoped<IExamAttemptRepository, ExamAttemptRepository>();
        services.AddScoped<IJournalCategoryRepository, JournalCategoryRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IReflectionRepository, ReflectionRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IPersonalMilestoneRepository, PersonalMilestoneRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Register Migrator
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        // Register Database Health Check
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("mysql_database", tags: new[] { "db", "mysql" });

        return services;
    }
}
