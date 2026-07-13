namespace Exam.WebApp.Services;

public static class ApiRoutes
{
    public static class Users
    {
        public const string Base = "/api/users";
        public const string Me = "/api/users/me";
        public static string Role(string externalId) => $"{Base}/{externalId}/role";
        public static string Paged(int page, int pageSize) => $"{Base}?page={page}&pageSize={pageSize}";
    }

    public static class Categories
    {
        public const string Base = "/api/categories";
        public static string ById(string id) => $"{Base}/{id}";
    }

    public static class Questions
    {
        public const string Base = "/api/questions";
        public static string ById(string id) => $"{Base}/{id}";
        public static string ByCategory(string categoryId) => $"{Base}/by-category/{categoryId}";

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
        public static string Question(string examId, string questionId) => $"{Base}/{examId}/questions/{questionId}";
        public static string QuestionPool(string examId) => $"{Base}/{examId}/question-pool";
        public static string Availability(string examId) => $"{Base}/{examId}/availability";
        public static string NegativeMarking(string examId) => $"{Base}/{examId}/negative-marking";
        public static string Publish(string examId) => $"{Base}/{examId}/publish";
        public static string Unpublish(string examId) => $"{Base}/{examId}/unpublish";
        public static string Archive(string examId) => $"{Base}/{examId}/archive";
        public static string Results(string examId, int page, int pageSize) => $"{Base}/{examId}/results?page={page}&pageSize={pageSize}";
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
        public static string Paged(int page, int pageSize) => $"{Base}?page={page}&pageSize={pageSize}";
    }
}
