-- =============================================================================
-- Migration 009: Unique Constraint for Active Revision Schedules
-- Phase 6 Acceptance Hardening
-- Enforces that at most one active revision schedule (Upcoming, DueToday, Overdue)
-- can exist for any (user_id, topic_id) tuple at the database engine level.
-- Completed and Skipped historical schedules have NULL active_topic_lock and are unaffected.
-- =============================================================================

ALTER TABLE revision_schedules 
ADD COLUMN active_topic_lock VARCHAR(64) GENERATED ALWAYS AS (
    CASE 
        WHEN status IN ('Upcoming', 'DueToday', 'Overdue') AND is_deleted = 0 
        THEN CONCAT(user_id, '-', topic_id) 
        ELSE NULL 
    END
) VIRTUAL,
ADD UNIQUE KEY uq_rev_sched_active_user_topic (active_topic_lock);
