-- =============================================================================
-- Migration 007: Goals, Preparation Milestones, and Spaced Repetition Schema
-- Phase 6 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Revision Policies Table (Configurable spaced repetition intervals)
CREATE TABLE IF NOT EXISTS revision_policies (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL, -- NULL indicates global system default policy
    policy_name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NULL,
    intervals_csv VARCHAR(100) NOT NULL DEFAULT '1,3,7,14,30',
    is_system TINYINT(1) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_revision_policies_user (user_id, is_active),
    CONSTRAINT fk_revision_policies_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Revision Schedules Table (Calendar-based scheduled topic revisions)
CREATE TABLE IF NOT EXISTS revision_schedules (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    topic_id BIGINT UNSIGNED NOT NULL,
    revision_policy_id BIGINT UNSIGNED NOT NULL,
    step_number INT NOT NULL DEFAULT 1,
    scheduled_date DATE NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Upcoming', -- 'Upcoming', 'DueToday', 'Overdue', 'Completed', 'Skipped', 'Rescheduled'
    completed_at_utc DATETIME NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_rev_sched_user_date (user_id, scheduled_date, status, is_deleted),
    INDEX ix_rev_sched_topic (user_id, topic_id, is_deleted),
    CONSTRAINT fk_rev_sched_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_rev_sched_topic FOREIGN KEY (topic_id)
        REFERENCES syllabus_topics (id) ON DELETE CASCADE,
    CONSTRAINT fk_rev_sched_policy FOREIGN KEY (revision_policy_id)
        REFERENCES revision_policies (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Revision Attempts Table (Immutable historical attempt records)
CREATE TABLE IF NOT EXISTS revision_attempts (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    revision_schedule_id BIGINT UNSIGNED NULL,
    user_id BIGINT UNSIGNED NOT NULL,
    topic_id BIGINT UNSIGNED NOT NULL,
    revised_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revision_date DATE NOT NULL,
    understanding_level VARCHAR(30) NOT NULL, -- 'Difficult', 'Partial', 'Good', 'Mastered'
    confidence_rating TINYINT NOT NULL DEFAULT 3,
    notes TEXT NULL,
    linked_activity_record_id BIGINT UNSIGNED NULL,
    next_scheduled_date DATE NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_rev_attempts_user_topic (user_id, topic_id),
    INDEX ix_rev_attempts_schedule (revision_schedule_id),
    CONSTRAINT fk_rev_attempts_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_rev_attempts_topic FOREIGN KEY (topic_id)
        REFERENCES syllabus_topics (id) ON DELETE CASCADE,
    CONSTRAINT fk_rev_attempts_schedule FOREIGN KEY (revision_schedule_id)
        REFERENCES revision_schedules (id) ON DELETE SET NULL,
    CONSTRAINT fk_rev_attempts_activity FOREIGN KEY (linked_activity_record_id)
        REFERENCES activity_records (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. User Goals Table (Daily, Weekly, Monthly, and Custom Goals)
CREATE TABLE IF NOT EXISTS user_goals (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(150) NOT NULL,
    description TEXT NULL,
    goal_period VARCHAR(30) NOT NULL, -- 'Daily', 'Weekly', 'Monthly', 'Custom'
    target_metric VARCHAR(50) NOT NULL, -- 'StudyMinutes', 'TopicsCompleted', 'SubtopicsCompleted', 'RevisionsCompleted', 'ActivitiesCount'
    target_value DECIMAL(10,2) NOT NULL,
    unit VARCHAR(30) NOT NULL DEFAULT 'Minutes', -- 'Minutes', 'Hours', 'Topics', 'Subtopics', 'Revisions', 'Sessions'
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    subject_id BIGINT UNSIGNED NULL,
    topic_id BIGINT UNSIGNED NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Active', -- 'Active', 'Paused', 'Completed', 'Cancelled'
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_user_goals_period_status (user_id, goal_period, status, is_deleted),
    INDEX ix_user_goals_dates (user_id, start_date, end_date),
    CONSTRAINT fk_user_goals_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_goals_subject FOREIGN KEY (subject_id)
        REFERENCES syllabus_subjects (id) ON DELETE SET NULL,
    CONSTRAINT fk_user_goals_topic FOREIGN KEY (topic_id)
        REFERENCES syllabus_topics (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Preparation Milestones Table
CREATE TABLE IF NOT EXISTS preparation_milestones (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(150) NOT NULL,
    description TEXT NULL,
    target_date DATE NOT NULL,
    subject_id BIGINT UNSIGNED NULL,
    topic_id BIGINT UNSIGNED NULL,
    priority VARCHAR(20) NOT NULL DEFAULT 'Medium', -- 'Low', 'Medium', 'High', 'Urgent'
    status VARCHAR(30) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Completed', 'Overdue', 'Cancelled'
    completed_at_utc DATETIME NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_prep_milestones_user (user_id, status, target_date, is_deleted),
    CONSTRAINT fk_prep_milestones_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_prep_milestones_subject FOREIGN KEY (subject_id)
        REFERENCES syllabus_subjects (id) ON DELETE SET NULL,
    CONSTRAINT fk_prep_milestones_topic FOREIGN KEY (topic_id)
        REFERENCES syllabus_topics (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

