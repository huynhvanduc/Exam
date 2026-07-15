using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MongoDB.Bson;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class DataSeeder
{
    private const string AdminExternalId = "528ac9b1-20ff-4d42-bd6d-e85300acde89";
    private const string InstructorExternalId = "22222222-2222-2222-2222-222222222222";
    private const string Student1ExternalId = "33333333-3333-3333-3333-333333333333";
    private const string Student2ExternalId = "44444444-4444-4444-4444-444444444444";
    private const string Student3ExternalId = "55555555-5555-5555-5555-555555555555";
    private const string Instructor2ExternalId = "66666666-6666-6666-6666-666666666666";
    private const string Student4ExternalId = "77777777-7777-7777-7777-777777777777";
    private const string Student5ExternalId = "88888888-8888-8888-8888-888888888888";
    private const string Student6ExternalId = "99999999-9999-9999-9999-999999999999";

    private sealed record SeedAnswer(string Content, bool IsCorrect);

    private sealed record SeedQuestion(string Content, QuestionType Type, Level Level, string Explain, SeedAnswer[] Answers);

    private sealed record SeedCategory(string Name, string UrlPath, SeedQuestion[] Questions);

    public static async Task EnsureSampleDataAsync(
        ICategoryRepository categoryRepository,
        IQuestionRepository questionRepository,
        IExamRepository examRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IClassRoomRepository classRoomRepository,
        CancellationToken cancellationToken = default)
    {
        var existingCategories = await categoryRepository.GetAllAsync(cancellationToken);
        if (existingCategories.Count > 0)
            return;

        var admin = await SeedUserAsync(userRepository, AdminExternalId, "admin@.com.vn", "Admin", "1", UserRole.Admin, cancellationToken);
        var instructor = await SeedUserAsync(userRepository, InstructorExternalId, "minh.tran@exam-platform.vn", "Minh", "Trần", UserRole.Instructor, cancellationToken);
        var student1 = await SeedUserAsync(userRepository, Student1ExternalId, "lan.nguyen@exam-platform.vn", "Lan", "Nguyễn", UserRole.Student, cancellationToken);
        var student2 = await SeedUserAsync(userRepository, Student2ExternalId, "hung.pham@exam-platform.vn", "Hùng", "Phạm", UserRole.Student, cancellationToken);
        await SeedUserAsync(userRepository, Student3ExternalId, "hoa.le@exam-platform.vn", "Hoa", "Lê", UserRole.Student, cancellationToken);
        var instructor2 = await SeedUserAsync(userRepository, Instructor2ExternalId, "tuan.le@exam-platform.vn", "Tuấn", "Lê", UserRole.Instructor, cancellationToken);

        // Học viên chưa thuộc lớp nào - dùng để test tính năng Import Excel danh sách lớp
        // (khớp vào tài khoản đã có sẵn theo email, không tạo tài khoản mới).
        await SeedUserAsync(userRepository, Student4ExternalId, "duong.vo@exam-platform.vn", "Dương", "Võ", UserRole.Student, cancellationToken);
        await SeedUserAsync(userRepository, Student5ExternalId, "mai.dang@exam-platform.vn", "Mai", "Đặng", UserRole.Student, cancellationToken);
        await SeedUserAsync(userRepository, Student6ExternalId, "khanh.bui@exam-platform.vn", "Khánh", "Bùi", UserRole.Student, cancellationToken);

        // Lớp demo: minh hoạ đề thi giao riêng cho lớp cụ thể (chỉ thành viên mới thi được), song song với
        // các đề công khai khác được seed bên dưới - để phân biệt rõ 2 chế độ trên UI.
        var demoClass = ClassRoom.Create("Lớp Demo K1", instructor.ExternalId);
        demoClass.AddMember(student1.ExternalId);
        demoClass.AddMember(student2.ExternalId);
        await classRoomRepository.InsertAsync(demoClass, cancellationToken);

        // Lớp thứ 2 do giáo viên khác quản lý - dùng để kiểm chứng Instructor A không thấy được lớp của Instructor B.
        var otherInstructorClass = ClassRoom.Create("Lớp của Tuấn Lê", instructor2.ExternalId);
        await classRoomRepository.InsertAsync(otherInstructorClass, cancellationToken);

        ExamEntity? firstPublishedExam = null;

        var publishedComposition = new List<ExamCompositionCell>
        {
            new(Level.Easy, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.MultipleSelection, 1),
            new(Level.Difficult, QuestionType.SingleSelection, 1),
        };
        var draftComposition = new List<ExamCompositionCell>
        {
            new(Level.Easy, QuestionType.SingleSelection, 1),
            new(Level.Medium, QuestionType.SingleSelection, 1),
        };

        foreach (var seedCategory in GetSeedCategories())
        {
            var category = Category.Create(seedCategory.Name, seedCategory.UrlPath);
            await categoryRepository.InsertAsync(category, cancellationToken);
            await auditLogRepository.InsertAsync(
                new AuditLogEntry(admin.ExternalId, "Category.Create", category.Id, $"Tạo danh mục \"{category.Name}\"."),
                cancellationToken);

            var questionIds = new List<string>();
            foreach (var seedQuestion in seedCategory.Questions)
            {
                var answers = seedQuestion.Answers
                    .Select(a => new Answer(ObjectId.GenerateNewId().ToString(), a.Content, a.IsCorrect))
                    .ToList();

                var question = new Question(null, seedQuestion.Content, seedQuestion.Type, seedQuestion.Level,
                    category.Id, answers, seedQuestion.Explain, ownerUserId: instructor.ExternalId,
                    categoryName: category.Name);

                await questionRepository.InsertAsync(question, cancellationToken);
                questionIds.Add(question.Id);
            }

            await auditLogRepository.InsertAsync(
                new AuditLogEntry(instructor.ExternalId, "Question.Create", category.Id,
                    $"Thêm {questionIds.Count} câu hỏi vào danh mục \"{category.Name}\"."),
                cancellationToken);

            var publishedExam = new ExamEntity(
                $"Bài kiểm tra {seedCategory.Name}", $"Đề kiểm tra tổng hợp - {seedCategory.Name}",
                $"Bài thi gồm {publishedComposition.Sum(c => c.Count)} câu hỏi thuộc danh mục {seedCategory.Name}.",
                TimeSpan.FromMinutes(45), Level.Medium, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: true, minimumPassingScore: 6.0m);
            publishedExam.ConfigureComposition(publishedComposition);
            publishedExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
            publishedExam.Publish();

            firstPublishedExam ??= publishedExam;
            if (ReferenceEquals(firstPublishedExam, publishedExam))
                publishedExam.AssignToClass(demoClass.Id);

            await examRepository.InsertAsync(publishedExam, cancellationToken);
            await auditLogRepository.InsertAsync(
                new AuditLogEntry(instructor.ExternalId, "Exam.Publish", publishedExam.Id,
                    $"Xuất bản đề thi \"{publishedExam.Name}\"."),
                cancellationToken);

            var draftExam = new ExamEntity(
                $"Đề nháp {seedCategory.Name}", $"Đề đang soạn - {seedCategory.Name}",
                $"Bản nháp đề thi thuộc danh mục {seedCategory.Name}, chưa xuất bản.",
                TimeSpan.FromMinutes(30), Level.Easy, instructor.ExternalId, category.Id, category.Name,
                isTimeRestricted: false, minimumPassingScore: 6.0m);
            draftExam.ConfigureComposition(draftComposition);
            await examRepository.InsertAsync(draftExam, cancellationToken);
        }
    }

    private static async Task<User> SeedUserAsync(IUserRepository userRepository, string externalId, string email,
        string firstName, string lastName, UserRole role, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByExternalIdAsync(externalId, cancellationToken);
        if (existing != null)
            return existing;

        var user = User.CreateNewUser(externalId, email, firstName, lastName, role);
        await userRepository.InsertAsync(user, cancellationToken);
        return user;
    }

    private static SeedCategory[] GetSeedCategories() =>
    [
        new SeedCategory("Lập trình C#", "lap-trinh-csharp",
        [
            new SeedQuestion("Từ khóa nào được dùng để ngăn một lớp bị kế thừa trong C#?",
                QuestionType.SingleSelection, Level.Easy,
                "sealed ngăn không cho lớp khác kế thừa từ lớp này.",
                [
                    new SeedAnswer("sealed", true),
                    new SeedAnswer("abstract", false),
                    new SeedAnswer("virtual", false),
                    new SeedAnswer("partial", false),
                ]),
            new SeedQuestion("Trong C#, `string` thuộc loại kiểu dữ liệu nào?",
                QuestionType.SingleSelection, Level.Medium,
                "string là reference type dù có ngữ nghĩa bất biến (immutable) giống value type.",
                [
                    new SeedAnswer("Kiểu tham chiếu (reference type)", true),
                    new SeedAnswer("Kiểu giá trị (value type)", false),
                    new SeedAnswer("Kiểu con trỏ", false),
                    new SeedAnswer("Kiểu liệt kê (enum)", false),
                ]),
            new SeedQuestion("Những phát biểu nào sau đây đúng về LINQ trong C#?",
                QuestionType.MultipleSelection, Level.Medium,
                "LINQ to Objects truy vấn được trên collection trong bộ nhớ và áp dụng deferred execution.",
                [
                    new SeedAnswer("Cho phép truy vấn dữ liệu bằng cú pháp giống SQL", true),
                    new SeedAnswer("Chỉ hoạt động với cơ sở dữ liệu SQL Server", false),
                    new SeedAnswer("LINQ to Objects truy vấn được trên collection trong bộ nhớ", true),
                    new SeedAnswer("Truy vấn LINQ mặc định chỉ thực thi khi được duyệt (deferred execution)", true),
                ]),
            new SeedQuestion("Garbage Collector của .NET quản lý bộ nhớ theo cơ chế nào?",
                QuestionType.SingleSelection, Level.Difficult,
                ".NET GC dùng mô hình generational (thế hệ 0, 1, 2) để tối ưu việc thu gom.",
                [
                    new SeedAnswer("Generational GC (thế hệ 0, 1, 2)", true),
                    new SeedAnswer("Reference counting", false),
                    new SeedAnswer("Giải phóng thủ công bằng free() như C", false),
                    new SeedAnswer("Chỉ cấp phát trên stack", false),
                ]),
        ]),
        new SeedCategory("Cơ sở dữ liệu", "co-so-du-lieu",
        [
            new SeedQuestion("Câu lệnh SQL nào dùng để lấy dữ liệu từ bảng?",
                QuestionType.SingleSelection, Level.Easy,
                "SELECT dùng để truy vấn/lấy dữ liệu.",
                [
                    new SeedAnswer("SELECT", true),
                    new SeedAnswer("INSERT", false),
                    new SeedAnswer("UPDATE", false),
                    new SeedAnswer("DELETE", false),
                ]),
            new SeedQuestion("Khóa nào đảm bảo tính duy nhất và không NULL cho mỗi dòng trong bảng?",
                QuestionType.SingleSelection, Level.Medium,
                "Primary Key vừa duy nhất vừa không được NULL.",
                [
                    new SeedAnswer("Primary Key", true),
                    new SeedAnswer("Foreign Key", false),
                    new SeedAnswer("Unique Key", false),
                    new SeedAnswer("Index", false),
                ]),
            new SeedQuestion("Những đặc tính nào sau đây thuộc ACID trong giao dịch cơ sở dữ liệu?",
                QuestionType.MultipleSelection, Level.Medium,
                "ACID gồm Atomicity, Consistency, Isolation, Durability - không có Availability.",
                [
                    new SeedAnswer("Atomicity", true),
                    new SeedAnswer("Consistency", true),
                    new SeedAnswer("Isolation", true),
                    new SeedAnswer("Availability", false),
                ]),
            new SeedQuestion("Chuẩn hóa (normalization) dạng 3NF yêu cầu điều gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "3NF loại bỏ phụ thuộc bắc cầu (transitive dependency) vào khóa chính.",
                [
                    new SeedAnswer("Loại bỏ phụ thuộc bắc cầu vào khóa chính", true),
                    new SeedAnswer("Loại bỏ tất cả các khóa ngoại", false),
                    new SeedAnswer("Gộp tất cả bảng thành một bảng duy nhất", false),
                    new SeedAnswer("Chỉ áp dụng cho cơ sở dữ liệu NoSQL", false),
                ]),
        ]),
        new SeedCategory("Mạng máy tính", "mang-may-tinh",
        [
            new SeedQuestion("Giao thức nào hoạt động ở tầng vận chuyển và đảm bảo truyền dữ liệu tin cậy?",
                QuestionType.SingleSelection, Level.Easy,
                "TCP đảm bảo tin cậy nhờ bắt tay ba bước và xác nhận gói tin.",
                [
                    new SeedAnswer("TCP", true),
                    new SeedAnswer("UDP", false),
                    new SeedAnswer("IP", false),
                    new SeedAnswer("HTTP", false),
                ]),
            new SeedQuestion("Cổng (port) mặc định của HTTPS là gì?",
                QuestionType.SingleSelection, Level.Medium,
                "HTTPS mặc định dùng cổng 443.",
                [
                    new SeedAnswer("443", true),
                    new SeedAnswer("80", false),
                    new SeedAnswer("21", false),
                    new SeedAnswer("25", false),
                ]),
            new SeedQuestion("Những giao thức nào thuộc tầng ứng dụng trong mô hình TCP/IP?",
                QuestionType.MultipleSelection, Level.Medium,
                "HTTP, FTP, DNS đều thuộc tầng ứng dụng; TCP thuộc tầng vận chuyển.",
                [
                    new SeedAnswer("HTTP", true),
                    new SeedAnswer("FTP", true),
                    new SeedAnswer("DNS", true),
                    new SeedAnswer("TCP", false),
                ]),
            new SeedQuestion("Subnet mask 255.255.255.192 cho phép bao nhiêu host hợp lệ trên mỗi subnet?",
                QuestionType.SingleSelection, Level.Difficult,
                "/26 có 2^6 - 2 = 62 host hợp lệ.",
                [
                    new SeedAnswer("62", true),
                    new SeedAnswer("64", false),
                    new SeedAnswer("30", false),
                    new SeedAnswer("126", false),
                ]),
        ]),
        new SeedCategory("Cấu trúc dữ liệu & Giải thuật", "cau-truc-du-lieu-giai-thuat",
        [
            new SeedQuestion("Cấu trúc dữ liệu nào hoạt động theo nguyên tắc vào sau ra trước (LIFO)?",
                QuestionType.SingleSelection, Level.Easy,
                "Stack hoạt động theo LIFO.",
                [
                    new SeedAnswer("Stack", true),
                    new SeedAnswer("Queue", false),
                    new SeedAnswer("Linked List", false),
                    new SeedAnswer("Array", false),
                ]),
            new SeedQuestion("Độ phức tạp thời gian trung bình của thuật toán Quick Sort là gì?",
                QuestionType.SingleSelection, Level.Medium,
                "Quick Sort có độ phức tạp trung bình O(n log n), trường hợp xấu nhất O(n^2).",
                [
                    new SeedAnswer("O(n log n)", true),
                    new SeedAnswer("O(n^2)", false),
                    new SeedAnswer("O(n)", false),
                    new SeedAnswer("O(log n)", false),
                ]),
            new SeedQuestion("Những thuật toán nào sau đây thuộc nhóm sắp xếp so sánh (comparison-based sorting)?",
                QuestionType.MultipleSelection, Level.Medium,
                "Bubble/Merge/Quick Sort đều so sánh phần tử; Counting Sort không dựa trên so sánh.",
                [
                    new SeedAnswer("Bubble Sort", true),
                    new SeedAnswer("Merge Sort", true),
                    new SeedAnswer("Quick Sort", true),
                    new SeedAnswer("Counting Sort", false),
                ]),
            new SeedQuestion("Cây nhị phân tìm kiếm cân bằng (AVL) đảm bảo độ phức tạp tìm kiếm là gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "AVL luôn cân bằng nên chiều cao cây là O(log n), do đó tìm kiếm là O(log n).",
                [
                    new SeedAnswer("O(log n)", true),
                    new SeedAnswer("O(n)", false),
                    new SeedAnswer("O(1)", false),
                    new SeedAnswer("O(n log n)", false),
                ]),
        ]),
        new SeedCategory("Kiến thức chung CNTT", "kien-thuc-chung-cntt",
        [
            new SeedQuestion("HTML là viết tắt của gì?",
                QuestionType.SingleSelection, Level.Easy,
                "HTML = HyperText Markup Language.",
                [
                    new SeedAnswer("HyperText Markup Language", true),
                    new SeedAnswer("High-Tech Modern Language", false),
                    new SeedAnswer("Home Tool Markup Language", false),
                    new SeedAnswer("Hyperlink Text Management Language", false),
                ]),
            new SeedQuestion("Mô hình phát triển phần mềm nào chia dự án thành nhiều chu kỳ lặp ngắn?",
                QuestionType.SingleSelection, Level.Medium,
                "Agile/Scrum phát triển qua các sprint ngắn, lặp đi lặp lại.",
                [
                    new SeedAnswer("Agile/Scrum", true),
                    new SeedAnswer("Waterfall", false),
                    new SeedAnswer("V-Model", false),
                    new SeedAnswer("Big Bang", false),
                ]),
            new SeedQuestion("Những yếu tố nào sau đây thuộc nguyên lý SOLID trong lập trình hướng đối tượng?",
                QuestionType.MultipleSelection, Level.Medium,
                "SOLID gồm Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion.",
                [
                    new SeedAnswer("Single Responsibility", true),
                    new SeedAnswer("Open/Closed", true),
                    new SeedAnswer("Liskov Substitution", true),
                    new SeedAnswer("Global State", false),
                ]),
            new SeedQuestion("Trong kiến trúc microservices, cơ chế nào thường dùng để các service giao tiếp bất đồng bộ?",
                QuestionType.SingleSelection, Level.Difficult,
                "Message Queue/Event Bus (RabbitMQ, Kafka...) là lựa chọn phổ biến cho giao tiếp bất đồng bộ.",
                [
                    new SeedAnswer("Message Queue / Event Bus", true),
                    new SeedAnswer("Shared Database", false),
                    new SeedAnswer("Truy cập file trực tiếp", false),
                    new SeedAnswer("Biến toàn cục (global variables)", false),
                ]),
        ]),
    ];
}
