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
        public static string ByCategoryPaged(string categoryId, int page, int pageSize) => $"{Base}/by-category/{categoryId}/page?page={page}&pageSize={pageSize}";
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
