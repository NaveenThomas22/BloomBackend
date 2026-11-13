using Bloom.BLL.Interfaces;
using Bloom.BLL.Services;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bloom.BLL.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Business Logic Layer services and validators into the service collection.
    /// </summary>
    public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Security primitives
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        // Application services
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISyllabusService, SyllabusService>();
        services.AddScoped<IActivityCategoryService, ActivityCategoryService>();
        services.AddScoped<ITimeAccountingService, TimeAccountingService>();
        services.AddScoped<IActivityRecordService, ActivityRecordService>();
        services.AddScoped<IStudyTimerService, StudyTimerService>();
        services.AddScoped<IPlannedActivityService, PlannedActivityService>();
        services.AddScoped<IActivityReportService, ActivityReportService>();
        services.AddScoped<IRevisionPolicyService, RevisionPolicyService>();
        services.AddScoped<IGoalProgressService, GoalProgressService>();
        services.AddScoped<IRevisionScheduleService, RevisionScheduleService>();
        services.AddScoped<IUserGoalService, UserGoalService>();
        services.AddScoped<IPreparationMilestoneService, PreparationMilestoneService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<IQuestionService>(sp => sp.GetRequiredService<QuestionService>());
        services.AddScoped<IQuestionPassageService>(sp => sp.GetRequiredService<QuestionService>());
        services.AddScoped<IQuestionSetService>(sp => sp.GetRequiredService<QuestionService>());
        services.AddScoped<IBookmarkService>(sp => sp.GetRequiredService<QuestionService>());
        services.AddScoped<IPracticeSessionService, PracticeSessionService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IReflectionService, ReflectionService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IPersonalMilestoneService, PersonalMilestoneService>();
        services.AddScoped<IWebPushService, WebPushService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddHttpClient("WebPush");

        // Register FluentValidation validators from assembly
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }
}
