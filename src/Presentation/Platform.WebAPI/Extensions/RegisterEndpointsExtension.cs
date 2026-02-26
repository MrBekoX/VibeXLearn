using Platform.WebAPI.Endpoints;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// Extension to register all Minimal API endpoints.
/// </summary>
public static class RegisterEndpointsExtension
{
    public static IEndpointRouteBuilder RegisterAllEndpoints(this IEndpointRouteBuilder app)
    {
        app.RegisterAuthEndpoints();
        app.RegisterCourseEndpoints();
        app.RegisterCategoryEndpoints();
        app.RegisterLessonEndpoints();
        app.RegisterLiveSessionEndpoints();
        app.RegisterEnrollmentEndpoints();
        app.RegisterOrderEndpoints();
        app.RegisterPaymentEndpoints();
        app.RegisterCouponEndpoints();
        app.RegisterBadgeEndpoints();
        app.RegisterCertificateEndpoints();
        app.RegisterSubmissionEndpoints();
        app.RegisterHealthEndpoints();

        return app;
    }
}
