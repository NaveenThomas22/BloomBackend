namespace Bloom.Core.Enums;

public enum QuestionType
{
    MCQ,
    TITA
}

public enum QuestionStatus
{
    Draft,
    Published,
    Archived
}

public enum PracticeMode
{
    Topic,
    Subject,
    Difficulty,
    Custom,
    DILRSet,
    VARCPassage
}

public enum PracticeSessionStatus
{
    InProgress,
    Completed,
    Abandoned
}

public enum ExamType
{
    FullMock,
    SectionalMock,
    Custom
}

public enum ExamAttemptStatus
{
    Created,
    InProgress,
    Submitted,
    AutoSubmitted,
    Expired,
    Abandoned
}

public enum AnswerStatus
{
    NotVisited,
    NotAnswered,
    Answered,
    MarkedForReview,
    AnsweredAndMarkedForReview
}

public enum AttemptSourceType
{
    Practice,
    MockExam
}

