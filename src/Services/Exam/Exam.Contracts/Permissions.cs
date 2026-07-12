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
        public const string Publish = "Exam.Publish";
        public const string Unpublish = "Exam.Unpublish";
        public const string Archive = "Exam.Archive";
    }

    public static class User
    {
        public const string View = "User.View";
        public const string PromoteRole = "User.PromoteRole";
    }

    public static readonly IReadOnlyCollection<string> All =
    [
        Category.Create, Category.Update, Category.Delete,
        Question.View, Question.Create, Question.Update, Question.Delete,
        Exam.Create, Exam.Update, Exam.Delete, Exam.ManageQuestions, Exam.ManagePool,
        Exam.ManageAvailability, Exam.ManageNegativeMarking, Exam.Publish, Exam.Unpublish, Exam.Archive,
        User.View, User.PromoteRole
    ];
}
