-- =============================================================================
-- Migration 014: Seed Journal Categories & Reflection Moods
-- Phase 8.1 - Bloom (My CAT Journey)
-- =============================================================================

INSERT INTO journal_categories 
(id, user_id, category_code, category_name, description, icon, color, is_active, display_order)
VALUES
(1, NULL, 'PERSONAL_THOUGHTS', 'Personal Thoughts', 'Daily personal journaling and stream of consciousness', 'book-open', '#A8BDA5', 1, 1),
(2, NULL, 'CAT_PREPARATION', 'CAT Preparation', 'Reflections on CAT preparation, mock tests, and strategy', 'graduation-cap', '#405B49', 1, 2),
(3, NULL, 'ACHIEVEMENTS', 'Achievements', 'Celebrating breakthroughs, wins, and solved milestones', 'award', '#DDA15E', 1, 3),
(4, NULL, 'CHALLENGES', 'Challenges', 'Difficult concepts, mock test setbacks, and barriers', 'mountain', '#BC6C25', 1, 4),
(5, NULL, 'GRATITUDE', 'Gratitude', 'Daily appreciation and positive perspectives', 'heart', '#E9C9C5', 1, 5),
(6, NULL, 'IDEAS', 'Ideas', 'Preparation strategies, time management insights, and solutions', 'lightbulb', '#F4A261', 1, 6),
(7, NULL, 'LESSONS_LEARNED', 'Lessons Learned', 'Key learnings, exam tricks, and error analysis takeaways', 'sparkles', '#457B9D', 1, 7),
(8, NULL, 'PERSONAL_GROWTH', 'Personal Growth', 'Discipline, consistency, resilience, and mindset shifts', 'trending-up', '#2D6A4F', 1, 8)
ON DUPLICATE KEY UPDATE 
    category_name = VALUES(category_name),
    description = VALUES(description),
    icon = VALUES(icon),
    color = VALUES(color),
    display_order = VALUES(display_order);

INSERT INTO reflection_moods
(id, mood_code, mood_name, emoji, color, description, display_order, is_active)
VALUES
(1, 'HAPPY', 'Happy', '😊', '#74C69D', 'Joyful, contented, and optimistic', 1, 1),
(2, 'CALM', 'Calm', '😌', '#A8BDA5', 'Peaceful, balanced, and steady', 2, 1),
(3, 'MOTIVATED', 'Motivated', '🔥', '#E76F51', 'Energized, determined, and ready to conquer goals', 3, 1),
(4, 'FOCUSED', 'Focused', '🎯', '#2D6A4F', 'Deep concentration and productive flow', 4, 1),
(5, 'NEUTRAL', 'Neutral', '😐', '#BDB2FF', 'Even-keeled and steady routine', 5, 1),
(6, 'TIRED', 'Tired', '😴', '#936639', 'Low energy, needing sleep or rest', 6, 1),
(7, 'STRESSED', 'Stressed', '😰', '#F4A261', 'Under pressure or feeling overwhelmed', 7, 1),
(8, 'FRUSTRATED', 'Frustrated', '😤', '#D90429', 'Annoyed by difficulties or stalled progress', 8, 1),
(9, 'SAD', 'Sad', '😔', '#457B9D', 'Downcast, low spirits, or disappointed', 9, 1)
ON DUPLICATE KEY UPDATE
    mood_name = VALUES(mood_name),
    emoji = VALUES(emoji),
    color = VALUES(color),
    description = VALUES(description),
    display_order = VALUES(display_order);

