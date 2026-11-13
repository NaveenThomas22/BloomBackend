-- =============================================================================
-- Migration 006: Seed Shared Default Activity Categories
-- Phase 5 - Bloom (My CAT Journey)
-- =============================================================================

INSERT INTO activity_categories 
(id, user_id, category_code, category_name, description, icon, color, category_group, is_active, display_order)
VALUES
-- 1. Rest & Sleep
(1, NULL, 'SLEEP', 'Night Sleep', 'Essential restorative sleep', 'moon', '#405B49', 'RestAndSleep', 1, 1),
(2, NULL, 'NAP', 'Power Nap', 'Afternoon rest or power nap', 'sun', '#6F8A74', 'RestAndSleep', 1, 2),
(3, NULL, 'REST', 'Rest & Recharge', 'Quiet relaxation and mindfulness', 'coffee', '#8FA893', 'RestAndSleep', 1, 3),

-- 2. CAT Study
(4, NULL, 'CAT_STUDY', 'CAT Study (General)', 'Comprehensive CAT exam preparation', 'book-open', '#2D6A4F', 'Study', 1, 4),
(5, NULL, 'STUDY_QA', 'Quantitative Aptitude (QA)', 'Arithmetic, Algebra, Geometry & Numbers', 'calculator', '#1B4332', 'Study', 1, 5),
(6, NULL, 'STUDY_DILR', 'Data Interpretation & LR', 'Sets, puzzles, arrangements and charts', 'layout-grid', '#208B3A', 'Study', 1, 6),
(7, NULL, 'STUDY_VARC', 'Verbal Ability & Reading', 'RC passages, critical reasoning, parajumbles', 'file-text', '#40916C', 'Study', 1, 7),
(8, NULL, 'REVISION', 'Spaced Revision', 'Active recall and formula revisions', 'rotate-cw', '#52B788', 'Study', 1, 8),
(9, NULL, 'QUESTION_PRACTICE', 'Question Practice Set', 'Timed problem sets and drills', 'check-circle-2', '#74C69D', 'Study', 1, 9),
(10, NULL, 'GENERAL_READING', 'Reading & Editorials', 'Aeon essays, The Hindu, economic articles', 'glasses', '#95D5B2', 'Study', 1, 10),

-- 3. Meals & Breaks
(11, NULL, 'BREAKFAST', 'Breakfast', 'Morning nourishment', 'utensils', '#D4A373', 'MealsAndBreaks', 1, 11),
(12, NULL, 'LUNCH', 'Lunch', 'Mid-day meal', 'utensils', '#BC6C25', 'MealsAndBreaks', 1, 12),
(13, NULL, 'DINNER', 'Dinner', 'Evening meal', 'utensils', '#A68A64', 'MealsAndBreaks', 1, 13),
(14, NULL, 'SNACKS', 'Snacks', 'Light bite or refreshments', 'apple', '#DDA15E', 'MealsAndBreaks', 1, 14),
(15, NULL, 'TEA_COFFEE', 'Tea & Coffee Break', 'Warm beverage and mental pause', 'cup-soda', '#936639', 'MealsAndBreaks', 1, 15),

-- 4. Routine & Personal Care
(16, NULL, 'MORNING_ROUTINE', 'Morning Routine', 'Waking up, hydration, planning', 'sunrise', '#E9C9C5', 'Personal', 1, 16),
(17, NULL, 'PERSONAL_CARE', 'Personal Care & Grooming', 'Shower, meditation, journaling', 'sparkles', '#D8B4E2', 'Personal', 1, 17),
(18, NULL, 'HOUSEHOLD', 'Household Work', 'Errands, cleaning, daily maintenance', 'home', '#BDB2FF', 'Personal', 1, 18),

-- 5. Work & Commute
(19, NULL, 'WORK', 'Work / College', 'Professional tasks or lectures', 'briefcase', '#457B9D', 'WorkAndCommute', 1, 19),
(20, NULL, 'COMMUTE', 'Commute & Travel', 'Travel time and commuting', 'car', '#1D3557', 'WorkAndCommute', 1, 20),

-- 6. Health & Fitness
(21, NULL, 'EXERCISE', 'Exercise & Gym', 'Strength training or cardio workout', 'dumbbell', '#E76F51', 'HealthAndFitness', 1, 21),
(22, NULL, 'WALKING', 'Walking & Fresh Air', 'Brisk walk or nature stroll', 'footprints', '#F4A261', 'HealthAndFitness', 1, 22),

-- 7. Leisure & Social
(23, NULL, 'ENTERTAINMENT', 'Entertainment & Media', 'Music, books, cinema or leisure reading', 'tv', '#FFB703', 'LeisureAndSocial', 1, 23),
(24, NULL, 'SOCIAL_MEDIA', 'Digital Browsing', 'Online recreation and news', 'smartphone', '#FB8500', 'LeisureAndSocial', 1, 24),
(25, NULL, 'FAMILY', 'Family Time', 'Quality time with family', 'users', '#9C6644', 'LeisureAndSocial', 1, 25),
(26, NULL, 'FRIENDS', 'Friends & Social', 'Socializing and conversations', 'smile', '#7F4F24', 'LeisureAndSocial', 1, 26),

-- 8. Other
(27, NULL, 'CUSTOM', 'Custom Activity', 'Personal customized block', 'more-horizontal', '#6C757D', 'Other', 1, 27),
(28, NULL, 'UNPLANNED', 'Unplanned Activity', 'Ad-hoc tasks or interruptions', 'help-circle', '#495057', 'Other', 1, 28)
ON DUPLICATE KEY UPDATE 
    category_name = VALUES(category_name),
    category_group = VALUES(category_group),
    color = VALUES(color),
    icon = VALUES(icon);

