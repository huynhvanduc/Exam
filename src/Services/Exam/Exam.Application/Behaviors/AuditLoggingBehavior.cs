using System.Text.Json;
using Exam.Application.Common;
using Exam.Domain.AggregateModels.AuditAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Exam.Application.Behaviors;

// Tự động ghi audit log cho MỌI command (namespace chứa ".Commands.") sau khi handler chạy
// thành công, thay vì phải tự inject IAuditLogRepository và gọi InsertAsync trong từng handler.
// Action được suy ra từ tên aggregate + tên command (vd "Category.Create"); Description là JSON
// serialize của command; TargetId ưu tiên lấy từ property "Id"/"*Id" của response (case tạo mới),
// nếu không có thì lấy từ request (case update/delete đã có sẵn Id).
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

    public AuditLoggingBehavior(
        IAuditLogRepository auditLogRepository,
        ICurrentUserAccessor currentUser,
        ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
    {
        _auditLogRepository = auditLogRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var isCommand = requestType.Namespace?.Contains(".Commands.") == true;

        if (!isCommand || request is ISkipAutoAuditLog)
            return await next();

        var response = await next();

        try
        {
            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
                return response;

            var auditEntry = new AuditLogEntry(
                actorUserId,
                BuildAction(requestType),
                TryGetId(response) ?? TryGetId(request) ?? string.Empty,
                JsonSerializer.Serialize(request, requestType, SerializerOptions));

            await _auditLogRepository.InsertAsync(auditEntry, cancellationToken);
        }
        catch (Exception ex)
        {
            // Ghi audit log thất bại không được phép làm hỏng thao tác nghiệp vụ chính.
            _logger.LogWarning(ex, "Failed to write audit log for {RequestType}", requestType.Name);
        }

        return response;
    }

    private static string BuildAction(Type requestType)
    {
        var aggregate = requestType.Namespace?
            .Split('.')
            .FirstOrDefault(segment => segment.EndsWith("Aggregate", StringComparison.Ordinal))?
            .Replace("Aggregate", string.Empty) ?? "Unknown";

        var verb = requestType.Name.EndsWith("Command", StringComparison.Ordinal)
            ? requestType.Name[..^"Command".Length]
            : requestType.Name;

        if (verb.Length > aggregate.Length && verb.EndsWith(aggregate, StringComparison.Ordinal))
            verb = verb[..^aggregate.Length];

        return $"{aggregate}.{verb}";
    }

    private static string? TryGetId(object? source)
    {
        if (source is null)
            return null;

        var type = source.GetType();
        var idProperty = type.GetProperty("Id")
            ?? type.GetProperties().FirstOrDefault(p => p.PropertyType == typeof(string) && p.Name.EndsWith("Id", StringComparison.Ordinal));

        return idProperty?.GetValue(source) as string;
    }
}
