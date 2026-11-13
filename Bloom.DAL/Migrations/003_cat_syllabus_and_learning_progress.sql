-- =============================================================================
-- Migration 003: CAT Syllabus Hierarchy, Resources, and Learning Progress Schema
-- Phase 4 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Syllabus Subjects Table
CREATE TABLE IF NOT EXISTS syllabus_subjects (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL,
    code VARCHAR(20) NOT NULL,
    name VARCHAR(150) NOT NULL,
    description VARCHAR(500) NULL,
    display_order INT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_syllabus_subjects_user (user_id, is_active, display_order),
    CONSTRAINT fk_syllabus_subjects_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Syllabus Modules Table
CREATE TABLE IF NOT EXISTS syllabus_modules (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    subject_id BIGINT UNSIGNED NOT NULL,
    user_id BIGINT UNSIGNED NULL,
    name VARCHAR(150) NOT NULL,
    description VARCHAR(500) NULL,
    display_order INT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_syllabus_modules_subj_user (subject_id, user_id, is_active, display_order),
    CONSTRAINT fk_syllabus_modules_subject FOREIGN KEY (subject_id) 
        REFERENCES syllabus_subjects (id) ON DELETE CASCADE,
    CONSTRAINT fk_syllabus_modules_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Syllabus Topics Table
CREATE TABLE IF NOT EXISTS syllabus_topics (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    module_id BIGINT UNSIGNED NOT NULL,
    user_id BIGINT UNSIGNED NULL,
    name VARCHAR(150) NOT NULL,
    description TEXT NULL,
    estimated_study_hours DECIMAL(5,2) NOT NULL DEFAULT 4.00,
    difficulty_level VARCHAR(20) NOT NULL DEFAULT 'Medium',
    importance_level VARCHAR(20) NOT NULL DEFAULT 'High',
    display_order INT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_syllabus_topics_mod_user (module_id, user_id, is_active, display_order),
    CONSTRAINT fk_syllabus_topics_module FOREIGN KEY (module_id) 
        REFERENCES syllabus_modules (id) ON DELETE CASCADE,
    CONSTRAINT fk_syllabus_topics_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Syllabus Subtopics Table
CREATE TABLE IF NOT EXISTS syllabus_subtopics (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    topic_id BIGINT UNSIGNED NOT NULL,
    user_id BIGINT UNSIGNED NULL,
    name VARCHAR(150) NOT NULL,
    description VARCHAR(500) NULL,
    display_order INT UNSIGNED NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_syllabus_subtopics_top_user (topic_id, user_id, is_active, display_order),
    CONSTRAINT fk_syllabus_subtopics_topic FOREIGN KEY (topic_id) 
        REFERENCES syllabus_topics (id) ON DELETE CASCADE,
    CONSTRAINT fk_syllabus_subtopics_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. User Topic Progress Table (Status, Priority, Target Date, Personal Notes)
CREATE TABLE IF NOT EXISTS user_topic_progress (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    topic_id BIGINT UNSIGNED NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'NotStarted',
    priority VARCHAR(20) NOT NULL DEFAULT 'Medium',
    target_date DATE NULL,
    completion_date DATE NULL,
    confidence_rating TINYINT UNSIGNED NOT NULL DEFAULT 0,
    personal_notes TEXT NULL,
    is_custom_excluded TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_user_topic_progress (user_id, topic_id),
    INDEX ix_user_topic_status (user_id, status),
    INDEX ix_user_topic_priority (user_id, priority),
    CONSTRAINT fk_user_topic_progress_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_topic_progress_topic FOREIGN KEY (topic_id) 
        REFERENCES syllabus_topics (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. User Subtopic Progress Table
CREATE TABLE IF NOT EXISTS user_subtopic_progress (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    subtopic_id BIGINT UNSIGNED NOT NULL,
    is_completed TINYINT(1) NOT NULL DEFAULT 0,
    completed_at_utc DATETIME NULL,
    notes VARCHAR(1000) NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_user_subtopic_progress (user_id, subtopic_id),
    CONSTRAINT fk_user_subtopic_progress_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_subtopic_progress_subtopic FOREIGN KEY (subtopic_id) 
        REFERENCES syllabus_subtopics (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Syllabus Resources Table
CREATE TABLE IF NOT EXISTS syllabus_resources (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    topic_id BIGINT UNSIGNED NOT NULL,
    user_id BIGINT UNSIGNED NULL,
    title VARCHAR(200) NOT NULL,
    resource_type VARCHAR(50) NOT NULL DEFAULT 'Article',
    url VARCHAR(1000) NULL,
    notes TEXT NULL,
    display_order INT UNSIGNED NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_syllabus_resources_topic_user (topic_id, user_id),
    CONSTRAINT fk_syllabus_resources_topic FOREIGN KEY (topic_id) 
        REFERENCES syllabus_topics (id) ON DELETE CASCADE,
    CONSTRAINT fk_syllabus_resources_user FOREIGN KEY (user_id) 
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Register migration in history table
INSERT IGNORE INTO schema_migrations (version, description) 
VALUES ('003', 'Phase 4: CAT Syllabus Hierarchy, Resources, and Learning Progress Schema');

