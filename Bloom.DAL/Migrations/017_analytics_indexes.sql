-- =============================================================================
-- Migration 017: Performance Indexes for Analytics & Insights Engine
-- Phase 9 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Activity Records index for date-bounded completed activity aggregation
ALTER TABLE activity_records 
ADD INDEX idx_act_rec_analytics (user_id, is_planned, activity_status, deleted_at_utc, start_at_utc);

-- 2. Question Attempts index for fast practice/exam source filtering and date range queries
ALTER TABLE question_attempts 
ADD INDEX ix_qattempts_analytics (user_id, source_type, created_at_utc);

-- 3. Exam Attempts index for mock test historical scoring trends
ALTER TABLE exam_attempts 
ADD INDEX ix_exam_attempts_analytics (user_id, status, submitted_at_utc);

-- 4. Revision Schedules index for quick overdue and completion status checks
ALTER TABLE revision_schedules 
ADD INDEX ix_rev_sched_analytics (user_id, is_deleted, status, scheduled_date);

