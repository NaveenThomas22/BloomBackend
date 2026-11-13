-- =============================================================================
-- Migration 016: Seed Habit Categories and Notification Categories
-- Phase 8 (8.4, 8.5) - Bloom (My CAT Journey)
-- =============================================================================

INSERT INTO habit_categories 
(id, user_id, category_code, category_name, description, icon, color, is_active, display_order)
VALUES
(1, NULL, 'STUDY', 'CAT Study', 'Core syllabus study, problem sets, and concepts', 'book-open', '#405B49', 1, 1),
(2, NULL, 'REVISION', 'Spaced Revision', 'Reviewing formulas, flashcards, and completed modules', 'repeat', '#2D6A4F', 1, 2),
(3, NULL, 'MOCK_PRACTICE', 'Mock & Practice', 'Timed CAT practice, sectional tests, and test-taking discipline', 'award', '#DDA15E', 1, 3),
(4, NULL, 'HEALTH_WELLNESS', 'Health & Fitness', 'Exercise, adequate sleep, hydration, and physical stamina', 'activity', '#74C69D', 1, 4),
(5, NULL, 'MINDSET_ROUTINE', 'Mindset & Discipline', 'Daily reflection, morning routine, mindfulness, and consistency', 'compass', '#F4A261', 1, 5)
ON DUPLICATE KEY UPDATE 
    category_name = VALUES(category_name),
    description = VALUES(description),
    icon = VALUES(icon),
    color = VALUES(color),
    display_order = VALUES(display_order);

INSERT INTO notification_categories
(id, category_code, category_name, description, icon, color, default_enabled)
VALUES
(1, 'REVISION_DUE', 'Revision Due', 'Daily reminders for scheduled spaced repetition revisions', 'repeat', '#2D6A4F', 1),
(2, 'MORNING_REFLECTION', 'Morning Intention', 'Start your day with focused intentions and top priorities', 'sun', '#DDA15E', 1),
(3, 'EVENING_REFLECTION', 'Evening Reflection', 'Wrap up your day, log what went well, and practice gratitude', 'moon', '#457B9D', 1),
(4, 'HABIT_REMINDER', 'Habit Check-in', 'Gentle reminder to log your daily habits before day end', 'check-circle', '#74C69D', 1),
(5, 'DAILY_GOAL', 'Daily Target', 'Progress update on daily study time and questions solved targets', 'target', '#BC6C25', 1),
(6, 'MILESTONE_ACHIEVED', 'Milestone Celebration', 'Celebration alerts when milestones are reached', 'award', '#E76F51', 1),
(7, 'SYSTEM_UPDATE', 'System & Motivation', 'Curated CAT preparation insights, reminders, and motivational tips', 'bell', '#A8BDA5', 1)
ON DUPLICATE KEY UPDATE
    category_name = VALUES(category_name),
    description = VALUES(description),
    icon = VALUES(icon),
    color = VALUES(color),
    default_enabled = VALUES(default_enabled);

