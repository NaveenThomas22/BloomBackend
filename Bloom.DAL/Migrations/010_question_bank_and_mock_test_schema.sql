-- =============================================================================
-- Migration 010: CAT Question Bank, Practice Engine & Mock Test Schema
-- Phase 7 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. VARC Reading Comprehension Passages
CREATE TABLE IF NOT EXISTS question_passages (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    content MEDIUMTEXT NOT NULL,
    word_count INT NOT NULL DEFAULT 0,
    genre VARCHAR(100) NULL,
    source_attribution VARCHAR(255) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_passages_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. DILR Question Sets (Context Scenarios, Tables, and Charts)
CREATE TABLE IF NOT EXISTS question_sets (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    context_scenario MEDIUMTEXT NOT NULL,
    data_payload_json JSON NULL,
    set_type VARCHAR(50) NOT NULL DEFAULT 'DataInterpretation',
    source_attribution VARCHAR(255) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_qsets_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Questions Bank (MCQ and TITA)
CREATE TABLE IF NOT EXISTS questions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    subject_id BIGINT UNSIGNED NOT NULL,
    module_id BIGINT UNSIGNED NOT NULL,
    topic_id BIGINT UNSIGNED NOT NULL,
    subtopic_id BIGINT UNSIGNED NULL,
    question_set_id BIGINT UNSIGNED NULL,
    passage_id BIGINT UNSIGNED NULL,
    question_type VARCHAR(20) NOT NULL DEFAULT 'MCQ', -- 'MCQ', 'TITA'
    difficulty_level VARCHAR(20) NOT NULL DEFAULT 'Medium', -- 'Easy', 'Medium', 'Hard'
    question_content MEDIUMTEXT NOT NULL,
    question_image_url VARCHAR(500) NULL,
    correct_answer VARCHAR(255) NOT NULL,
    tita_numeric_range_min DECIMAL(12,4) NULL,
    tita_numeric_range_max DECIMAL(12,4) NULL,
    explanation MEDIUMTEXT NULL,
    solution_steps MEDIUMTEXT NULL,
    estimated_seconds INT NOT NULL DEFAULT 120,
    default_marks DECIMAL(5,2) NOT NULL DEFAULT 3.00,
    default_penalty DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    source_attribution VARCHAR(200) NULL,
    tags VARCHAR(255) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Published', -- 'Draft', 'Published', 'Archived'
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_questions_subject_status (subject_id, status),
    INDEX ix_questions_topic_diff_status (topic_id, difficulty_level, status),
    INDEX ix_questions_passage (passage_id),
    INDEX ix_questions_set (question_set_id),
    CONSTRAINT fk_q_subject FOREIGN KEY (subject_id) REFERENCES syllabus_subjects (id) ON DELETE CASCADE,
    CONSTRAINT fk_q_module FOREIGN KEY (module_id) REFERENCES syllabus_modules (id) ON DELETE CASCADE,
    CONSTRAINT fk_q_topic FOREIGN KEY (topic_id) REFERENCES syllabus_topics (id) ON DELETE CASCADE,
    CONSTRAINT fk_q_subtopic FOREIGN KEY (subtopic_id) REFERENCES syllabus_subtopics (id) ON DELETE SET NULL,
    CONSTRAINT fk_q_set FOREIGN KEY (question_set_id) REFERENCES question_sets (id) ON DELETE SET NULL,
    CONSTRAINT fk_q_passage FOREIGN KEY (passage_id) REFERENCES question_passages (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Question Options (for MCQ items)
CREATE TABLE IF NOT EXISTS question_options (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    question_id BIGINT UNSIGNED NOT NULL,
    option_key VARCHAR(5) NOT NULL, -- 'A', 'B', 'C', 'D'
    content TEXT NOT NULL,
    is_correct TINYINT(1) NOT NULL DEFAULT 0,
    display_order INT NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_question_option (question_id, option_key),
    CONSTRAINT fk_qoptions_q FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. User Question Bookmarks
CREATE TABLE IF NOT EXISTS user_question_bookmarks (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    question_id BIGINT UNSIGNED NOT NULL,
    note VARCHAR(500) NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_user_q_bookmark (user_id, question_id),
    INDEX ix_bookmarks_user (user_id, created_at_utc),
    CONSTRAINT fk_bookmarks_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_bookmarks_q FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Practice Sessions
CREATE TABLE IF NOT EXISTS practice_sessions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    title VARCHAR(150) NOT NULL,
    practice_mode VARCHAR(50) NOT NULL DEFAULT 'Topic', -- 'Topic', 'Subject', 'Difficulty', 'Custom', 'DILRSet', 'VARCPassage'
    subject_id BIGINT UNSIGNED NULL,
    topic_id BIGINT UNSIGNED NULL,
    difficulty_level VARCHAR(20) NULL,
    total_questions INT NOT NULL DEFAULT 0,
    attempted_questions INT NOT NULL DEFAULT 0,
    correct_answers INT NOT NULL DEFAULT 0,
    incorrect_answers INT NOT NULL DEFAULT 0,
    unanswered_questions INT NOT NULL DEFAULT 0,
    score DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    accuracy_percentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    time_limit_seconds INT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'InProgress', -- 'InProgress', 'Completed', 'Abandoned'
    started_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at_utc DATETIME NULL,
    activity_record_id BIGINT UNSIGNED NULL,
    INDEX ix_practice_user_status (user_id, status, started_at_utc),
    CONSTRAINT fk_practice_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_practice_subject FOREIGN KEY (subject_id) REFERENCES syllabus_subjects (id) ON DELETE SET NULL,
    CONSTRAINT fk_practice_topic FOREIGN KEY (topic_id) REFERENCES syllabus_topics (id) ON DELETE SET NULL,
    CONSTRAINT fk_practice_activity FOREIGN KEY (activity_record_id) REFERENCES activity_records (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Practice Session Questions
CREATE TABLE IF NOT EXISTS practice_session_questions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    practice_session_id BIGINT UNSIGNED NOT NULL,
    question_id BIGINT UNSIGNED NOT NULL,
    display_order INT NOT NULL DEFAULT 1,
    user_answer VARCHAR(255) NULL,
    is_answered TINYINT(1) NOT NULL DEFAULT 0,
    is_correct TINYINT(1) NULL,
    is_marked_for_review TINYINT(1) NOT NULL DEFAULT 0,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    answered_at_utc DATETIME NULL,
    UNIQUE KEY uq_practice_q_order (practice_session_id, display_order),
    CONSTRAINT fk_practice_q_session FOREIGN KEY (practice_session_id) REFERENCES practice_sessions (id) ON DELETE CASCADE,
    CONSTRAINT fk_practice_q_question FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. Question Attempts (Persistent historical record for practice & exams)
CREATE TABLE IF NOT EXISTS question_attempts (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    question_id BIGINT UNSIGNED NOT NULL,
    source_type VARCHAR(30) NOT NULL DEFAULT 'Practice', -- 'Practice', 'MockExam'
    source_id BIGINT UNSIGNED NOT NULL,
    submitted_answer VARCHAR(255) NULL,
    is_correct TINYINT(1) NOT NULL DEFAULT 0,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    marks_obtained DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    topic_id BIGINT UNSIGNED NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_attempts_user_q (user_id, question_id, created_at_utc),
    INDEX ix_attempts_user_topic (user_id, topic_id),
    CONSTRAINT fk_qattempts_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_qattempts_q FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE,
    CONSTRAINT fk_qattempts_topic FOREIGN KEY (topic_id) REFERENCES syllabus_topics (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 9. Exam Templates (CAT Full Mocks, Sectional Mocks, Custom Mocks)
CREATE TABLE IF NOT EXISTS exam_templates (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT NULL,
    exam_type VARCHAR(50) NOT NULL DEFAULT 'FullMock', -- 'FullMock', 'SectionalMock', 'Custom'
    total_duration_minutes INT NOT NULL DEFAULT 120,
    total_questions INT NOT NULL DEFAULT 66,
    total_marks DECIMAL(6,2) NOT NULL DEFAULT 198.00,
    instructions MEDIUMTEXT NULL,
    is_published TINYINT(1) NOT NULL DEFAULT 1,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_exam_templates_type (exam_type, is_published, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 10. Exam Sections (e.g. VARC, DILR, QA with strictly timed progression)
CREATE TABLE IF NOT EXISTS exam_sections (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    exam_template_id BIGINT UNSIGNED NOT NULL,
    section_name VARCHAR(100) NOT NULL,
    subject_id BIGINT UNSIGNED NULL,
    display_order INT NOT NULL DEFAULT 1,
    duration_minutes INT NOT NULL DEFAULT 40,
    question_count INT NOT NULL DEFAULT 22,
    can_navigate_back TINYINT(1) NOT NULL DEFAULT 0, -- CAT standard: strictly forward sectional progression
    correct_marks DECIMAL(5,2) NOT NULL DEFAULT 3.00,
    incorrect_penalty DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_exam_sections_template (exam_template_id, display_order),
    CONSTRAINT fk_exam_sections_template FOREIGN KEY (exam_template_id) REFERENCES exam_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_exam_sections_subject FOREIGN KEY (subject_id) REFERENCES syllabus_subjects (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 11. Exam Section Questions Link
CREATE TABLE IF NOT EXISTS exam_section_questions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    exam_section_id BIGINT UNSIGNED NOT NULL,
    question_id BIGINT UNSIGNED NOT NULL,
    display_order INT NOT NULL DEFAULT 1,
    marks_override DECIMAL(5,2) NULL,
    penalty_override DECIMAL(5,2) NULL,
    UNIQUE KEY uq_exam_sec_q (exam_section_id, display_order),
    CONSTRAINT fk_sec_q_section FOREIGN KEY (exam_section_id) REFERENCES exam_sections (id) ON DELETE CASCADE,
    CONSTRAINT fk_sec_q_question FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 12. Exam Attempts (Live session state, server-authoritative timers)
CREATE TABLE IF NOT EXISTS exam_attempts (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    exam_template_id BIGINT UNSIGNED NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'InProgress', -- 'Created', 'InProgress', 'Submitted', 'AutoSubmitted', 'Expired', 'Abandoned'
    started_at_utc DATETIME NOT NULL,
    deadline_utc DATETIME NOT NULL,
    current_section_id BIGINT UNSIGNED NULL,
    section_started_at_utc DATETIME NOT NULL,
    section_deadline_utc DATETIME NOT NULL,
    current_question_index INT NOT NULL DEFAULT 0,
    submitted_at_utc DATETIME NULL,
    total_score DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    max_possible_score DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    total_questions INT NOT NULL DEFAULT 0,
    total_attempted INT NOT NULL DEFAULT 0,
    total_correct INT NOT NULL DEFAULT 0,
    total_incorrect INT NOT NULL DEFAULT 0,
    total_unanswered INT NOT NULL DEFAULT 0,
    accuracy_percentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    activity_record_id BIGINT UNSIGNED NULL,
    created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX ix_exam_attempts_user_status (user_id, status),
    INDEX ix_exam_attempts_deadlines (status, deadline_utc, section_deadline_utc),
    CONSTRAINT fk_exam_attempts_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_exam_attempts_template FOREIGN KEY (exam_template_id) REFERENCES exam_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_exam_attempts_cur_sec FOREIGN KEY (current_section_id) REFERENCES exam_sections (id) ON DELETE SET NULL,
    CONSTRAINT fk_exam_attempts_activity FOREIGN KEY (activity_record_id) REFERENCES activity_records (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 13. Exam Attempt Questions (Immutable Question Snapshots + User Responses)
CREATE TABLE IF NOT EXISTS exam_attempt_questions (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    exam_attempt_id BIGINT UNSIGNED NOT NULL,
    exam_section_id BIGINT UNSIGNED NOT NULL,
    question_id BIGINT UNSIGNED NOT NULL,
    display_order INT NOT NULL DEFAULT 1,
    snapshot_question_type VARCHAR(20) NOT NULL,
    snapshot_content MEDIUMTEXT NOT NULL,
    snapshot_options_json JSON NULL,
    snapshot_passage_json JSON NULL,
    snapshot_set_json JSON NULL,
    snapshot_correct_answer VARCHAR(255) NOT NULL,
    snapshot_marks DECIMAL(5,2) NOT NULL DEFAULT 3.00,
    snapshot_penalty DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    snapshot_explanation MEDIUMTEXT NULL,
    snapshot_solution_steps MEDIUMTEXT NULL,
    user_answer VARCHAR(255) NULL,
    answer_status VARCHAR(35) NOT NULL DEFAULT 'NotVisited', -- 'NotVisited', 'NotAnswered', 'Answered', 'MarkedForReview', 'AnsweredAndMarkedForReview'
    is_correct TINYINT(1) NULL,
    marks_awarded DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    answered_at_utc DATETIME NULL,
    last_saved_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_attempt_sec_order (exam_attempt_id, exam_section_id, display_order),
    INDEX ix_attempt_q_status (exam_attempt_id, exam_section_id, answer_status),
    CONSTRAINT fk_attempt_q_attempt FOREIGN KEY (exam_attempt_id) REFERENCES exam_attempts (id) ON DELETE CASCADE,
    CONSTRAINT fk_attempt_q_section FOREIGN KEY (exam_section_id) REFERENCES exam_sections (id) ON DELETE CASCADE,
    CONSTRAINT fk_attempt_q_question FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 14. Exam Attempt Section Results (Historical review snapshot per section)
CREATE TABLE IF NOT EXISTS exam_attempt_section_results (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    exam_attempt_id BIGINT UNSIGNED NOT NULL,
    exam_section_id BIGINT UNSIGNED NOT NULL,
    section_name VARCHAR(100) NOT NULL,
    display_order INT NOT NULL DEFAULT 1,
    score DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    max_score DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    total_questions INT NOT NULL DEFAULT 0,
    attempted INT NOT NULL DEFAULT 0,
    correct INT NOT NULL DEFAULT 0,
    incorrect INT NOT NULL DEFAULT 0,
    unanswered INT NOT NULL DEFAULT 0,
    accuracy_percentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    time_spent_seconds INT NOT NULL DEFAULT 0,
    UNIQUE KEY uq_attempt_section (exam_attempt_id, exam_section_id),
    CONSTRAINT fk_sec_res_attempt FOREIGN KEY (exam_attempt_id) REFERENCES exam_attempts (id) ON DELETE CASCADE,
    CONSTRAINT fk_sec_res_section FOREIGN KEY (exam_section_id) REFERENCES exam_sections (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

