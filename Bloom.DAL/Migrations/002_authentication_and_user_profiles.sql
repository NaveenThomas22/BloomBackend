-- =============================================================================
-- Migration 002: Authentication & User Profiles Schema
-- Phase 2 - Bloom (My CAT Journey)
-- =============================================================================

CREATE TABLE IF NOT EXISTS schema_migrations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    version VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255) NOT NULL,
    applied_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 1. Users Table
CREATE TABLE IF NOT EXISTS users (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(255) NOT NULL,
    normalized_email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    profile_picture_url VARCHAR(1000) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    email_verified TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at_utc DATETIME NULL,
    UNIQUE KEY uq_users_normalized_email (normalized_email),
    INDEX ix_users_email (email),
    INDEX ix_users_is_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. User Profiles Table
CREATE TABLE IF NOT EXISTS user_profiles (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    date_of_birth DATE NULL,
    cat_target_year SMALLINT UNSIGNED NOT NULL DEFAULT 2026,
    cat_exam_date DATE NULL,
    preparation_level VARCHAR(50) NOT NULL DEFAULT 'Beginner',
    preparation_mode VARCHAR(50) NOT NULL DEFAULT 'SelfStudy',
    timezone VARCHAR(50) NOT NULL DEFAULT 'Asia/Kolkata',
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_user_profiles_user_id (user_id),
    CONSTRAINT fk_user_profiles_user_id FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. User Preferences Table
CREATE TABLE IF NOT EXISTS user_preferences (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    theme VARCHAR(50) NOT NULL DEFAULT 'Sage Garden',
    daily_study_target_minutes INT UNSIGNED NOT NULL DEFAULT 360,
    preferred_wake_time TIME NOT NULL DEFAULT '06:00:00',
    preferred_sleep_time TIME NOT NULL DEFAULT '22:30:00',
    notification_enabled TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_user_preferences_user_id (user_id),
    CONSTRAINT fk_user_preferences_user_id FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Refresh Tokens Table (Secure Token Family & Rotation)
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    token_hash VARCHAR(255) NOT NULL,
    token_family_id VARCHAR(100) NOT NULL,
    expires_at_utc DATETIME NOT NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at_utc DATETIME NULL,
    replaced_by_token_id BIGINT UNSIGNED NULL,
    created_by_ip VARCHAR(50) NULL,
    revoked_reason VARCHAR(255) NULL,
    UNIQUE KEY uq_refresh_tokens_token_hash (token_hash),
    INDEX ix_refresh_tokens_user_family (user_id, token_family_id),
    INDEX ix_refresh_tokens_user_status (user_id, revoked_at_utc, expires_at_utc),
    CONSTRAINT fk_refresh_tokens_user_id FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_refresh_tokens_replaced_by FOREIGN KEY (replaced_by_token_id) 
        REFERENCES refresh_tokens (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Register migration in history table
INSERT IGNORE INTO schema_migrations (version, description) 
VALUES ('002', 'Phase 2: Authentication, User Profiles, User Preferences, and Refresh Tokens schema');

