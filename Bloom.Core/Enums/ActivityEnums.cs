namespace Bloom.Core.Enums;

public enum CategoryGroup
{
    Study,
    RestAndSleep,
    MealsAndBreaks,
    Personal,
    WorkAndCommute,
    HealthAndFitness,
    LeisureAndSocial,
    Other
}

public enum ActivityStatus
{
    Running,
    Completed,
    Cancelled
}

public enum EntrySource
{
    Timer,
    Manual,
    PlanConverted
}

public enum PlanStatus
{
    Scheduled,
    InProgress,
    Completed,
    Skipped,
    Cancelled
}

public enum TimerState
{
    Running,
    Paused
}

public enum StudyMode
{
    Theory,
    Practice,
    Revision,
    SectionalQuiz,
    General
}

