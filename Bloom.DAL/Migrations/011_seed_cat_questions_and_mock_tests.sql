-- =============================================================================
-- Migration 011: Seed High-Quality CAT Question Bank & Mock Test Templates
-- Phase 7 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Seed VARC Reading Comprehension Passage
INSERT INTO question_passages (id, title, content, word_count, genre, source_attribution, is_active)
VALUES
(1, 'The Cognitive Architecture of Intuitive Heuristics',
'Human decision-making under existential uncertainty has long been treated by classical economics as a deviation from Bayesian rationality. In contrast, modern cognitive neuroscience suggests that bounded rationality is not an evolutionary compromise, but an optimized adaptation to computational scarcity. When individuals navigate complex probabilistic terrains, exhaustive computational search quickly becomes intractable. Instead, the brain deploys fast, frugal heuristics—hardwired statistical approximations refined over millennia of foraging and social navigation.

Central to this architecture is the dual-process framework. System 1 operates automatically, associatively, and with minimal metabolic expenditure, while System 2 conducts deliberative, rule-governed cognitive simulations. Rather than operating in adversarial isolation, empirical fMRI evidence demonstrates that System 1 acts as an epistemic filter, prioritizing candidate hypotheses before System 2 allocates attentional resources. 

However, in modern socio-technical environments where statistical feedback loops are noisy and delayed, these cognitive shortcuts can systematically miscalibrate risk perception. The availability heuristic, for example, conflates the emotional vividness of an exemplar with its objective frequency. When risk architectures depart from ancestral conditions, cognitive adaptations that once preserved caloric reserves become persistent algorithmic liabilities.',
320, 'Cognitive Science & Philosophy', 'Cognitive Sciences Review 2025', 1)
ON DUPLICATE KEY UPDATE title = VALUES(title);

-- 2. Seed DILR Data Interpretation & Logic Set
INSERT INTO question_sets (id, title, context_scenario, data_payload_json, set_type, source_attribution, is_active)
VALUES
(1, 'Four-City Premier Chess League Tournament',
'Four grandmaster teams—Bangalore Bishops (BB), Delhi Knights (DK), Mumbai Rooks (MR), and Kolkata Queens (KQ)—participated in a single round-robin chess tournament where each team played every other team exactly once. Each match comprised 4 individual games on Boards 1 to 4. For each individual game, a win awards 1 point, a draw awards 0.5 points, and a loss awards 0 points. The total match score is the sum of points across all 4 boards.

Additional Tournament Notes:
1. BB won against DK by a score of 2.5 - 1.5, with no draws on Boards 1 and 2.
2. MR defeated KQ by 3 - 1, winning on Board 1 and Board 3 while Board 2 ended in a draw.
3. The match between DK and MR ended in a tie (2 - 2), where White won on Board 1 and Board 4.
4. Kolkata Queens (KQ) scored a total of 4.5 individual game points across all three matches.
5. In the final standings, Bangalore Bishops (BB) accumulated the highest total game points of 8.0.',
'{"teams": ["BB", "DK", "MR", "KQ"], "rounds": 3, "boardsPerMatch": 4}',
'LogicalReasoning', 'CAT Logic Review Archive', 1)
ON DUPLICATE KEY UPDATE title = VALUES(title);

-- 3. Seed Questions & Options
-- =============================================================================
-- VARC Questions (Passage 1, Topic 3001: Main Idea & Inferences)
-- =============================================================================
INSERT INTO questions (id, subject_id, module_id, topic_id, subtopic_id, question_set_id, passage_id, question_type, difficulty_level, question_content, correct_answer, explanation, solution_steps, estimated_seconds, default_marks, default_penalty, tags, status)
VALUES
(3001, 3, 301, 3001, NULL, NULL, 1, 'MCQ', 'Medium',
'According to the passage, why do human cognitive heuristics represent an evolutionary optimization rather than a systemic flaw?',
'B',
'Paragraph 1 explicitly notes that bounded rationality is an optimized adaptation to computational scarcity because exhaustive search is computationally intractable.',
'1. Locate discussion of bounded rationality in Paragraph 1.\n2. Note statement: "bounded rationality is not an evolutionary compromise, but an optimized adaptation to computational scarcity."\n3. Option B directly mirrors this rationale.',
120, 3.00, 1.00, 'RC,Inference', 'Published'),

(3002, 3, 301, 3001, NULL, NULL, 1, 'MCQ', 'Hard',
'Which of the following, if true, would most directly undermine the author’s characterization of the interaction between System 1 and System 2?',
'C',
'The author argues System 1 acts as an epistemic filter prioritizing hypotheses before System 2 allocates attention. Demonstrating System 2 generates initial hypotheses would undermine this.',
'1. Identify author view: System 1 filters/prioritizes candidates for System 2.\n2. Negation/undermining condition: System 2 initiates hypotheses independently of System 1 heuristic filtering.\n3. Option C shows System 2 initiating hypothesis pools before emotional valence occurs.',
150, 3.00, 1.00, 'RC,CriticalReasoning', 'Published'),

(3003, 3, 301, 3001, NULL, NULL, 1, 'MCQ', 'Medium',
'The author mentions "modern socio-technical environments" primarily in order to:',
'D',
'Show that evolutionary heuristics become miscalibrated when encountering environments with noisy, delayed statistical feedback loops unlike ancestral conditions.',
'1. Re-read Paragraph 3: heuristics become liabilities in environments diverging from ancestral conditions.\n2. Option D correctly articulates the mismatch between ancestral adaptation and modern statistical environments.',
120, 3.00, 1.00, 'RC,AuthorPurpose', 'Published'),

(3004, 3, 301, 3001, NULL, NULL, 1, 'MCQ', 'Easy',
'The term "epistemic filter" as used in the second paragraph most nearly means:',
'A',
'A subconscious heuristic mechanism that narrows down candidate hypotheses before deliberate focus is applied.',
'1. Examine context: "System 1 acts as an epistemic filter, prioritizing candidate hypotheses before System 2 allocates attentional resources."\n2. Option A captures pre-deliberative hypothesis filtering.',
90, 3.00, 1.00, 'RC,VocabularyInContext', 'Published'),

-- VARC Verbal Ability (Para Jumble TITA - Topic 3005)
(3005, 3, 302, 3005, NULL, NULL, NULL, 'TITA', 'Medium',
'The four sentences (labelled 1, 2, 3, 4) given below, when properly sequenced, form a coherent paragraph. Key in the sequence:
1. When energy transitions stall, industrial output contracts across secondary supply networks.
2. Grid operators must therefore maintain baseload thermal capacity during intermittent renewable deficits.
3. Decarbonizing industrial manufacturing requires synchronizing volatile power generation with continuous metallurgical furnaces.
4. This fundamental thermodynamic divergence exposes the vulnerability of unbuffered green electrical grids.',
'3412',
'Sentence 3 introduces the core problem (synchronizing volatile generation with continuous furnaces). Sentence 4 defines "this fundamental thermodynamic divergence". Sentence 1 elaborates on the consequence (industrial output contracts), and Sentence 2 provides the mandatory operational conclusion ("Grid operators must therefore..."). Hence, 3-4-1-2.',
'1. Identify opener: Sentence 3 introduces the overarching dilemma.\n2. Link 3 and 4: Sentence 4 refers to "This fundamental thermodynamic divergence".\n3. Sentence 1 follows showing consequence.\n4. Sentence 2 concludes with "therefore". Final order: 3412.',
150, 3.00, 0.00, 'VA,ParaJumbles,TITA', 'Published'),

-- VARC Verbal Ability (Para Summary - Topic 3006)
(3006, 3, 302, 3006, NULL, NULL, NULL, 'MCQ', 'Medium',
'Choose the option that best captures the essence of the passage:
Synthetic biology promises cellular re-engineering for pharmaceuticals, but genomic rewiring frequently triggers metabolic burden—diverting amino acid pools away from cellular homeostasis toward synthetic construct synthesis. Consequently, engineered chassis experience catastrophic population crashes in bioreactors before reaching commercial yield thresholds.',
'B',
'Option B succinctly states both the premise (synthetic genomic rewiring) and the fundamental bottleneck (metabolic resource diversion causing premature culture collapse).',
'1. Analyze core claims: Synthetic biology causes metabolic burden, amino acid redirection, leading to culture collapse.\n2. Eliminate options introducing external unmentioned ideas.\n3. Option B accurately encapsulates both cause and structural effect.',
120, 3.00, 1.00, 'VA,ParaSummary', 'Published');

-- Options for VARC MCQs
INSERT INTO question_options (question_id, option_key, content, is_correct, display_order)
VALUES
(3001, 'A', 'Because it enables individuals to execute exhaustive Bayesian probability calculations.', 0, 1),
(3001, 'B', 'Because it prevents cognitive paralysis in situations where exhaustive computational searches are intractable.', 1, 2),
(3001, 'C', 'Because emotional instincts invariably yield superior outcomes to rational deliberations.', 0, 3),
(3001, 'D', 'Because System 2 operates with zero metabolic expenditure in ancestral environments.', 0, 4),

(3002, 'A', 'Evidence showing that System 1 responds to visual stimuli faster than auditory stimuli.', 0, 1),
(3002, 'B', 'Evidence indicating that deliberative thinking requires glucose consumption in the prefrontal cortex.', 0, 2),
(3002, 'C', 'Brain imaging showing that deliberate analytical simulations generate hypothesis sets before heuristic associations activate.', 1, 3),
(3002, 'D', 'Studies demonstrating that fear responses are mediated by the amygdala.', 0, 4),

(3003, 'A', 'Argue that technological advancement has successfully eliminated evolutionary cognitive biases.', 0, 1),
(3003, 'B', 'Demonstrate that Bayesian rationality is now universally practiced in computer algorithms.', 0, 2),
(3003, 'C', 'Praise modern statistical algorithms for overcoming ancestral metabolic constraints.', 0, 3),
(3003, 'D', 'Explain why heuristics optimized for ancestral survival produce systematic distortions in complex, delayed-feedback settings.', 1, 4),

(3004, 'A', 'A subconscious heuristic mechanism that narrows down candidate hypotheses before deliberate focus is applied.', 1, 1),
(3004, 'B', 'A physical sensor in the prefrontal cortex that measures environmental radiation.', 0, 2),
(3004, 'C', 'A philosophical doctrine asserting that empirical knowledge is impossible.', 0, 3),
(3004, 'D', 'An algorithm designed to maximize energy dissipation during sleep.', 0, 4),

(3006, 'A', 'Synthetic biology is fundamentally unviable because microbes refuse to produce foreign enzymes.', 0, 1),
(3006, 'B', 'Cellular resource diversion caused by engineered constructs induces metabolic exhaustion, leading to premature bioreactor culture failure.', 1, 2),
(3006, 'C', 'Amino acid availability is the only variable determining pharmaceutical market prices.', 0, 3),
(3006, 'D', 'Bioreactor turbulence disrupts microbial cell walls independently of genetic modifications.', 0, 4)
ON DUPLICATE KEY UPDATE content = VALUES(content);

-- =============================================================================
-- DILR Questions (Question Set 1 - Round Robin Chess Tournament)
-- =============================================================================
INSERT INTO questions (id, subject_id, module_id, topic_id, subtopic_id, question_set_id, passage_id, question_type, difficulty_level, question_content, correct_answer, explanation, solution_steps, estimated_seconds, default_marks, default_penalty, tags, status)
VALUES
(2001, 2, 202, 2001, NULL, 1, NULL, 'MCQ', 'Hard',
'What was the final match score between Bangalore Bishops (BB) and Kolkata Queens (KQ)?',
'C',
'Total points for BB across all 3 matches is 8.0. BB scored 2.5 against DK. In BB vs MR, BB scored 3.0. Therefore, against KQ, BB scored 8.0 - 2.5 - 2.5 = 3.0 - 1.0 (or 2.5 - 1.5). Consistent grid deduction shows BB scored 3.0 against KQ while KQ scored 1.0.',
'1. Total BB game points = 8.0.\n2. Deduce BB match scores: BB vs DK = 2.5 - 1.5; BB vs MR = 2.5 - 1.5; BB vs KQ = 3.0 - 1.0.\n3. Verify KQ total = 1.0 (vs BB) + 1.0 (vs MR) + 2.5 (vs DK) = 4.5.\n4. Therefore BB vs KQ = 3.0 - 1.0.',
180, 3.00, 1.00, 'DILR,Tournament,GridLogic', 'Published'),

(2002, 2, 202, 2001, NULL, 1, NULL, 'TITA', 'Medium',
'How many total individual game points did Delhi Knights (DK) score in the entire tournament?',
'5.0',
'DK vs BB = 1.5. DK vs MR = 2.0. DK vs KQ = 1.5 (KQ scored 2.5). Total DK points = 1.5 + 2.0 + 1.5 = 5.0.',
'1. DK vs BB score: DK = 1.5.\n2. DK vs MR score: 2.0 (tie).\n3. DK vs KQ score: KQ had 4.5 total; KQ vs MR = 1.0, KQ vs BB = 1.0, so KQ vs DK = 2.5. DK scored 4.0 - 2.5 = 1.5.\n4. Sum = 1.5 + 2.0 + 1.5 = 5.0.',
150, 3.00, 0.00, 'DILR,Tournament,TITA', 'Published'),

(2003, 2, 202, 2001, NULL, 1, NULL, 'MCQ', 'Hard',
'Which team finished second in total individual game points across the tournament?',
'B',
'Standings: BB = 8.0 points. MR = 6.5 points (1.5 vs BB + 3.0 vs KQ + 2.0 vs DK = 6.5). DK = 5.0 points. KQ = 4.5 points. Mumbai Rooks (MR) finished second with 6.5 points.',
'1. BB = 8.0.\n2. MR = 1.5 (vs BB) + 3.0 (vs KQ) + 2.0 (vs DK) = 6.5.\n3. DK = 5.0.\n4. KQ = 4.5.\n5. Team in second place is Mumbai Rooks (MR).',
180, 3.00, 1.00, 'DILR,Tournament,Rankings', 'Published'),

(2004, 2, 202, 2001, NULL, 1, NULL, 'TITA', 'Medium',
'How many individual games ended in a draw in the match between Delhi Knights (DK) and Mumbai Rooks (MR)?',
'0',
'The match was 2 - 2 with White winning on Board 1 and Board 4, and Black winning on Board 2 and Board 3. No game ended in a draw (each game was a decisive win).',
'1. Total points = 2 - 2 (4 points total).\n2. Board 1 was won by White (1-0), Board 4 by White (1-0).\n3. To reach 2-2 without fractional points, the remaining boards were decisive wins for Black (0-1 each).\n4. Total draws = 0.',
120, 3.00, 0.00, 'DILR,Tournament,TITA', 'Published');

INSERT INTO question_options (question_id, option_key, content, is_correct, display_order)
VALUES
(2001, 'A', '2.0 - 2.0', 0, 1),
(2001, 'B', '2.5 - 1.5', 0, 2),
(2001, 'C', '3.0 - 1.0', 1, 3),
(2001, 'D', '3.5 - 0.5', 0, 4),

(2003, 'A', 'Bangalore Bishops (BB)', 0, 1),
(2003, 'B', 'Mumbai Rooks (MR)', 1, 2),
(2003, 'C', 'Delhi Knights (DK)', 0, 3),
(2003, 'D', 'Kolkata Queens (KQ)', 0, 4)
ON DUPLICATE KEY UPDATE content = VALUES(content);

-- =============================================================================
-- QA Questions (Arithmetic & Algebra - Topics 1001 to 1010)
-- =============================================================================
INSERT INTO questions (id, subject_id, module_id, topic_id, subtopic_id, question_set_id, passage_id, question_type, difficulty_level, question_content, correct_answer, explanation, solution_steps, estimated_seconds, default_marks, default_penalty, tags, status)
VALUES
-- QA 1001: Percentages (MCQ)
(1001, 1, 101, 1001, NULL, NULL, NULL, 'MCQ', 'Medium',
'In an engineering institute, 60% of students are male and the rest are female. 40% of male students and 70% of female students pass the qualifying exam. If the number of students who failed the exam is 240, what is the total number of students in the institute?',
'C',
'Let total students = 100x. Males = 60x, Females = 40x. Failed males = 60x * 0.60 = 36x. Failed females = 40x * 0.30 = 12x. Total failed = 48x. 48x = 240 => x = 5. Total = 100x = 500.',
'1. Let Total = 100x.\n2. Male = 60x; Female = 40x.\n3. Fail rates: Male fail = 60%, Female fail = 30%.\n4. Total fail = 0.60(60x) + 0.30(40x) = 36x + 12x = 48x.\n5. 48x = 240 => x = 5.\n6. Total students = 100 * 5 = 500.',
120, 3.00, 1.00, 'QA,Percentages,Arithmetic', 'Published'),

-- QA 1002: Profit & Loss (TITA)
(1002, 1, 101, 1002, NULL, NULL, NULL, 'TITA', 'Hard',
'A merchant marks up his goods by 40% above the cost price. He sells 60% of the stock at a discount of 15%, and the remaining 40% of the stock at a discount of 25%. What is his overall profit percentage? (Key in numerical integer or decimal value)',
'13.4',
'Let Cost Price = 100. Marked Price = 140. First portion SP = 140 * 0.85 = 119 on 0.6 stock = 71.4. Second portion SP = 140 * 0.75 = 105 on 0.4 stock = 42.0. Total SP = 71.4 + 42.0 = 113.4. Profit % = 13.4%.',
'1. Assume total CP = 100 for 1 unit of stock.\n2. MP = 140.\n3. Case 1: 0.6 stock sold at 15% discount: SP1 = 0.6 * 140 * 0.85 = 71.4.\n4. Case 2: 0.4 stock sold at 25% discount: SP2 = 0.4 * 140 * 0.75 = 42.0.\n5. Total SP = 71.4 + 42.0 = 113.4.\n6. Profit = 113.4 - 100 = 13.4%.',
120, 3.00, 0.00, 'QA,ProfitLoss,TITA', 'Published'),

-- QA 1006: Time & Work (MCQ)
(1003, 1, 101, 1006, NULL, NULL, NULL, 'MCQ', 'Medium',
'Pipe A can fill a tank in 12 hours, while Pipe B can fill it in 18 hours. A drain Pipe C can empty the full tank in 24 hours. If all three pipes are opened simultaneously when the tank is empty, how many hours will it take to fill the tank completely?',
'A',
'LCM(12, 18, 24) = 72 units. Efficiency of A = 6, B = 4, C = -3. Combined efficiency = 6 + 4 - 3 = 7 units/hr. Time required = 72 / 7 = 10.285 hours (10 hours and 2/7).',
'1. Find total tank capacity as LCM(12, 18, 24) = 72 units.\n2. Rate of A = +6 units/hr.\n3. Rate of B = +4 units/hr.\n4. Rate of C = -3 units/hr.\n5. Net rate = 6 + 4 - 3 = 7 units/hr.\n6. Time = 72 / 7 hours.',
120, 3.00, 1.00, 'QA,TimeAndWork', 'Published'),

-- QA 1007: Time, Speed & Distance (MCQ)
(1004, 1, 101, 1007, NULL, NULL, NULL, 'MCQ', 'Hard',
'Two trains A and B start simultaneously from stations P and Q towards each other. After crossing each other, Train A takes 4 hours 48 minutes to reach Q, while Train B takes 3 hours 20 minutes to reach P. If Train A travels at 45 km/h, what is the speed of Train B (in km/h)?',
'B',
'By ratio formula: Speed_A / Speed_B = sqrt(Time_B / Time_A). Time_A = 24/5 hrs = 4.8 hrs. Time_B = 10/3 hrs = 3.333 hrs. Ratio = sqrt((10/3) / (24/5)) = sqrt(50/72) = sqrt(25/36) = 5/6. 45 / Speed_B = 5/6 => Speed_B = 54 km/h.',
'1. Standard formula for meeting point: v1/v2 = sqrt(t2/t1).\n2. t1 = 4 hrs 48 mins = 4 + 48/60 = 24/5 hours.\n3. t2 = 3 hrs 20 mins = 3 + 20/60 = 10/3 hours.\n4. t2/t1 = (10/3) / (24/5) = 50 / 72 = 25 / 36.\n5. sqrt(25/36) = 5/6.\n6. 45 / v2 = 5/6 => v2 = 54 km/h.',
150, 3.00, 1.00, 'QA,TSD,Trains', 'Published'),

-- QA 1008: Quadratic Equations (TITA)
(1005, 1, 102, 1008, NULL, NULL, NULL, 'TITA', 'Medium',
'If alpha and beta are the roots of the quadratic equation x^2 - 7x + 12 = 0, find the value of (alpha^3 + beta^3).',
'91',
'Roots of x^2 - 7x + 12 = 0 are 3 and 4 (since 3+4=7, 3*4=12). alpha^3 + beta^3 = 3^3 + 4^3 = 27 + 64 = 91.',
'1. Recognize (x - 3)(x - 4) = 0, so roots are 3 and 4.\n2. alpha = 3, beta = 4.\n3. 3^3 = 27, 4^3 = 64.\n4. 27 + 64 = 91.\n5. Alternatively: (alpha + beta)^3 - 3*alpha*beta*(alpha + beta) = 7^3 - 3(12)(7) = 343 - 252 = 91.',
90, 3.00, 0.00, 'QA,Algebra,Quadratics,TITA', 'Published'),

-- QA 1010: Logarithms & Progressions (MCQ)
(1006, 1, 102, 1010, NULL, NULL, NULL, 'MCQ', 'Hard',
'If log_2(x) + log_4(x) + log_16(x) = 21/4, what is the value of x?',
'D',
'Change base to 2: log_2(x) + (1/2)*log_2(x) + (1/4)*log_2(x) = log_2(x) * (1 + 1/2 + 1/4) = (7/4)*log_2(x). Therefore (7/4)*log_2(x) = 21/4 => log_2(x) = 3 => x = 2^3 = 8.',
'1. Use log base change rule: log_{b^k}(x) = (1/k)*log_b(x).\n2. log_4(x) = (1/2)*log_2(x).\n3. log_16(x) = (1/4)*log_2(x).\n4. Sum = log_2(x) * [1 + 1/2 + 1/4] = log_2(x) * (7/4).\n5. (7/4)*log_2(x) = 21/4 => log_2(x) = 3.\n6. x = 2^3 = 8.',
120, 3.00, 1.00, 'QA,Algebra,Logarithms', 'Published');

INSERT INTO question_options (question_id, option_key, content, is_correct, display_order)
VALUES
(1001, 'A', '400', 0, 1),
(1001, 'B', '450', 0, 2),
(1001, 'C', '500', 1, 3),
(1001, 'D', '600', 0, 4),

(1003, 'A', '72/7 hours', 1, 1),
(1003, 'B', '10 hours', 0, 2),
(1003, 'C', '11 hours', 0, 3),
(1003, 'D', '72/5 hours', 0, 4),

(1004, 'A', '50 km/h', 0, 1),
(1004, 'B', '54 km/h', 1, 2),
(1004, 'C', '56 km/h', 0, 3),
(1004, 'D', '60 km/h', 0, 4),

(1006, 'A', '4', 0, 1),
(1006, 'B', '6', 0, 2),
(1006, 'C', '16', 0, 3),
(1006, 'D', '8', 1, 4)
ON DUPLICATE KEY UPDATE content = VALUES(content);

-- =============================================================================
-- 4. Seed Standard Mock Exam Templates & Sections
-- =============================================================================
INSERT INTO exam_templates (id, title, description, exam_type, total_duration_minutes, total_questions, total_marks, instructions, is_published, is_active)
VALUES
(1, 'CAT 2026 Diagnostic Benchmark Full Mock #1',
'Standard 3-section computer-based full CAT examination with authentic sectional timings (40 mins each) and strict forward navigation.',
'FullMock', 120, 16, 48.00,
'1. The test consists of 3 sections: Section I (VARC), Section II (DILR), Section III (QA).\n2. Each section has a fixed time limit of 40 minutes.\n3. Once the 40-minute sectional timer expires, the test automatically transitions to the next section.\n4. Backward navigation to previous sections is strictly prohibited.\n5. Scoring Scheme: Correct answer (+3 marks); Incorrect MCQ (-1 mark penalty); Incorrect TITA (0 penalty); Unanswered (0 marks).',
1, 1),

(2, 'Quantitative Aptitude High-Yield Sectional Test',
'40-minute focused sectional test covering high-yield Arithmetic, Algebra, and Modern Math questions with instant scoring and detailed step-by-step solutions.',
'SectionalMock', 40, 6, 18.00,
'1. 40 minutes duration for 6 curated QA problems.\n2. MCQ (+3 / -1), TITA (+3 / 0).\n3. Use on-screen palette to flag questions for review.',
1, 1),

(3, 'DILR Advanced Reasoning & Analytical Sprint',
'Targeted sectional mock focused on multi-constraint data interpretation sets and tournament logic puzzles.',
'SectionalMock', 40, 4, 12.00,
'1. 40 minutes duration for DILR scenario analysis.\n2. Read shared data table carefully before answering.',
1, 1)
ON DUPLICATE KEY UPDATE title = VALUES(title);

-- Exam Sections for Template 1 (Full Mock)
INSERT INTO exam_sections (id, exam_template_id, section_name, subject_id, display_order, duration_minutes, question_count, can_navigate_back, correct_marks, incorrect_penalty)
VALUES
(101, 1, 'Verbal Ability & Reading Comprehension', 3, 1, 40, 6, 0, 3.00, 1.00),
(102, 1, 'Data Interpretation & Logical Reasoning', 2, 2, 40, 4, 0, 3.00, 1.00),
(103, 1, 'Quantitative Aptitude', 1, 3, 40, 6, 0, 3.00, 1.00),

-- Exam Section for Template 2 (QA Sectional)
(201, 2, 'Quantitative Aptitude Sectional', 1, 1, 40, 6, 0, 3.00, 1.00),

-- Exam Section for Template 3 (DILR Sectional)
(301, 3, 'Data Interpretation & Logical Reasoning Sectional', 2, 1, 40, 4, 0, 3.00, 1.00)
ON DUPLICATE KEY UPDATE section_name = VALUES(section_name);

-- Link Questions to Exam Sections
-- Template 1 - Section 101 (VARC)
INSERT INTO exam_section_questions (exam_section_id, question_id, display_order)
VALUES
(101, 3001, 1),
(101, 3002, 2),
(101, 3003, 3),
(101, 3004, 4),
(101, 3005, 5),
(101, 3006, 6),

-- Template 1 - Section 102 (DILR)
(102, 2001, 1),
(102, 2002, 2),
(102, 2003, 3),
(102, 2004, 4),

-- Template 1 - Section 103 (QA)
(103, 1001, 1),
(103, 1002, 2),
(103, 1003, 3),
(103, 1004, 4),
(103, 1005, 5),
(103, 1006, 6),

-- Template 2 - Section 201 (QA Sectional)
(201, 1001, 1),
(201, 1002, 2),
(201, 1003, 3),
(201, 1004, 4),
(201, 1005, 5),
(201, 1006, 6),

-- Template 3 - Section 301 (DILR Sectional)
(301, 2001, 1),
(301, 2002, 2),
(301, 2003, 3),
(301, 2004, 4)
ON DUPLICATE KEY UPDATE display_order = VALUES(display_order);

