-- =============================================================================
-- Migration 008: Seed Default Spaced Repetition Policies
-- Phase 6 - Bloom (My CAT Journey)
-- =============================================================================

INSERT INTO revision_policies (user_id, policy_name, description, intervals_csv, is_system, is_active, created_at_utc, updated_at_utc)
VALUES
(
    NULL,
    'Standard CAT Spaced Repetition',
    'Standard five-tier retention curve (Day 1, 3, 7, 14, 30) designed for optimal formula, concept, and pattern retention before mock season.',
    '1,3,7,14,30',
    1,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
),
(
    NULL,
    'Intensive High-Frequency Review',
    'Accelerated repetition cycle (Day 1, 2, 5, 10, 21) ideal for high-difficulty Quantitative Aptitude chapters or complex DILR arrangements.',
    '1,2,5,10,21',
    1,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
),
(
    NULL,
    'Long-Term Maintenance Strategy',
    'Extended spaced intervals (Day 3, 7, 14, 30, 60) for mastered foundational concepts requiring periodic refreshing without excessive daily load.',
    '3,7,14,30,60',
    1,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE updated_at_utc = UTC_TIMESTAMP();

