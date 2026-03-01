using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Auth.Rules;
using Platform.Application.Features.Badges.Rules;
using Platform.Application.Features.Categories.Rules;
using Platform.Application.Features.Certificates.Rules;
using Platform.Application.Features.Coupons.Rules;
using Platform.Application.Features.Courses.Rules;
using Platform.Application.Features.Enrollments.Rules;
using Platform.Application.Features.Lessons.Rules;
using Platform.Application.Features.LiveSessions.Rules;
using Platform.Application.Features.Orders.Rules;
using Platform.Application.Features.Payments.Rules;
using Platform.Application.Features.Submissions.Rules;

namespace Platform.Application.Extensions;

/// <summary>
/// Application katmanı DI extension'ları.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Business Rules
        services.AddScoped<IAuthBusinessRules, AuthBusinessRules>();
        services.AddScoped<IBadgeBusinessRules, BadgeBusinessRules>();
        services.AddScoped<ICategoryBusinessRules, CategoryBusinessRules>();
        services.AddScoped<ICertificateBusinessRules, CertificateBusinessRules>();
        services.AddScoped<ICouponBusinessRules, CouponBusinessRules>();
        services.AddScoped<ICourseBusinessRules, CourseBusinessRules>();
        services.AddScoped<IEnrollmentBusinessRules, EnrollmentBusinessRules>();
        services.AddScoped<ILessonBusinessRules, LessonBusinessRules>();
        services.AddScoped<ILiveSessionBusinessRules, LiveSessionBusinessRules>();
        services.AddScoped<IOrderBusinessRules, OrderBusinessRules>();
        services.AddScoped<IPaymentBusinessRules, PaymentBusinessRules>();
        services.AddScoped<ISubmissionBusinessRules, SubmissionBusinessRules>();

        return services;
    }
}
