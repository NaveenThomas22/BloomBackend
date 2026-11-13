-- =============================================================================
-- Migration 015: Habits, Personal Milestones, and Smart Notifications Schema
-- Phase 8 (8.4, 8.5, 8.6) - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Habit Categories Table
CREATE TABLE IF NOT EXISTS habit_categories (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL,
    category_code VARCHAR(50) NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    icon VARCHAR(50) NOT NULL DEFAULT 'check-circle',
    color VARCHAR(20) NOT NULL DEFAULT '#A8BDA5',
    display_order SMALLINT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_habit_categories_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_habit_cat_effective_code ((COALESCE(user_id, 0)), category_code),
    INDEX idx_habit_cat_lookup (user_id, is_active, display_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. User Habits Table
CREATE TABLE IF NOT EXISTS user_habits (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    category_id BIGINT UNSIGNED NOT NULL,
    name VARCHAR(150) NOT NULL,
    description VARCHAR(500) NULL,
    frequency VARCHAR(30) NOT NULL DEFAULT 'Daily',
    target_frequency_days INT UNSIGNED NOT NULL DEFAULT 7,
    target_value DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    target_unit VARCHAR(50) NOT NULL DEFAULT 'times',
    start_date DATE NOT NULL,
    end_date DATE NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    display_order SMALLINT UNSIGNED NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at_utc DATETIME NULL,
    CONSTRAINT fk_user_habits_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_habits_cat FOREIGN KEY (category_id) REFERENCES habit_categories(id) ON DELETE RESTRICT,
    INDEX idx_user_habits_active (user_id, is_active, deleted_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Habit Completions Table
CREATE TABLE IF NOT EXISTS habit_completions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    habit_id BIGINT UNSIGNED NOT NULL,
    completion_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Completed',
    actual_value DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    skip_reason VARCHAR(255) NULL,
    notes VARCHAR(500) NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_habit_completions_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_habit_completions_habit FOREIGN KEY (habit_id) REFERENCES user_habits(id) ON DELETE CASCADE,
    UNIQUE KEY uq_user_habit_date (user_id, habit_id, completion_date),
    INDEX idx_habit_completions_lookup (user_id, completion_date, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Personal Milestones Table
CREATE TABLE IF NOT EXISTS personal_milestones (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(500) NULL,
    category VARCHAR(50) NOT NULL DEFAULT 'GENERAL',
    target_value DECIMAL(10,2) NOT NULL,
    current_value DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    unit VARCHAR(50) NOT NULL DEFAULT 'count',
    target_date DATE NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'InProgress',
    achieved_at_utc DATETIME NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at_utc DATETIME NULL,
    CONSTRAINT fk_personal_milestones_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_milestones_user_status (user_id, status, deleted_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Notification Categories Table
CREATE TABLE IF NOT EXISTS notification_categories (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    category_code VARCHAR(50) NOT NULL UNIQUE,
    category_name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    icon VARCHAR(50) NOT NULL DEFAULT 'bell',
    color VARCHAR(20) NOT NULL DEFAULT '#A8BDA5',
    default_enabled TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. User Notification Preferences Table
CREATE TABLE IF NOT EXISTS user_notification_preferences (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    category_code VARCHAR(50) NOT NULL,
    is_enabled TINYINT(1) NOT NULL DEFAULT 1,
    preferred_time_utc TIME NULL,
    quiet_hours_start_utc TIME NULL,
    quiet_hours_end_utc TIME NULL,
    channels VARCHAR(50) NOT NULL DEFAULT 'IN_APP,PUSH',
    max_daily_alerts INT UNSIGNED NOT NULL DEFAULT 10,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_notif_pref_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_user_notif_pref (user_id, category_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. User Notifications Table
CREATE TABLE IF NOT EXISTS user_notifications (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    category_code VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    deep_link VARCHAR(255) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Unread',
    read_at_utc DATETIME NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_notif_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_notif_lookup (user_id, status, created_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. Notification Subscriptions Table (Web Push)
CREATE TABLE IF NOT EXISTS notification_subscriptions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    endpoint VARCHAR(1000) NOT NULL,
    p256dh VARCHAR(255) NOT NULL,
    auth VARCHAR(255) NOT NULL,
    user_agent VARCHAR(255) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_notif_sub_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_notif_sub_user (user_id, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 9. Notification Delivery Attempts Table
CREATE TABLE IF NOT EXISTS notification_delivery_attempts (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    notification_id BIGINT UNSIGNED NOT NULL,
    channel VARCHAR(20) NOT NULL DEFAULT 'PUSH',
    status VARCHAR(20) NOT NULL DEFAULT 'Success',
    error_message VARCHAR(500) NULL,
    attempted_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notif_attempt_notif FOREIGN KEY (notification_id) REFERENCES user_notifications(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

