namespace Exam.Contracts;

public static class Permissions
{
    public static class Category
    {
        public const string Create = "Category.Create";
        public const string Update = "Category.Update";
        public const string Delete = "Category.Delete";
    }

    public static class Question
    {
        public const string View = "Question.View";
        public const string Create = "Question.Create";
        public const string Update = "Question.Update";
        public const string Delete = "Question.Delete";
    }

    public static class Exam
    {
        public const string Create = "Exam.Create";
        public const string Update = "Exam.Update";
        public const string Delete = "Exam.Delete";
        public const string ManageQuestions = "Exam.ManageQuestions";
        public const string ManagePool = "Exam.ManagePool";
        public const string ManageAvailability = "Exam.ManageAvailability";
        public const string ManageNegativeMarking = "Exam.ManageNegativeMarking";
        public const string ManageMaxAttempts = "Exam.ManageMaxAttempts";
        public const string Publish = "Exam.Publish";
        public const string Unpublish = "Exam.Unpublish";
        public const string Archive = "Exam.Archive";
        public const string ViewResults = "Exam.ViewResults";
        public const string ManageClassAssignment = "Exam.ManageClassAssignment";
        public const string ForceFinishAttempt = "Exam.ForceFinishAttempt";
    }

    public static class Class
    {
        public const string View = "Class.View";
        public const string Create = "Class.Create";
        public const string Update = "Class.Update";
        public const string Delete = "Class.Delete";
        public const string ManageMembers = "Class.ManageMembers";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class User
    {
        public const string View = "User.View";
        public const string PromoteRole = "User.PromoteRole";
        public const string ToggleActive = "User.ToggleActive";
    }

    public static readonly IReadOnlyCollection<string> All =
    [
        Category.Create, Category.Update, Category.Delete,
        Question.View, Question.Create, Question.Update, Question.Delete,
        Exam.Create, Exam.Update, Exam.Delete, Exam.ManageQuestions, Exam.ManagePool,
        Exam.ManageAvailability, Exam.ManageNegativeMarking, Exam.ManageMaxAttempts, Exam.Publish, Exam.Unpublish, Exam.Archive,
        Exam.ViewResults, Exam.ManageClassAssignment, Exam.ForceFinishAttempt,
        Class.View, Class.Create, Class.Update, Class.Delete, Class.ManageMembers,
        User.View, User.PromoteRole, User.ToggleActive,
        Dashboard.View
    ];
}
