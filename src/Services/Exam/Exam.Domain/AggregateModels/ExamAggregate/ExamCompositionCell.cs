using Exam.Contracts;
using Exam.Domain.Exceptions;

namespace Exam.Domain.AggregateModels.ExamAggregate;

// Một ô trong ma trận cấu trúc đề thi: rút ngẫu nhiên đúng Count câu hỏi thuộc mức độ Level + loại
// QuestionType (trong đúng môn học của đề). Count có thể bằng 0 (ô trống); ràng buộc tổng > 0 nằm ở
// Exam.ConfigureComposition.
public class ExamCompositionCell
{
    public Level Level { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public int Count { get; private set; }

    private ExamCompositionCell()
    {
    }

    public ExamCompositionCell(Level level, QuestionType questionType, int count)
    {
        if (count < 0)
            throw new ExamDomainException("Composition cell count must not be negative.");

        Level = level;
        QuestionType = questionType;
        Count = count;
    }
}
