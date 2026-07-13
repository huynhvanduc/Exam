using Exam.API.Authorization;
using Exam.API.Middleware;
using Exam.API.Services;
using Exam.Application;
using Exam.Application.Common;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Infrastructure;
using Exam.Infrastructure.Persistence.Mongo;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.Audience = builder.Configuration["IdentityServer:Audience"];
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(50);
}).AddInMemoryStorage();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await MongoIndexInitializer.EnsureIndexesAsync(scope.ServiceProvider.GetRequiredService<MongoDbContext>());
    await RolePermissionSeeder.EnsureDefaultsAsync(scope.ServiceProvider.GetRequiredService<IRolePermissionRepository>());

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.EnsureSampleDataAsync(
            scope.ServiceProvider.GetRequiredService<ICategoryRepository>(),
            scope.ServiceProvider.GetRequiredService<IQuestionRepository>(),
            scope.ServiceProvider.GetRequiredService<IExamRepository>(),
            scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            scope.ServiceProvider.GetRequiredService<IAuditLogRepository>(),
            scope.ServiceProvider.GetRequiredService<IClassRoomRepository>());
    }
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<UserProvisioningMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecksUI(options => options.UIPath = "/healthchecks-ui").AllowAnonymous();

app.Run();
