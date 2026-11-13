-- =============================================================================
-- Migration 005: 24-Hour Life & Activity Logger Schema
-- Phase 5 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Activity Categories Table
CREATE TABLE IF NOT EXISTS activity_categories (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL,
    category_code VARCHAR(50) NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    icon VARCHAR(50) NOT NULL DEFAULT 'activity',
    color VARCHAR(20) NOT NULL DEFAULT '#A8BDA5',
    category_group VARCHAR(50) NOT NULL DEFAULT 'Other',
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    display_order SMALLINT UNSIGNED NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_activity_categories_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_act_categories_user_code (user_id, category_code),
    INDEX idx_act_categories_lookup (user_id, is_active, display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Activity Records Table (Authoritative Chronological Elapsed Time)
CREATE TABLE IF NOT EXISTS activity_records (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    activity_category_id BIGINT UNSIGNED NOT NULL,
    activity_title VARCHAR(255) NOT NULL,
    description TEXT NULL,
    start_at_utc DATETIME NOT NULL,
    end_at_utc DATETIME NULL,
    timezone_id VARCHAR(50) NOT NULL DEFAULT 'Asia/Kolkata',
    activity_status VARCHAR(30) NOT NULL DEFAULT 'Completed',
    entry_source VARCHAR(30) NOT NULL DEFAULT 'Manual',
    is_planned TINYINT(1) NOT NULL DEFAULT 0,
    session_group_id VARCHAR(64) NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at_utc DATETIME NULL,
    CONSTRAINT fk_activity_records_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_activity_records_category FOREIGN KEY (activity_category_id) REFERENCES activity_categories(id) ON DELETE RESTRICT,
    INDEX idx_act_rec_user_time (user_id, start_at_utc, end_at_utc),
    INDEX idx_act_rec_user_status (user_id, activity_status, deleted_at_utc),
    INDEX idx_act_rec_session_group (user_id, session_group_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Study Session Details Table (Strict 1:1 Extension - Zero Time Duplication)
CREATE TABLE IF NOT EXISTS study_session_details (
    activity_id BIGINT UNSIGNED PRIMARY KEY,
    topic_id BIGINT UNSIGNED NULL,
    study_mode VARCHAR(50) NOT NULL DEFAULT 'Practice',
    difficulty_level VARCHAR(20) NOT NULL DEFAULT 'Moderate',
    questions_attempted INT UNSIGNED NOT NULL DEFAULT 0,
    questions_correct INT UNSIGNED NOT NULL DEFAULT 0,
    session_notes TEXT NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_study_details_activity FOREIGN KEY (activity_id) REFERENCES activity_records(id) ON DELETE CASCADE,
    CONSTRAINT fk_study_details_topic FOREIGN KEY (topic_id) REFERENCES syllabus_topics(id) ON DELETE SET NULL,
    INDEX idx_study_details_topic (topic_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Planned Activities Table (Separate Plan - Not Elapsed Time)
CREATE TABLE IF NOT EXISTS planned_activities (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    activity_category_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT NULL,
    planned_start_at_utc DATETIME NOT NULL,
    planned_end_at_utc DATETIME NOT NULL,
    timezone_id VARCHAR(50) NOT NULL DEFAULT 'Asia/Kolkata',
    linked_topic_id BIGINT UNSIGNED NULL,
    plan_status VARCHAR(30) NOT NULL DEFAULT 'Scheduled',
    actual_activity_id BIGINT UNSIGNED NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_planned_activities_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_planned_activities_category FOREIGN KEY (activity_category_id) REFERENCES activity_categories(id) ON DELETE RESTRICT,
    CONSTRAINT fk_planned_activities_topic FOREIGN KEY (linked_topic_id) REFERENCES syllabus_topics(id) ON DELETE SET NULL,
    CONSTRAINT fk_planned_activities_actual FOREIGN KEY (actual_activity_id) REFERENCES activity_records(id) ON DELETE SET NULL,
    INDEX idx_planned_act_user_time (user_id, planned_start_at_utc, planned_end_at_utc),
    INDEX idx_planned_act_user_status (user_id, plan_status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. User Timer State Table (Authoritative Active Timer Tracking & Recovery)
CREATE TABLE IF NOT EXISTS user_timer_states (
    user_id BIGINT UNSIGNED PRIMARY KEY,
    session_group_id VARCHAR(64) NOT NULL,
    activity_category_id BIGINT UNSIGNED NOT NULL,
    topic_id BIGINT UNSIGNED NULL,
    title VARCHAR(255) NOT NULL,
    study_mode VARCHAR(50) NOT NULL DEFAULT 'Practice',
    timer_state VARCHAR(30) NOT NULL DEFAULT 'Running',
    current_interval_id BIGINT UNSIGNED NULL,
    started_at_utc DATETIME NOT NULL,
    last_paused_at_utc DATETIME NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_timer_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_timer_category FOREIGN KEY (activity_category_id) REFERENCES activity_categories(id) ON DELETE RESTRICT,
    CONSTRAINT fk_user_timer_topic FOREIGN KEY (topic_id) REFERENCES syllabus_topics(id) ON DELETE SET NULL,
    CONSTRAINT fk_user_timer_interval FOREIGN KEY (current_interval_id) REFERENCES activity_records(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

