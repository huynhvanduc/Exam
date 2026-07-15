using System.Reflection;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MongoDB.Bson;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class DataSeeder
{
    // Id cố định để khớp với tài khoản Identity Server tương ứng (xem Identity.Server/Persistence/SeedData.cs) -
    // các user này đăng nhập thật được. Các user "phụ" thêm bên dưới (SeedExtraId) chỉ tồn tại ở Mongo, không
    // đăng nhập được - dùng để lấp đầy danh sách lớp/kết quả thi cho giống môi trường production thật.
    private const string AdminExternalId = "528ac9b1-20ff-4d42-bd6d-e85300acde89";
    private const string InstructorExternalId = "22222222-2222-2222-2222-222222222222";
    private const string Student1ExternalId = "33333333-3333-3333-3333-333333333333";
    private const string Student2ExternalId = "44444444-4444-4444-4444-444444444444";
    private const string Student3ExternalId = "55555555-5555-5555-5555-555555555555";
    private const string Instructor2ExternalId = "66666666-6666-6666-6666-666666666666";
    private const string Student4ExternalId = "77777777-7777-7777-7777-777777777777";
    private const string Student5ExternalId = "88888888-8888-8888-8888-888888888888";
    private const string Student6ExternalId = "99999999-9999-9999-9999-999999999999";

    private static readonly Random Rng = new(20260715); // seed cố định -> dữ liệu demo giống nhau mỗi lần reseed

    private sealed record SeedAnswer(string Content, bool IsCorrect);

    private sealed record SeedQuestion(string Content, QuestionType Type, Level Level, string Explain, SeedAnswer[] Answers);

    private sealed record SeedCategory(string Name, string UrlPath, SeedQuestion[] Questions);

    private sealed record SeedExtraUser(string ExternalId, string Email, string FirstName, string LastName);

    private sealed record CategoryPool(Category Category, IReadOnlyList<Question> Questions);

    private static readonly ExamCompositionCell[] StandardComposition =
    [
        new(Level.Easy, QuestionType.SingleSelection, 2),
        new(Level.Medium, QuestionType.SingleSelection, 2),
        new(Level.Medium, QuestionType.MultipleSelection, 1),
        new(Level.Difficult, QuestionType.SingleSelection, 1),
    ];

    private static readonly ExamCompositionCell[] DraftComposition =
    [
        new(Level.Easy, QuestionType.SingleSelection, 1),
        new(Level.Medium, QuestionType.SingleSelection, 1),
    ];

    public static async Task EnsureSampleDataAsync(
        ICategoryRepository categoryRepository,
        IQuestionRepository questionRepository,
        IExamRepository examRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IClassRoomRepository classRoomRepository,
        IExamResultRepository examResultRepository,
        CancellationToken cancellationToken = default)
    {
        var existingCategories = await categoryRepository.GetAllAsync(cancellationToken);
        if (existingCategories.Count > 0)
            return;

        var (admin, instructors, coreStudents, extraStudents) = await SeedUsersAsync(userRepository, cancellationToken);
        var allStudents = coreStudents.Concat(extraStudents).ToList();

        var classes = await SeedClassesAsync(classRoomRepository, instructors, extraStudents, cancellationToken);

        var categoryPools = await SeedCategoriesAndQuestionsAsync(
            categoryRepository, questionRepository, auditLogRepository, admin, instructors[0], cancellationToken);

        var exams = await SeedExamsAsync(examRepository, auditLogRepository, categoryPools, instructors, classes, cancellationToken);

        await SeedExamResultsAsync(examResultRepository, exams, classes, allStudents, cancellationToken);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Users
    // ---------------------------------------------------------------------------------------------------------

    private static async Task<(User Admin, IReadOnlyList<User> Instructors, IReadOnlyList<User> CoreStudents, IReadOnlyList<User> ExtraStudents)>
        SeedUsersAsync(IUserRepository userRepository, CancellationToken cancellationToken)
    {
        var admin = await SeedUserAsync(userRepository, AdminExternalId, "admin@.com.vn", "Admin", "1", UserRole.Admin, cancellationToken);
        var instructor1 = await SeedUserAsync(userRepository, InstructorExternalId, "minh.tran@exam-platform.vn", "Minh", "Trần", UserRole.Instructor, cancellationToken);
        var instructor2 = await SeedUserAsync(userRepository, Instructor2ExternalId, "tuan.le@exam-platform.vn", "Tuấn", "Lê", UserRole.Instructor, cancellationToken);
        var instructor3 = await SeedUserAsync(userRepository, ExtraId(100), "hai.do@exam-platform.vn", "Hải", "Đỗ", UserRole.Instructor, cancellationToken);

        var student1 = await SeedUserAsync(userRepository, Student1ExternalId, "lan.nguyen@exam-platform.vn", "Lan", "Nguyễn", UserRole.Student, cancellationToken);
        var student2 = await SeedUserAsync(userRepository, Student2ExternalId, "hung.pham@exam-platform.vn", "Hùng", "Phạm", UserRole.Student, cancellationToken);
        var student3 = await SeedUserAsync(userRepository, Student3ExternalId, "hoa.le@exam-platform.vn", "Hoa", "Lê", UserRole.Student, cancellationToken);

        // Học viên chưa thuộc lớp nào - dùng để test tính năng Import Excel danh sách lớp (khớp vào tài khoản
        // đã có sẵn theo email, không tạo tài khoản mới). Giữ nguyên KHÔNG gán lớp để không phá vỡ kịch bản test đó.
        var student4 = await SeedUserAsync(userRepository, Student4ExternalId, "duong.vo@exam-platform.vn", "Dương", "Võ", UserRole.Student, cancellationToken);
        var student5 = await SeedUserAsync(userRepository, Student5ExternalId, "mai.dang@exam-platform.vn", "Mai", "Đặng", UserRole.Student, cancellationToken);
        var student6 = await SeedUserAsync(userRepository, Student6ExternalId, "khanh.bui@exam-platform.vn", "Khánh", "Bùi", UserRole.Student, cancellationToken);

        var extraUserDefs = new[]
        {
            new SeedExtraUser(ExtraId(1), "quang.tran@exam-platform.vn", "Quang", "Trần"),
            new SeedExtraUser(ExtraId(2), "linh.do@exam-platform.vn", "Linh", "Đỗ"),
            new SeedExtraUser(ExtraId(3), "nam.hoang@exam-platform.vn", "Nam", "Hoàng"),
            new SeedExtraUser(ExtraId(4), "thao.vu@exam-platform.vn", "Thảo", "Vũ"),
            new SeedExtraUser(ExtraId(5), "binh.ngo@exam-platform.vn", "Bình", "Ngô"),
            new SeedExtraUser(ExtraId(6), "trang.duong@exam-platform.vn", "Trang", "Dương"),
            new SeedExtraUser(ExtraId(7), "duc.phan@exam-platform.vn", "Đức", "Phan"),
            new SeedExtraUser(ExtraId(8), "ngoc.dinh@exam-platform.vn", "Ngọc", "Đinh"),
            new SeedExtraUser(ExtraId(9), "son.ly@exam-platform.vn", "Sơn", "Lý"),
            new SeedExtraUser(ExtraId(10), "ha.trinh@exam-platform.vn", "Hà", "Trịnh"),
            new SeedExtraUser(ExtraId(11), "phuc.cao@exam-platform.vn", "Phúc", "Cao"),
            new SeedExtraUser(ExtraId(12), "yen.doan@exam-platform.vn", "Yến", "Đoàn"),
            new SeedExtraUser(ExtraId(13), "tung.lam@exam-platform.vn", "Tùng", "Lâm"),
            new SeedExtraUser(ExtraId(14), "vy.mac@exam-platform.vn", "Vy", "Mạc"),
            new SeedExtraUser(ExtraId(15), "an.to@exam-platform.vn", "An", "Tô"),
        };

        var extraStudents = new List<User>();
        foreach (var def in extraUserDefs)
            extraStudents.Add(await SeedUserAsync(userRepository, def.ExternalId, def.Email, def.FirstName, def.LastName, UserRole.Student, cancellationToken));

        return (admin,
            [instructor1, instructor2, instructor3],
            [student1, student2, student3, student4, student5, student6],
            extraStudents);
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

    // GUID hợp lệ nhưng không đụng dải id "1..9 lặp lại" đã dùng cho các tài khoản đăng nhập được ở trên.
    private static string ExtraId(int n) => $"a0000000-0000-4000-8000-{n:D12}";

    // ---------------------------------------------------------------------------------------------------------
    // Classes
    // ---------------------------------------------------------------------------------------------------------

    private static async Task<IReadOnlyList<ClassRoom>> SeedClassesAsync(IClassRoomRepository classRoomRepository,
        IReadOnlyList<User> instructors, IReadOnlyList<User> extraStudents, CancellationToken cancellationToken)
    {
        var (instructor1, instructor2, instructor3) = (instructors[0], instructors[1], instructors[2]);
        var e = extraStudents; // e[0] = Quang Trần ... e[14] = An Tô

        // Lớp demo gốc: minh hoạ đề thi giao riêng cho lớp cụ thể (chỉ thành viên mới thi được), song song với
        // các đề công khai khác được seed bên dưới - để phân biệt rõ 2 chế độ trên UI. Giữ nguyên 2 thành viên
        // gốc (đã dùng làm baseline test cho tính năng Import/Export roster).
        var demoClass = ClassRoom.Create("Lớp Demo K1", instructor1.ExternalId);
        demoClass.AddMember(Student1ExternalId);
        demoClass.AddMember(Student2ExternalId);
        await classRoomRepository.InsertAsync(demoClass, cancellationToken);

        var cnttK2 = ClassRoom.Create("Lớp CNTT K2", instructor1.ExternalId);
        cnttK2.AddMember(Student3ExternalId);
        foreach (var s in e.Take(5)) // Quang, Linh, Nam, Thảo, Bình
            cnttK2.AddMember(s.ExternalId);
        await classRoomRepository.InsertAsync(cnttK2, cancellationToken);

        // Lớp thứ 2 do giáo viên khác quản lý - dùng để kiểm chứng Instructor A không thấy được lớp của Instructor B.
        // Trước đây để trống, giờ có thành viên thật cho gần với dữ liệu production hơn.
        var tuanLeClass = ClassRoom.Create("Lớp của Tuấn Lê", instructor2.ExternalId);
        foreach (var s in e.Skip(5).Take(6)) // Trang, Đức, Ngọc, Sơn, Hà, Phúc
            tuanLeClass.AddMember(s.ExternalId);
        await classRoomRepository.InsertAsync(tuanLeClass, cancellationToken);

        var aiClass = ClassRoom.Create("Lớp AI nâng cao", instructor2.ExternalId);
        foreach (var s in e.Skip(11).Take(3)) // Yến, Tùng, Vy
            aiClass.AddMember(s.ExternalId);
        await classRoomRepository.InsertAsync(aiClass, cancellationToken);

        var securityClass = ClassRoom.Create("Lớp Bảo mật hệ thống", instructor3.ExternalId);
        securityClass.AddMember(e[0].ExternalId);  // Quang - học chung với CNTT K2, thực tế học 2 lớp
        securityClass.AddMember(e[5].ExternalId);  // Trang - học chung với lớp Tuấn Lê
        securityClass.AddMember(e[14].ExternalId); // An Tô
        await classRoomRepository.InsertAsync(securityClass, cancellationToken);

        return [demoClass, cnttK2, tuanLeClass, aiClass, securityClass];
    }

    // ---------------------------------------------------------------------------------------------------------
    // Categories & Questions
    // ---------------------------------------------------------------------------------------------------------

    private static async Task<IReadOnlyList<CategoryPool>> SeedCategoriesAndQuestionsAsync(
        ICategoryRepository categoryRepository, IQuestionRepository questionRepository,
        IAuditLogRepository auditLogRepository, User admin, User owner, CancellationToken cancellationToken)
    {
        var pools = new List<CategoryPool>();

        foreach (var seedCategory in GetSeedCategories())
        {
            var category = Category.Create(seedCategory.Name, seedCategory.UrlPath);
            await categoryRepository.InsertAsync(category, cancellationToken);
            await auditLogRepository.InsertAsync(
                new AuditLogEntry(admin.ExternalId, "Category.Create", category.Id, $"Tạo danh mục \"{category.Name}\"."),
                cancellationToken);

            var questions = new List<Question>();
            foreach (var seedQuestion in seedCategory.Questions)
            {
                var answers = seedQuestion.Answers
                    .Select(a => new Answer(ObjectId.GenerateNewId().ToString(), a.Content, a.IsCorrect))
                    .ToList();

                var question = new Question(null, seedQuestion.Content, seedQuestion.Type, seedQuestion.Level,
                    category.Id, answers, seedQuestion.Explain, ownerUserId: owner.ExternalId,
                    categoryName: category.Name);

                await questionRepository.InsertAsync(question, cancellationToken);
                questions.Add(question);
            }

            await auditLogRepository.InsertAsync(
                new AuditLogEntry(owner.ExternalId, "Question.Create", category.Id,
                    $"Thêm {questions.Count} câu hỏi vào danh mục \"{category.Name}\"."),
                cancellationToken);

            pools.Add(new CategoryPool(category, questions));
        }

        return pools;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Exams
    // ---------------------------------------------------------------------------------------------------------

    private sealed record SeededExam(ExamEntity Exam, CategoryPool Pool, IReadOnlyList<Question> DrawnQuestions, ClassRoom? Class);

    private static async Task<IReadOnlyList<SeededExam>> SeedExamsAsync(IExamRepository examRepository,
        IAuditLogRepository auditLogRepository, IReadOnlyList<CategoryPool> categoryPools,
        IReadOnlyList<User> instructors, IReadOnlyList<ClassRoom> classes, CancellationToken cancellationToken)
    {
        var owner = instructors[0];
        var seeded = new List<SeededExam>();

        for (var i = 0; i < categoryPools.Count; i++)
        {
            var pool = categoryPools[i];
            var assignedClass = classes[i % classes.Count];
            var drawnForThisCategory = DrawStandardComposition(pool.Questions);

            // 1) Đề đã xuất bản, giao riêng cho 1 lớp cụ thể - chỉ thành viên lớp đó thi được.
            var classExam = new ExamEntity(
                $"Bài kiểm tra {pool.Category.Name}", $"Đề kiểm tra dành cho {assignedClass.Name}",
                $"Bài thi gồm {StandardComposition.Sum(c => c.Count)} câu hỏi thuộc danh mục {pool.Category.Name}, " +
                $"giao riêng cho lớp {assignedClass.Name}.",
                TimeSpan.FromMinutes(45), Level.Medium, owner.ExternalId, pool.Category.Id, pool.Category.Name,
                isTimeRestricted: true, minimumPassingScore: 6.0m);
            classExam.ConfigureComposition(StandardComposition);
            if (i == 1) // 1 ví dụ giới hạn số lần thi lại - phải cấu hình trước khi Publish (chỉ sửa được ở Draft).
                classExam.ConfigureMaxAttempts(2);
            classExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow.AddDays(30));
            classExam.Publish();
            classExam.AssignToClass(assignedClass.Id);
            await examRepository.InsertAsync(classExam, cancellationToken);
            await auditLogRepository.InsertAsync(
                new AuditLogEntry(owner.ExternalId, "Exam.Publish", classExam.Id, $"Xuất bản đề thi \"{classExam.Name}\"."),
                cancellationToken);
            seeded.Add(new SeededExam(classExam, pool, drawnForThisCategory, assignedClass));

            // 2) Đề đã xuất bản, KHÔNG giao lớp nào - công khai cho mọi học viên (IsPublic = true).
            var publicExam = new ExamEntity(
                $"Đề luyện tập {pool.Category.Name}", $"Đề luyện tập tự do - {pool.Category.Name}",
                $"Bài luyện tập công khai gồm {StandardComposition.Sum(c => c.Count)} câu hỏi thuộc danh mục {pool.Category.Name}, " +
                "học viên nào cũng thi được, không giới hạn theo lớp.",
                TimeSpan.FromMinutes(45), Level.Medium, owner.ExternalId, pool.Category.Id, pool.Category.Name,
                isTimeRestricted: true, minimumPassingScore: 6.0m);
            publicExam.ConfigureComposition(StandardComposition);
            if (i == 2) // 1 ví dụ lịch phát hành ở TƯƠNG LAI - để kiểm tra UI trạng thái "chưa mở".
                publicExam.ScheduleAvailability(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(37));
            else
                publicExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow.AddDays(30));
            publicExam.Publish();
            await examRepository.InsertAsync(publicExam, cancellationToken);
            await auditLogRepository.InsertAsync(
                new AuditLogEntry(owner.ExternalId, "Exam.Publish", publicExam.Id, $"Xuất bản đề thi \"{publicExam.Name}\"."),
                cancellationToken);
            seeded.Add(new SeededExam(publicExam, pool, drawnForThisCategory, Class: null));

            // 3) Đề nháp - đang soạn, chưa xuất bản.
            var draftExam = new ExamEntity(
                $"Đề nháp {pool.Category.Name}", $"Đề đang soạn - {pool.Category.Name}",
                $"Bản nháp đề thi thuộc danh mục {pool.Category.Name}, chưa xuất bản.",
                TimeSpan.FromMinutes(30), Level.Easy, owner.ExternalId, pool.Category.Id, pool.Category.Name,
                isTimeRestricted: false, minimumPassingScore: 6.0m);
            draftExam.ConfigureComposition(DraftComposition);
            await examRepository.InsertAsync(draftExam, cancellationToken);

            // 4) Đề đã lưu trữ - ví dụ duy nhất ở danh mục cuối, để kiểm tra trạng thái Archived trên UI.
            if (i == categoryPools.Count - 1)
            {
                var archivedExam = new ExamEntity(
                    $"Đề thi cũ {pool.Category.Name} (khoá trước)", $"Đề thi khoá trước - {pool.Category.Name}",
                    $"Đề thi đã dùng cho khoá trước, hiện đã lưu trữ và không còn nhận bài làm mới.",
                    TimeSpan.FromMinutes(45), Level.Medium, owner.ExternalId, pool.Category.Id, pool.Category.Name,
                    isTimeRestricted: true, minimumPassingScore: 6.0m);
                archivedExam.ConfigureComposition(StandardComposition);
                archivedExam.ScheduleAvailability(DateTime.UtcNow.AddDays(-90), DateTime.UtcNow.AddDays(-60));
                archivedExam.Publish();
                archivedExam.Archive();
                await examRepository.InsertAsync(archivedExam, cancellationToken);
                await auditLogRepository.InsertAsync(
                    new AuditLogEntry(owner.ExternalId, "Exam.Archive", archivedExam.Id, $"Lưu trữ đề thi \"{archivedExam.Name}\"."),
                    cancellationToken);
            }
        }

        return seeded;
    }

    // Chọn cố định (không random thật) các câu hỏi khớp đúng số lượng từng ô trong StandardComposition từ pool
    // của danh mục - đủ dùng để build ExamResult mô phỏng, không cần gọi ExamQuestionPoolService (vốn dùng cho
    // luồng thi thật qua HTTP). Pool mỗi danh mục có dư nhiều hơn số cần rút cho mỗi ô nên luôn đủ.
    private static IReadOnlyList<Question> DrawStandardComposition(IReadOnlyList<Question> pool)
    {
        var result = new List<Question>();
        foreach (var cell in StandardComposition)
        {
            var candidates = pool.Where(q => q.Level == cell.Level && q.QuestionType == cell.QuestionType).Take(cell.Count);
            result.AddRange(candidates);
        }
        return result;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Exam results (bài làm đã hoàn thành, mô phỏng học viên thi thật)
    // ---------------------------------------------------------------------------------------------------------

    // Tỉ lệ trả lời đúng theo "học lực" - lặp vòng qua danh sách học viên để điểm số đa dạng, có cả đạt lẫn rớt
    // so với minimumPassingScore = 6.0.
    private static readonly double[] AbilityProfiles = [0.85, 0.65, 0.45, 0.75, 0.30];

    private static async Task SeedExamResultsAsync(IExamResultRepository examResultRepository,
        IReadOnlyList<SeededExam> exams, IReadOnlyList<ClassRoom> classes, IReadOnlyList<User> allStudents,
        CancellationToken cancellationToken)
    {
        var studentsByExternalId = allStudents.ToDictionary(s => s.ExternalId);
        var publicTestTakers = allStudents.Where(s => s.ExternalId is Student1ExternalId or Student2ExternalId or Student3ExternalId)
            .Concat(allStudents.Skip(6).Take(3)) // + 3 học viên "phụ" đầu tiên
            .Distinct()
            .ToList();

        var profileIndex = 0;
        foreach (var seededExam in exams)
        {
            if (seededExam.Exam.Status != ExamStatus.Published)
                continue; // đề nháp/lưu trữ không có bài làm

            var takers = seededExam.Class != null
                ? seededExam.Class.MemberUserIds.Select(id => studentsByExternalId.GetValueOrDefault(id)).Where(u => u != null).Cast<User>().ToList()
                : publicTestTakers;

            // Không phải ai cũng đã thi - lấy tối đa 4 người mỗi đề cho gần với thực tế (không phải 100% đã làm bài).
            foreach (var student in takers.Take(4))
            {
                var correctRatio = AbilityProfiles[profileIndex % AbilityProfiles.Length];
                profileIndex++;

                var startedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 45)).AddHours(-Rng.Next(0, 23));
                var takenDuration = TimeSpan.FromMinutes(Rng.Next(8, 40));

                var examResult = BuildFinishedExamResult(student, seededExam.Exam, seededExam.DrawnQuestions, correctRatio, startedAt, takenDuration);
                await examResultRepository.InsertAsync(examResult, cancellationToken);
            }
        }
    }

    private static ExamResult BuildFinishedExamResult(User student, ExamEntity exam, IReadOnlyList<Question> drawnQuestions,
        double correctRatio, DateTime startedAt, TimeSpan takenDuration)
    {
        var examResult = new ExamResult(student.ExternalId, exam.Id);
        examResult.SetExamTitle(exam.Name);
        examResult.SetUserInfo(student.Email, $"{student.FirstName} {student.LastName}".Trim());
        examResult.AssignQuestions(drawnQuestions.Select(q => q.Id).ToList());
        examResult.SetDuration(exam.IsTimeRestricted ? exam.Duration : null);

        foreach (var question in drawnQuestions)
        {
            var chosenIds = ChooseSimulatedAnswerIds(question, correctRatio);
            var answerResults = question.Answers
                .Select(a => new AnswerResult(a.Id, a.Content, chosenIds.Contains(a.Id), a.IsCorrect))
                .ToList();
            examResult.AddQuestionResult(new QuestionResult(question.Id, question.Content, question.QuestionType,
                question.Level, answerResults, question.Explain));
        }

        examResult.Finish(exam.MinimumPassingScore);

        // ExamStartDate/ExamFinishDate không có setter công khai (đúng chủ ý domain - người dùng thật không được
        // tự sửa thời điểm làm bài của mình). Chỉ dùng reflection Ở ĐÂY, trong seed data, để rải lịch sử làm bài
        // qua nhiều ngày cho Dashboard/báo cáo có dữ liệu thật để vẽ xu hướng - không áp dụng cách này ở nơi khác.
        typeof(ExamResult).GetProperty(nameof(ExamResult.ExamStartDate))!.SetValue(examResult, startedAt);
        typeof(ExamResult).GetProperty(nameof(ExamResult.ExamFinishDate))!.SetValue(examResult, startedAt + takenDuration);

        return examResult;
    }

    private static HashSet<string> ChooseSimulatedAnswerIds(Question question, double correctRatio)
    {
        var correctIds = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToHashSet();

        if (Rng.NextDouble() < correctRatio)
            return correctIds;

        // Trả lời sai có chủ đích: một đáp án (nếu là câu 1 đáp án đúng) hoặc thiếu 1 đáp án đúng (nếu nhiều đáp án).
        var wrongOptions = question.Answers.Where(a => !a.IsCorrect).Select(a => a.Id).ToList();
        if (question.QuestionType == QuestionType.SingleSelection || correctIds.Count <= 1)
            return wrongOptions.Count > 0 ? [wrongOptions[Rng.Next(wrongOptions.Count)]] : correctIds;

        return correctIds.Skip(1).ToHashSet();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Question bank content
    // ---------------------------------------------------------------------------------------------------------

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
            new SeedQuestion("Kiểu dữ liệu nào trong C# dùng để lưu giá trị đúng/sai?",
                QuestionType.SingleSelection, Level.Easy,
                "bool chỉ nhận 1 trong 2 giá trị true/false.",
                [
                    new SeedAnswer("bool", true),
                    new SeedAnswer("byte", false),
                    new SeedAnswer("char", false),
                    new SeedAnswer("int", false),
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
            new SeedQuestion("Từ khóa `yield return` trong C# dùng để làm gì?",
                QuestionType.SingleSelection, Level.Medium,
                "yield return tạo iterator, trả về từng phần tử một cách lười (deferred/lazy execution).",
                [
                    new SeedAnswer("Tạo iterator trả về từng phần tử một cách lười (lazy)", true),
                    new SeedAnswer("Dừng chương trình ngay lập tức", false),
                    new SeedAnswer("Khai báo một hằng số", false),
                    new SeedAnswer("Ép kiểu dữ liệu", false),
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
            new SeedQuestion("Những phát biểu nào sau đây đúng về `async`/`await` trong C#?",
                QuestionType.MultipleSelection, Level.Medium,
                "Phương thức async trả về Task/Task<T>, await không chặn luồng gọi; async void chỉ nên dùng cho event handler.",
                [
                    new SeedAnswer("Phương thức async thường trả về Task hoặc Task<T>", true),
                    new SeedAnswer("await không chặn (block) luồng đang gọi", true),
                    new SeedAnswer("async luôn tạo ra một thread mới", false),
                    new SeedAnswer("Có thể dùng async void cho event handler", true),
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
            new SeedQuestion("Sự khác biệt chính giữa `IEnumerable<T>` và `IQueryable<T>` là gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "IQueryable dịch biểu thức LINQ thành truy vấn thực thi tại nguồn dữ liệu (vd SQL); IEnumerable thực thi trong bộ nhớ.",
                [
                    new SeedAnswer("IQueryable dịch truy vấn để thực thi tại nguồn dữ liệu, IEnumerable thực thi trong bộ nhớ", true),
                    new SeedAnswer("Cả hai hoàn toàn giống nhau, chỉ khác tên gọi", false),
                    new SeedAnswer("IEnumerable chỉ dùng được với mảng, IQueryable dùng được với List", false),
                    new SeedAnswer("IQueryable không hỗ trợ vòng lặp foreach", false),
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
            new SeedQuestion("Lệnh SQL nào dùng để xoá toàn bộ dữ liệu trong bảng nhưng vẫn giữ nguyên cấu trúc bảng?",
                QuestionType.SingleSelection, Level.Easy,
                "TRUNCATE TABLE xoá toàn bộ dòng nhưng giữ nguyên schema của bảng.",
                [
                    new SeedAnswer("TRUNCATE TABLE", true),
                    new SeedAnswer("DROP TABLE", false),
                    new SeedAnswer("DELETE DATABASE", false),
                    new SeedAnswer("ALTER TABLE", false),
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
            new SeedQuestion("Chỉ mục (Index) trong cơ sở dữ liệu chủ yếu giúp cải thiện điều gì?",
                QuestionType.SingleSelection, Level.Medium,
                "Index tăng tốc độ đọc/truy vấn, đánh đổi bằng chi phí ghi (insert/update) cao hơn.",
                [
                    new SeedAnswer("Tốc độ truy vấn (đọc), đánh đổi bằng tốc độ ghi", true),
                    new SeedAnswer("Dung lượng ổ đĩa còn trống", false),
                    new SeedAnswer("Tính bảo mật của dữ liệu", false),
                    new SeedAnswer("Số lượng kết nối đồng thời tối đa", false),
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
            new SeedQuestion("Những phát biểu nào đúng về Transaction trong cơ sở dữ liệu?",
                QuestionType.MultipleSelection, Level.Medium,
                "COMMIT lưu vĩnh viễn thay đổi, ROLLBACK huỷ thay đổi chưa commit, có thể lồng nhiều câu lệnh trong 1 transaction.",
                [
                    new SeedAnswer("COMMIT lưu vĩnh viễn thay đổi", true),
                    new SeedAnswer("ROLLBACK huỷ toàn bộ thay đổi chưa commit", true),
                    new SeedAnswer("Transaction luôn tự động commit ngay khi có lỗi", false),
                    new SeedAnswer("Có thể gộp nhiều câu lệnh trong cùng 1 transaction", true),
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
            new SeedQuestion("Trong MongoDB, cơ chế nào đảm bảo tính nhất quán khi ghi dữ liệu vào 1 document?",
                QuestionType.SingleSelection, Level.Difficult,
                "MongoDB đảm bảo mỗi thao tác ghi trên 1 document là nguyên tử (document-level atomicity).",
                [
                    new SeedAnswer("Tính nguyên tử ở cấp document (document-level atomicity)", true),
                    new SeedAnswer("Luôn khoá toàn bộ collection khi ghi", false),
                    new SeedAnswer("Không có cơ chế đảm bảo nào, dữ liệu có thể mất bất kỳ lúc nào", false),
                    new SeedAnswer("Chỉ đảm bảo khi chạy ở chế độ standalone, không có replica set", false),
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
            new SeedQuestion("Địa chỉ IP phiên bản nào có độ dài 32 bit?",
                QuestionType.SingleSelection, Level.Easy,
                "IPv4 có độ dài 32 bit; IPv6 dài 128 bit.",
                [
                    new SeedAnswer("IPv4", true),
                    new SeedAnswer("IPv6", false),
                    new SeedAnswer("IPv5", false),
                    new SeedAnswer("IPv8", false),
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
            new SeedQuestion("Giao thức nào dùng để cấp phát địa chỉ IP động cho thiết bị trong mạng?",
                QuestionType.SingleSelection, Level.Medium,
                "DHCP tự động cấp phát địa chỉ IP cho thiết bị khi gia nhập mạng.",
                [
                    new SeedAnswer("DHCP", true),
                    new SeedAnswer("DNS", false),
                    new SeedAnswer("FTP", false),
                    new SeedAnswer("ARP", false),
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
            new SeedQuestion("Những đặc điểm nào đúng về giao thức UDP?",
                QuestionType.MultipleSelection, Level.Medium,
                "UDP không đảm bảo thứ tự/không bắt tay ba bước, nhưng nhanh hơn TCP do ít overhead; không có ACK như TCP.",
                [
                    new SeedAnswer("Không đảm bảo thứ tự gói tin", true),
                    new SeedAnswer("Không có bắt tay ba bước (three-way handshake)", true),
                    new SeedAnswer("Có cơ chế xác nhận (ACK) như TCP", false),
                    new SeedAnswer("Tốc độ truyền nhanh hơn TCP do ít overhead", true),
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
            new SeedQuestion("Kỹ thuật NAT (Network Address Translation) chủ yếu giải quyết vấn đề gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "NAT cho phép nhiều thiết bị trong mạng nội bộ dùng chung 1 địa chỉ IP công cộng.",
                [
                    new SeedAnswer("Cho nhiều thiết bị nội bộ dùng chung 1 địa chỉ IP công cộng", true),
                    new SeedAnswer("Mã hoá toàn bộ lưu lượng mạng", false),
                    new SeedAnswer("Tăng băng thông đường truyền", false),
                    new SeedAnswer("Phân giải tên miền thành địa chỉ IP", false),
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
            new SeedQuestion("Cấu trúc dữ liệu nào hoạt động theo nguyên tắc vào trước ra trước (FIFO)?",
                QuestionType.SingleSelection, Level.Easy,
                "Queue hoạt động theo FIFO.",
                [
                    new SeedAnswer("Queue", true),
                    new SeedAnswer("Stack", false),
                    new SeedAnswer("Tree", false),
                    new SeedAnswer("Hash Table", false),
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
            new SeedQuestion("Bảng băm (Hash Table) thường xử lý va chạm (collision) bằng kỹ thuật nào?",
                QuestionType.SingleSelection, Level.Medium,
                "Chaining (nối danh sách liên kết tại mỗi bucket) là kỹ thuật xử lý va chạm phổ biến.",
                [
                    new SeedAnswer("Chaining (danh sách liên kết tại mỗi bucket)", true),
                    new SeedAnswer("Xoá bớt phần tử cũ trong bảng", false),
                    new SeedAnswer("Không xử lý, chấp nhận ghi đè", false),
                    new SeedAnswer("Chuyển toàn bộ sang mảng đã sắp xếp", false),
                ]),
            new SeedQuestion("Những thuật toán nào sau đây thuộc nhóm duyệt/tìm đường trên đồ thị?",
                QuestionType.MultipleSelection, Level.Medium,
                "BFS, DFS, Dijkstra đều là thuật toán trên đồ thị; Quick Sort là thuật toán sắp xếp.",
                [
                    new SeedAnswer("BFS (Breadth-First Search)", true),
                    new SeedAnswer("DFS (Depth-First Search)", true),
                    new SeedAnswer("Quick Sort", false),
                    new SeedAnswer("Dijkstra", true),
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
            new SeedQuestion("Bài toán tìm đường đi ngắn nhất có cạnh trọng số âm nên dùng thuật toán nào thay vì Dijkstra?",
                QuestionType.SingleSelection, Level.Difficult,
                "Bellman-Ford xử lý được trọng số âm (và phát hiện chu trình âm); Dijkstra thì không.",
                [
                    new SeedAnswer("Bellman-Ford", true),
                    new SeedAnswer("Prim", false),
                    new SeedAnswer("Kruskal", false),
                    new SeedAnswer("Selection Sort", false),
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
            new SeedQuestion("Git là công cụ dùng để làm gì?",
                QuestionType.SingleSelection, Level.Easy,
                "Git là hệ thống quản lý phiên bản (version control) mã nguồn.",
                [
                    new SeedAnswer("Quản lý phiên bản mã nguồn (version control)", true),
                    new SeedAnswer("Biên dịch mã nguồn thành file thực thi", false),
                    new SeedAnswer("Thiết kế giao diện người dùng", false),
                    new SeedAnswer("Quản lý cơ sở dữ liệu", false),
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
            new SeedQuestion("Design pattern nào đảm bảo một lớp chỉ có duy nhất 1 instance trong suốt vòng đời ứng dụng?",
                QuestionType.SingleSelection, Level.Medium,
                "Singleton đảm bảo chỉ có 1 instance duy nhất và cung cấp điểm truy cập toàn cục tới nó.",
                [
                    new SeedAnswer("Singleton", true),
                    new SeedAnswer("Factory", false),
                    new SeedAnswer("Observer", false),
                    new SeedAnswer("Adapter", false),
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
            new SeedQuestion("Những nguyên tắc nào sau đây thuộc mô hình kiến trúc REST?",
                QuestionType.MultipleSelection, Level.Medium,
                "REST gồm Stateless, Client-Server, Uniform Interface... không bắt buộc định dạng dữ liệu cụ thể như XML.",
                [
                    new SeedAnswer("Stateless (không lưu trạng thái phiên trên server)", true),
                    new SeedAnswer("Client-Server tách biệt", true),
                    new SeedAnswer("Bắt buộc dùng định dạng XML", false),
                    new SeedAnswer("Uniform Interface (giao diện thống nhất)", true),
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
            new SeedQuestion("Trong kiến trúc CQRS, mục đích chính của việc tách Command và Query là gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "CQRS tách biệt luồng ghi (Command) và luồng đọc (Query) để mỗi phía có thể tối ưu/mở rộng độc lập.",
                [
                    new SeedAnswer("Tách luồng ghi và luồng đọc để tối ưu độc lập từng phía", true),
                    new SeedAnswer("Giảm số lượng bảng trong cơ sở dữ liệu", false),
                    new SeedAnswer("Bắt buộc phải dùng NoSQL", false),
                    new SeedAnswer("Thay thế hoàn toàn cho kiến trúc microservices", false),
                ]),
        ]),
        new SeedCategory("Trí tuệ nhân tạo & Machine Learning", "tri-tue-nhan-tao-machine-learning",
        [
            new SeedQuestion("Học máy có giám sát (Supervised Learning) sử dụng loại dữ liệu nào để huấn luyện?",
                QuestionType.SingleSelection, Level.Easy,
                "Supervised Learning cần dữ liệu đã được gán nhãn (label) trước để mô hình học ánh xạ đầu vào -> đầu ra.",
                [
                    new SeedAnswer("Dữ liệu đã được gán nhãn", true),
                    new SeedAnswer("Dữ liệu hoàn toàn ngẫu nhiên, không cần xử lý", false),
                    new SeedAnswer("Chỉ cần dữ liệu dạng hình ảnh", false),
                    new SeedAnswer("Không cần dữ liệu, chỉ cần luật do lập trình viên viết tay", false),
                ]),
            new SeedQuestion("Mạng nơ-ron nhân tạo (Neural Network) được lấy cảm hứng từ đâu?",
                QuestionType.SingleSelection, Level.Easy,
                "Neural Network mô phỏng cách các nơ-ron sinh học trong não bộ kết nối và truyền tín hiệu.",
                [
                    new SeedAnswer("Cấu trúc nơ-ron sinh học trong não bộ", true),
                    new SeedAnswer("Cấu trúc bảng trong cơ sở dữ liệu quan hệ", false),
                    new SeedAnswer("Sơ đồ mạng máy tính LAN", false),
                    new SeedAnswer("Cấu trúc cây thư mục hệ điều hành", false),
                ]),
            new SeedQuestion("Overfitting là hiện tượng gì trong Machine Learning?",
                QuestionType.SingleSelection, Level.Easy,
                "Overfitting là khi mô hình học quá khớp với dữ liệu huấn luyện, kém tổng quát hoá trên dữ liệu mới.",
                [
                    new SeedAnswer("Mô hình học quá khớp dữ liệu huấn luyện, kém tổng quát hoá trên dữ liệu mới", true),
                    new SeedAnswer("Mô hình học quá nhanh nên tốn ít tài nguyên", false),
                    new SeedAnswer("Mô hình không học được gì từ dữ liệu", false),
                    new SeedAnswer("Dữ liệu huấn luyện bị thiếu định dạng", false),
                ]),
            new SeedQuestion("Hàm kích hoạt (activation function) nào thường dùng cho lớp ẩn để hạn chế vanishing gradient tốt hơn Sigmoid?",
                QuestionType.SingleSelection, Level.Medium,
                "ReLU (Rectified Linear Unit) ít bị vanishing gradient hơn Sigmoid nên phổ biến cho lớp ẩn.",
                [
                    new SeedAnswer("ReLU", true),
                    new SeedAnswer("Softmax", false),
                    new SeedAnswer("Linear", false),
                    new SeedAnswer("Step function", false),
                ]),
            new SeedQuestion("Thuật toán nào thường dùng để tối ưu (cập nhật) trọng số trong mạng nơ-ron khi huấn luyện?",
                QuestionType.SingleSelection, Level.Medium,
                "Gradient Descent (và các biến thể như Adam, SGD) cập nhật trọng số theo hướng giảm hàm mất mát.",
                [
                    new SeedAnswer("Gradient Descent", true),
                    new SeedAnswer("Binary Search", false),
                    new SeedAnswer("Quick Sort", false),
                    new SeedAnswer("Depth-First Search", false),
                ]),
            new SeedQuestion("Trong bài toán phân loại, ma trận nhầm lẫn (confusion matrix) dùng để làm gì?",
                QuestionType.SingleSelection, Level.Medium,
                "Confusion matrix thống kê số lượng dự đoán đúng/sai theo từng lớp, giúp đánh giá hiệu năng mô hình.",
                [
                    new SeedAnswer("Đánh giá hiệu năng mô hình qua số dự đoán đúng/sai theo từng lớp", true),
                    new SeedAnswer("Giảm kích thước tập dữ liệu huấn luyện", false),
                    new SeedAnswer("Trực quan hoá cấu trúc mạng nơ-ron", false),
                    new SeedAnswer("Mã hoá dữ liệu văn bản thành số", false),
                ]),
            new SeedQuestion("Những kỹ thuật nào sau đây giúp giảm overfitting khi huấn luyện mô hình?",
                QuestionType.MultipleSelection, Level.Medium,
                "Regularization, Dropout, Data Augmentation đều giúp giảm overfitting; tăng epoch vô hạn thường làm overfitting nặng hơn.",
                [
                    new SeedAnswer("Regularization (L1/L2)", true),
                    new SeedAnswer("Dropout", true),
                    new SeedAnswer("Tăng số epoch huấn luyện đến vô hạn", false),
                    new SeedAnswer("Data Augmentation (tăng cường dữ liệu)", true),
                ]),
            new SeedQuestion("Những kỹ thuật/mô hình nào sau đây thuộc nhóm học không giám sát (Unsupervised Learning)?",
                QuestionType.MultipleSelection, Level.Medium,
                "K-Means, PCA, Hierarchical Clustering không cần nhãn; Linear Regression là học có giám sát.",
                [
                    new SeedAnswer("K-Means Clustering", true),
                    new SeedAnswer("PCA (Principal Component Analysis)", true),
                    new SeedAnswer("Hierarchical Clustering", true),
                    new SeedAnswer("Linear Regression", false),
                ]),
            new SeedQuestion("Cơ chế Attention trong mô hình Transformer chủ yếu giải quyết vấn đề gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "Attention cho phép mô hình tập trung vào các phần liên quan của chuỗi đầu vào bất kể khoảng cách, khắc phục hạn chế phụ thuộc xa của RNN.",
                [
                    new SeedAnswer("Cho phép mô hình tập trung vào phần liên quan của chuỗi đầu vào dù ở xa nhau", true),
                    new SeedAnswer("Giảm số lượng tham số của mô hình xuống 0", false),
                    new SeedAnswer("Thay thế hoàn toàn nhu cầu dữ liệu huấn luyện", false),
                    new SeedAnswer("Chỉ áp dụng được cho dữ liệu hình ảnh", false),
                ]),
            new SeedQuestion("Kỹ thuật Backpropagation dùng để làm gì trong huấn luyện mạng nơ-ron?",
                QuestionType.SingleSelection, Level.Difficult,
                "Backpropagation tính gradient của hàm mất mát theo từng trọng số bằng quy tắc chuỗi (chain rule) để cập nhật trọng số.",
                [
                    new SeedAnswer("Tính gradient của hàm mất mát theo từng trọng số bằng chain rule", true),
                    new SeedAnswer("Khởi tạo ngẫu nhiên toàn bộ trọng số ban đầu", false),
                    new SeedAnswer("Chuẩn hoá dữ liệu đầu vào về khoảng [0,1]", false),
                    new SeedAnswer("Chia tập dữ liệu thành train/test", false),
                ]),
        ]),
        new SeedCategory("An toàn thông tin", "an-toan-thong-tin",
        [
            new SeedQuestion("Kỹ thuật nào dùng để chuyển đổi dữ liệu thành dạng không đọc được nếu không có khóa giải mã?",
                QuestionType.SingleSelection, Level.Easy,
                "Mã hoá (Encryption) biến dữ liệu gốc thành dạng không đọc được nếu thiếu khoá giải mã.",
                [
                    new SeedAnswer("Mã hóa (Encryption)", true),
                    new SeedAnswer("Nén dữ liệu (Compression)", false),
                    new SeedAnswer("Sao lưu (Backup)", false),
                    new SeedAnswer("Định dạng lại ổ đĩa (Format)", false),
                ]),
            new SeedQuestion("SQL Injection là dạng tấn công khai thác lỗ hổng ở đâu?",
                QuestionType.SingleSelection, Level.Easy,
                "SQL Injection khai thác việc câu truy vấn SQL không kiểm tra/escape đầu vào của người dùng đúng cách.",
                [
                    new SeedAnswer("Câu truy vấn SQL không được kiểm tra/escape đầu vào đúng cách", true),
                    new SeedAnswer("Card mạng của máy chủ", false),
                    new SeedAnswer("Bộ nhớ RAM vật lý của máy chủ", false),
                    new SeedAnswer("Nguồn điện của trung tâm dữ liệu", false),
                ]),
            new SeedQuestion("HTTPS khác HTTP ở điểm nào?",
                QuestionType.SingleSelection, Level.Easy,
                "HTTPS mã hóa dữ liệu truyền tải giữa client và server bằng TLS/SSL.",
                [
                    new SeedAnswer("HTTPS mã hóa dữ liệu truyền tải bằng TLS/SSL", true),
                    new SeedAnswer("HTTPS chạy nhanh hơn gấp đôi HTTP", false),
                    new SeedAnswer("HTTPS chỉ dùng được trên mạng nội bộ", false),
                    new SeedAnswer("HTTPS không cần địa chỉ IP", false),
                ]),
            new SeedQuestion("Kỹ thuật nào giúp xác minh danh tính người dùng qua nhiều lớp bảo mật (vd: mật khẩu + mã OTP)?",
                QuestionType.SingleSelection, Level.Medium,
                "Xác thực đa yếu tố (MFA) yêu cầu từ 2 yếu tố xác minh trở lên, giảm rủi ro khi lộ mật khẩu.",
                [
                    new SeedAnswer("Xác thực đa yếu tố (Multi-Factor Authentication)", true),
                    new SeedAnswer("Mã hoá đối xứng (Symmetric Encryption)", false),
                    new SeedAnswer("Tường lửa (Firewall)", false),
                    new SeedAnswer("VPN", false),
                ]),
            new SeedQuestion("Thuật toán băm (hash) như SHA-256 thường dùng để làm gì trong bảo mật?",
                QuestionType.SingleSelection, Level.Medium,
                "Hash một chiều thường dùng để lưu mật khẩu (không lưu plaintext) và kiểm tra tính toàn vẹn dữ liệu.",
                [
                    new SeedAnswer("Lưu trữ mật khẩu một chiều và kiểm tra tính toàn vẹn dữ liệu", true),
                    new SeedAnswer("Nén file để giảm dung lượng", false),
                    new SeedAnswer("Tăng tốc độ truy vấn cơ sở dữ liệu", false),
                    new SeedAnswer("Mã hoá 2 chiều để giải mã lại dữ liệu gốc", false),
                ]),
            new SeedQuestion("Tấn công XSS (Cross-Site Scripting) khai thác lỗ hổng ở đâu?",
                QuestionType.SingleSelection, Level.Medium,
                "XSS chèn mã script độc hại vào trang web mà không được escape khi hiển thị cho người dùng khác.",
                [
                    new SeedAnswer("Chèn mã script độc hại vào trang web không được escape khi hiển thị", true),
                    new SeedAnswer("Khai thác lỗ hổng phần cứng CPU", false),
                    new SeedAnswer("Khai thác cấu hình DNS sai", false),
                    new SeedAnswer("Khai thác giao thức FTP", false),
                ]),
            new SeedQuestion("Những biện pháp nào sau đây giúp phòng chống SQL Injection?",
                QuestionType.MultipleSelection, Level.Medium,
                "Câu lệnh tham số hoá, validate đầu vào, và nguyên tắc least privilege cho tài khoản DB đều giúp phòng chống SQL Injection.",
                [
                    new SeedAnswer("Sử dụng câu lệnh tham số hóa (parameterized query)", true),
                    new SeedAnswer("Escape/validate đầu vào người dùng", true),
                    new SeedAnswer("Tắt hẳn kết nối tới cơ sở dữ liệu", false),
                    new SeedAnswer("Áp dụng nguyên tắc quyền tối thiểu (least privilege) cho tài khoản DB", true),
                ]),
            new SeedQuestion("Những yếu tố nào thuộc bộ ba CIA Triad trong an toàn thông tin?",
                QuestionType.MultipleSelection, Level.Medium,
                "CIA Triad gồm Confidentiality, Integrity, Availability - không bao gồm Authentication (dù liên quan mật thiết).",
                [
                    new SeedAnswer("Confidentiality (bảo mật)", true),
                    new SeedAnswer("Integrity (toàn vẹn)", true),
                    new SeedAnswer("Availability (sẵn sàng)", true),
                    new SeedAnswer("Authentication (xác thực)", false),
                ]),
            new SeedQuestion("Kỹ thuật tấn công Man-in-the-Middle (MITM) hoạt động theo nguyên lý nào?",
                QuestionType.SingleSelection, Level.Difficult,
                "Kẻ tấn công chen vào giữa 2 bên giao tiếp để nghe lén hoặc giả mạo dữ liệu mà 2 bên không hay biết.",
                [
                    new SeedAnswer("Chen vào giữa 2 bên giao tiếp để nghe lén/giả mạo mà không bị phát hiện", true),
                    new SeedAnswer("Gửi hàng loạt yêu cầu để làm sập máy chủ", false),
                    new SeedAnswer("Đoán mật khẩu bằng cách thử tất cả tổ hợp", false),
                    new SeedAnswer("Lừa người dùng tự tiết lộ thông tin qua email giả mạo", false),
                ]),
            new SeedQuestion("Zero-day vulnerability là gì?",
                QuestionType.SingleSelection, Level.Difficult,
                "Zero-day là lỗ hổng bảo mật chưa được nhà phát triển biết đến hoặc chưa có bản vá tại thời điểm bị khai thác.",
                [
                    new SeedAnswer("Lỗ hổng bảo mật chưa được biết đến/chưa có bản vá khi bị khai thác", true),
                    new SeedAnswer("Lỗ hổng đã được vá từ 0 ngày trước", false),
                    new SeedAnswer("Một loại virus chỉ hoạt động vào lúc 0 giờ", false),
                    new SeedAnswer("Lỗi cấu hình tường lửa mặc định", false),
                ]),
        ]),
    ];
}
