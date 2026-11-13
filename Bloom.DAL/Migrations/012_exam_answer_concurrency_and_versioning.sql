-- =============================================================================
-- Migration 012: Exam Answer Concurrency, Versioning, and Security Hardening
-- Phase 7 Acceptance Hardening
-- 1. Adds optimistic concurrency 'version' column to exam_attempt_questions.
-- 2. Adds 'role' column to users to support RBAC on content authoring.
-- 3. Adjusts question foreign key on exam attempts to ON DELETE SET NULL
--    so question deletions never corrupt or destroy historical exam attempts.
-- =============================================================================

ALTER TABLE users 
ADD COLUMN role VARCHAR(50) NOT NULL DEFAULT 'Aspirant';

ALTER TABLE exam_attempt_questions 
ADD COLUMN version INT UNSIGNED NOT NULL DEFAULT 1;

ALTER TABLE exam_attempt_questions 
DROP FOREIGN KEY fk_attempt_q_question;

ALTER TABLE exam_attempt_questions 
MODIFY COLUMN question_id BIGINT UNSIGNED NULL;

ALTER TABLE exam_attempt_questions 
ADD CONSTRAINT fk_attempt_q_question FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE SET NULL;

