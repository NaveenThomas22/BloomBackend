-- =============================================================================
-- Migration 013: Journal & Daily Reflections Schema
-- Phase 8.1 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Journal Categories Table
CREATE TABLE IF NOT EXISTS journal_categories (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL,
    category_code VARCHAR(50) NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    icon VARCHAR(50) NOT NULL DEFAULT 'book',
    color VARCHAR(20) NOT NULL DEFAULT '#A8BDA5',
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    display_order SMALLINT UNSIGNED NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_journal_categories_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_journal_cat_effective_code ((COALESCE(user_id, 0)), category_code),
    INDEX idx_journal_cat_lookup (user_id, is_active, display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Journal Entries Table
CREATE TABLE IF NOT EXISTS journal_entries (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    category_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(255) NOT NULL,
    content MEDIUMTEXT NOT NULL,
    entry_date DATE NOT NULL,
    is_draft TINYINT(1) NOT NULL DEFAULT 0,
    word_count INT UNSIGNED NOT NULL DEFAULT 0,
    version INT UNSIGNED NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at_utc DATETIME NULL,
    CONSTRAINT fk_journal_entries_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_journal_entries_category FOREIGN KEY (category_id) REFERENCES journal_categories(id) ON DELETE RESTRICT,
    INDEX idx_journal_user_date (user_id, entry_date, is_draft, deleted_at_utc),
    INDEX idx_journal_user_category (user_id, category_id, deleted_at_utc),
    INDEX idx_journal_user_created (user_id, created_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Reflection Moods Table
CREATE TABLE IF NOT EXISTS reflection_moods (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    mood_code VARCHAR(50) NOT NULL UNIQUE,
    mood_name VARCHAR(50) NOT NULL,
    emoji VARCHAR(16) NOT NULL,
    color VARCHAR(20) NOT NULL DEFAULT '#A8BDA5',
    description VARCHAR(255) NULL,
    display_order SMALLINT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Daily Reflections Table (Morning & Evening Reflections per user per date)
CREATE TABLE IF NOT EXISTS daily_reflections (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    reflection_date DATE NOT NULL,
    
    -- Morning Reflection Fields
    morning_mood_id BIGINT UNSIGNED NULL,
    morning_mood_intensity TINYINT UNSIGNED NULL,
    morning_energy_level TINYINT UNSIGNED NULL,
    today_intention VARCHAR(500) NULL,
    main_priority VARCHAR(500) NULL,
    task_one VARCHAR(255) NULL,
    task_two VARCHAR(255) NULL,
    task_three VARCHAR(255) NULL,
    study_target_minutes INT UNSIGNED NULL,
    personal_commitment VARCHAR(500) NULL,
    morning_notes TEXT NULL,
    morning_completed_at_utc DATETIME NULL,
    
    -- Evening Reflection Fields
    evening_mood_id BIGINT UNSIGNED NULL,
    evening_mood_intensity TINYINT UNSIGNED NULL,
    how_was_today TEXT NULL,
    what_accomplished TEXT NULL,
    what_went_well TEXT NULL,
    could_have_been_better TEXT NULL,
    what_learned_today TEXT NULL,
    what_challenged_me TEXT NULL,
    satisfaction_rating TINYINT UNSIGNED NULL,
    improve_tomorrow TEXT NULL,
    gratitude_entry TEXT NULL,
    evening_completed_at_utc DATETIME NULL,
    
    -- Timestamps
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_daily_reflections_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_reflections_morning_mood FOREIGN KEY (morning_mood_id) REFERENCES reflection_moods(id) ON DELETE SET NULL,
    CONSTRAINT fk_reflections_evening_mood FOREIGN KEY (evening_mood_id) REFERENCES reflection_moods(id) ON DELETE SET NULL,
    UNIQUE KEY uq_daily_reflections_user_date (user_id, reflection_date),
    INDEX idx_reflections_user_created (user_id, created_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

