namespace Exam.WebApp.Services;

public static class ApiRoutes
{
    public static class Users
    {
        public const string Base = "/api/users";
        public const string Me = "/api/users/me";
        public static string Role(string externalId) => $"{Base}/{externalId}/role";
        public static string Active(string externalId) => $"{Base}/{externalId}/active";
        public static string Paged(int page, int pageSize, string? search = null, UserRole? role = null, bool? isActive = null)
        {
            var url = $"{Base}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (role.HasValue)
                url += $"&role={role.Value}";
            if (isActive.HasValue)
                url += $"&isActive={isActive.Value}";
            return url;
        }
    }

    public static class Categories
    {
        public const string Base = "/api/categories";
        public static string ById(string id) => $"{Base}/{id}";
    }

    public static class Questions
    {
        public const string Base = "/api/questions";
        public const string Move = "/api/questions/move";
        public static string Import(bool dryRun) => $"/api/questions/import?dryRun={dryRun}";
        public static string ById(string id) => $"{Base}/{id}";
        public static string ByCategory(string categoryId) => $"{Base}/by-category/{categoryId}";
        public static string Export(string categoryId) => $"{Base}/export?categoryId={Uri.EscapeDataString(categoryId)}";

        public static string ByCategoryPaged(string categoryId, int page, int pageSize, Level? level = null,
            QuestionType? questionType = null, string? keyword = null)
        {
            var url = $"{Base}/by-category/{categoryId}/page?page={page}&pageSize={pageSize}";
            if (level.HasValue)
                url += $"&level={level.Value}";
            if (questionType.HasValue)
                url += $"&questionType={questionType.Value}";
            if (!string.IsNullOrWhiteSpace(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            return url;
        }
    }

    public static class Exams
    {
        public const string Base = "/api/exams";
        public static string ById(string id) => $"{Base}/{id}";
        public static string ByCategory(string categoryId, int page, int pageSize) => $"{Base}/by-category/{categoryId}?page={page}&pageSize={pageSize}";
        public static string Composition(string examId) => $"{Base}/{examId}/composition";
        public static string Availability(string examId) => $"{Base}/{examId}/availability";
        public static string MaxAttempts(string examId) => $"{Base}/{examId}/max-attempts";
        public static string Publish(string examId) => $"{Base}/{examId}/publish";
        public static string Unpublish(string examId) => $"{Base}/{examId}/unpublish";
        public static string Archive(string examId) => $"{Base}/{examId}/archive";
        public static string Results(string examId, int page, int pageSize) => $"{Base}/{examId}/results?page={page}&pageSize={pageSize}";
        public static string ExportResults(string examId) => $"{Base}/{examId}/results/export";
        public static string NotAttempted(string examId) => $"{Base}/{examId}/not-attempted";
        public static string Available(int page, int pageSize) => $"{Base}/available?page={page}&pageSize={pageSize}";
        public static string Class(string examId, string classId) => $"{Base}/{examId}/classes/{classId}";
    }

    public static class Classes
    {
        public const string Base = "/api/classes";
        public const string Mine = "/api/classes/mine";
        public const string Join = "/api/classes/join";
        public static string ById(string id) => $"{Base}/{id}";
        public static string RegenerateCode(string id) => $"{Base}/{id}/regenerate-code";
        public static string Member(string id, string userId) => $"{Base}/{id}/members/{userId}";
        public static string ImportMembers(string id, bool dryRun) => $"{Base}/{id}/members/import?dryRun={dryRun}";
        public static string ExportMembers(string id) => $"{Base}/{id}/members/export";
    }

    public static class ExamAttempts
    {
        public const string Base = "/api/exam-attempts";
        public const string Start = "/api/exam-attempts/start";
        public static string ById(string id) => $"{Base}/{id}";
        public static string Answers(string id) => $"{Base}/{id}/answers";
        public static string Finish(string id) => $"{Base}/{id}/finish";
        public static string Result(string id) => $"{Base}/{id}/result";
        public static string History(int page, int pageSize) => $"{Base}/history?page={page}&pageSize={pageSize}";
        public static string AdminStatus(string id) => $"{Base}/{id}/admin-status";
        public static string AdminForceFinish(string id) => $"{Base}/{id}/admin-force-finish";
        public static string AdminRegrade(string id) => $"{Base}/{id}/admin-regrade";
    }

    public static class Dashboard
    {
        public const string Summary = "/api/dashboard/summary";
    }

    public static class RolePermissions
    {
        public const string Base = "/api/role-permissions";
        public static string ByRole(UserRole role) => $"{Base}/{role}";
    }

    public static class AuditLog
    {
        public const string Base = "/api/audit-log";

        public static string Paged(int page, int pageSize, string? actor = null, string? action = null,
            DateTime? from = null, DateTime? to = null)
        {
            var url = $"{Base}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(actor))
                url += $"&actor={Uri.EscapeDataString(actor)}";
            if (!string.IsNullOrWhiteSpace(action))
                url += $"&action={Uri.EscapeDataString(action)}";
            if (from.HasValue)
                url += $"&from={from.Value:yyyy-MM-dd}";
            if (to.HasValue)
                url += $"&to={to.Value:yyyy-MM-dd}";
            return url;
        }
    }
}
