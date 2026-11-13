-- =============================================================================
-- Migration 004: Default CAT Syllabus Catalog Seed Data
-- Phase 4 - Bloom (My CAT Journey)
-- =============================================================================

-- 1. Insert Default Subjects (QA, DILR, VARC)
INSERT INTO syllabus_subjects (id, user_id, code, name, description, display_order, is_active)
VALUES
(1, NULL, 'QA', 'Quantitative Aptitude', 'Mathematical foundation, arithmetic reasoning, algebra, geometry, and modern math.', 1, 1),
(2, NULL, 'DILR', 'Data Interpretation & Logical Reasoning', 'Complex data sets, analytical deduction, arrangements, matrices, and games.', 2, 1),
(3, NULL, 'VARC', 'Verbal Ability & Reading Comprehension', 'Reading comprehension passages, critical reasoning, para jumbles, and vocabulary in context.', 3, 1)
ON DUPLICATE KEY UPDATE name = VALUES(name);

-- 2. Insert Default Modules
-- QA Modules (Subject ID 1)
INSERT INTO syllabus_modules (id, subject_id, user_id, name, description, display_order, is_active)
VALUES
(101, 1, NULL, 'Arithmetic', 'Core high-yield section: Percentages, Profit & Loss, SI/CI, Ratios, Averages, Time & Work, TSD.', 1, 1),
(102, 1, NULL, 'Algebra', 'Linear and quadratic equations, polynomials, inequalities, logarithms, progressions, and functions.', 2, 1),
(103, 1, NULL, 'Geometry & Mensuration', 'Triangles, circles, quadrilaterals, coordinate geometry, trigonometry, and solid mensuration.', 3, 1),
(104, 1, NULL, 'Number Systems', 'Properties of numbers, divisibility, factors, multiples, LCM/HCF, remainders, and base systems.', 4, 1),
(105, 1, NULL, 'Modern Mathematics', 'Permutations & combinations, probability, set theory, and Venn diagrams.', 5, 1),

-- DILR Modules (Subject ID 2)
(201, 2, NULL, 'Data Interpretation', 'Tables, line graphs, bar charts, pie charts, radar graphs, and mixed caselets.', 1, 1),
(202, 2, NULL, 'Logical Reasoning', 'Arrangements (linear/circular), grid matching, blood relations, directions, and binary logic.', 2, 1),
(203, 2, NULL, 'Advanced Analytical Reasoning', 'Games & tournaments, scheduling, network diagrams, and quantitative reasoning puzzles.', 3, 1),

-- VARC Modules (Subject ID 3)
(301, 3, NULL, 'Reading Comprehension', 'Philosophy, science, sociology, economics, arts passages, main ideas, and tone analysis.', 1, 1),
(302, 3, NULL, 'Verbal Ability', 'Para jumbles (TITA), para summary, odd sentence out, and paragraph completion.', 2, 1),
(303, 3, NULL, 'Critical Reasoning', 'Strengthening/weakening arguments, assumptions, inferences, and logical flaws.', 3, 1)
ON DUPLICATE KEY UPDATE name = VALUES(name);

-- 3. Insert Default Topics
-- QA - Arithmetic (Module 101)
INSERT INTO syllabus_topics (id, module_id, user_id, name, description, estimated_study_hours, difficulty_level, importance_level, display_order, is_active)
VALUES
(1001, 101, NULL, 'Percentages & Fraction Equivalence', 'Foundational arithmetic conversions, successive changes, and multiplier techniques.', 4.0, 'Easy', 'High', 1, 1),
(1002, 101, NULL, 'Profit, Loss & Discount', 'Cost/Selling price dynamics, marked price, faulty balances, and successive discounts.', 5.0, 'Medium', 'High', 2, 1),
(1003, 101, NULL, 'Simple & Compound Interest', 'Annual, semi-annual compounding, difference formulas, and loan installments.', 4.5, 'Medium', 'Medium', 3, 1),
(1004, 101, NULL, 'Ratios, Proportions & Variations', 'Compounded ratios, direct/inverse variation, and partnership investments.', 4.0, 'Easy', 'High', 4, 1),
(1005, 101, NULL, 'Averages, Mixtures & Alligations', 'Weighted averages, replacement formulas, and cross-alligation methods.', 6.0, 'Medium', 'High', 5, 1),
(1006, 101, NULL, 'Time and Work', 'Unit work method, efficiency ratios, pipes & cisterns, and alternate work days.', 6.0, 'Medium', 'High', 6, 1),
(1007, 101, NULL, 'Time, Speed & Distance', 'Relative speed, trains, boats & streams, circular tracks, and race problems.', 8.0, 'Hard', 'High', 7, 1),

-- QA - Algebra (Module 102)
(1008, 102, NULL, 'Linear & Quadratic Equations', 'Roots properties, discriminant analysis, signs of roots, and word problems.', 5.5, 'Medium', 'High', 1, 1),
(1009, 102, NULL, 'Polynomials & Algebraic Identities', 'Remainder theorem, factor theorem, and symmetric polynomial expansions.', 4.0, 'Medium', 'Medium', 2, 1),
(1010, 102, NULL, 'Inequalities & Modulus', 'Wavy curve method, absolute value equations, AM-GM inequality, and Cauchy-Schwarz.', 6.0, 'Hard', 'High', 3, 1),
(1011, 102, NULL, 'Logarithms & Indices', 'Fundamental laws, change of base rule, characteristic & mantissa, and log equations.', 4.5, 'Medium', 'High', 4, 1),
(1012, 102, NULL, 'Progressions & Series (AP, GP, HP)', 'Sum of series, arithmetic-geometric progression (AGP), and telescopic summation.', 5.0, 'Medium', 'High', 5, 1),
(1013, 102, NULL, 'Functions & Graphs', 'Domain & range, composite & inverse functions, transformations, and maxima-minima.', 7.0, 'Hard', 'High', 6, 1),

-- QA - Geometry & Mensuration (Module 103)
(1014, 103, NULL, 'Triangles & Polygons', 'Similarity, congruence, angle bisector theorem, Apollonius theorem, and area formulas.', 7.0, 'Medium', 'High', 1, 1),
(1015, 103, NULL, 'Circles & Tangents', 'Chords, secants, cyclic quadrilaterals, tangent-secant theorem, and common tangents.', 6.5, 'Hard', 'High', 2, 1),
(1016, 103, NULL, 'Coordinate Geometry', 'Distance formula, section formula, slope, line equations, and circle equations.', 4.5, 'Medium', 'Medium', 3, 1),
(1017, 103, NULL, 'Mensuration 2D & 3D', 'Perimeters, surface areas, volumes of prisms, pyramids, cones, and composite solids.', 5.0, 'Medium', 'Medium', 4, 1),

-- QA - Number Systems (Module 104)
(1018, 104, NULL, 'Divisibility & Factors', 'Prime factorization, number of factors, sum of factors, and coprime concepts.', 4.0, 'Easy', 'Medium', 1, 1),
(1019, 104, NULL, 'LCM & HCF', 'Product properties, division algorithm models, and remainder relations.', 3.5, 'Medium', 'Medium', 2, 1),
(1020, 104, NULL, 'Remainders, Totient & Fermat Theorems', 'Euler totient, Fermat little theorem, Wilson theorem, and cyclicity patterns.', 6.0, 'Hard', 'Medium', 3, 1),
(1021, 104, NULL, 'Highest Power of Prime & Factorials', 'Legendre formula, trailing zeroes, and base conversions.', 4.0, 'Medium', 'Low', 4, 1),

-- QA - Modern Math (Module 105)
(1022, 105, NULL, 'Permutations & Combinations', 'Fundamental counting principles, circular permutations, derangements, and partitions.', 7.0, 'Hard', 'High', 1, 1),
(1023, 105, NULL, 'Probability', 'Conditional probability, Bayes theorem, geometric probability, and binomial distributions.', 6.0, 'Hard', 'High', 2, 1),
(1024, 105, NULL, 'Set Theory & Venn Diagrams', '2-set and 3-set Venn diagrams, maxima-minima set intersections, and Cartesian products.', 5.0, 'Medium', 'High', 3, 1),

-- DILR - Data Interpretation (Module 201)
(2001, 201, NULL, 'Tables & Missing Data Tables', 'Tabular calculation efficiency, speed approximation, and percentage indexing.', 5.0, 'Medium', 'High', 1, 1),
(2002, 201, NULL, 'Bar Charts & Stacked Columns', 'Comparative trends, growth rates, stacked shares, and deviation bars.', 5.0, 'Medium', 'High', 2, 1),
(2003, 201, NULL, 'Line Graphs & Multi-trend Charts', 'Trend interpolation, crossover analysis, and rate-of-change estimation.', 4.5, 'Medium', 'High', 3, 1),
(2004, 201, NULL, 'Pie Charts with Multiple Variables', 'Degree to percentage conversions, combined pie charts, and ratio-constrained pies.', 5.0, 'Medium', 'High', 4, 1),
(2005, 201, NULL, 'Mixed & Unconventional Charts', 'Radar charts, scatter plots, bubble charts, and caselet data interpretation.', 6.0, 'Hard', 'High', 5, 1),

-- DILR - Logical Reasoning (Module 202)
(2006, 202, NULL, 'Linear Arrangements', 'Single row, facing North/South, double rows, and conditional positioning.', 5.5, 'Medium', 'High', 1, 1),
(2007, 202, NULL, 'Circular Arrangements & Polygons', 'Inward/outward facing circles, square tables, and multi-attribute seating.', 6.0, 'Hard', 'High', 2, 1),
(2008, 202, NULL, 'Matrix Matching & Attribute Grids', '3-attribute and 4-attribute matrix grids with elimination deduction.', 6.0, 'Medium', 'High', 3, 1),
(2009, 202, NULL, 'Binary Logic & Truth-Tellers', 'Truth-teller, liar, and alternator logic puzzles with case splitting.', 5.5, 'Hard', 'Medium', 4, 1),

-- DILR - Advanced Analytical Reasoning (Module 203)
(2010, 203, NULL, 'Games & Tournaments', 'Round-robin, knockout stages, seeded tournaments, and tennis/chess score sheets.', 8.0, 'Hard', 'High', 1, 1),
(2011, 203, NULL, 'Quantitative Reasoning Puzzles', 'Algebraic DILR sets, optimization problems, and constraint satisfactions.', 7.5, 'Hard', 'High', 2, 1),
(2012, 203, NULL, 'Networks & Routes', 'Flow diagrams, pipelines, network maximums, and critical path analysis.', 5.5, 'Hard', 'Medium', 3, 1),

-- VARC - Reading Comprehension (Module 301)
(3001, 301, NULL, 'Philosophy & Psychology Passages', 'Abstract arguments, epistemological theories, and existentialist reasoning.', 6.0, 'Hard', 'High', 1, 1),
(3002, 301, NULL, 'Economics & Policy Passages', 'Market behavior, international trade, inflation dynamics, and policy critiques.', 5.0, 'Medium', 'High', 2, 1),
(3003, 301, NULL, 'Science, Technology & Ecology', 'Evolutionary biology, quantum computing, climate science, and environmental ethics.', 5.0, 'Medium', 'High', 3, 1),
(3004, 301, NULL, 'Sociology, History & Art', 'Cultural anthropology, historical analysis, literary critique, and post-colonial studies.', 5.0, 'Medium', 'High', 4, 1),
(3005, 301, NULL, 'RC Question Type Mastery', 'Inference questions, author tone, main idea, primary purpose, and except/must-be-true.', 6.0, 'Medium', 'High', 5, 1),

-- VARC - Verbal Ability (Module 302)
(3006, 302, NULL, 'Para Jumbles (TITA)', 'Mandatory pairs, pronoun-antecedent links, chronology markers, and opening/closing lines.', 5.5, 'Hard', 'High', 1, 1),
(3007, 302, NULL, 'Para Summary', 'Core premise identification, distortion elimination, and conciseness rules.', 5.0, 'Medium', 'High', 2, 1),
(3008, 302, NULL, 'Odd Sentence Out', 'Theme divergence, scope mismatch, and structural misfit detection.', 4.5, 'Medium', 'High', 3, 1),
(3009, 302, NULL, 'Paragraph Completion & Inferences', 'Concluding sentence deduction, coherent narrative transitions, and logical flow.', 4.5, 'Medium', 'Medium', 4, 1),

-- VARC - Critical Reasoning (Module 303)
(3010, 303, NULL, 'Arguments, Assumptions & Flaws', 'Identifying unstated assumptions, flaw in reasoning, and paradox resolution.', 5.0, 'Medium', 'High', 1, 1),
(3011, 303, NULL, 'Strengthening & Weakening Logic', 'Counter-evidence impact, alternative causality, and controlled experiment evaluations.', 5.0, 'Medium', 'High', 2, 1)
ON DUPLICATE KEY UPDATE name = VALUES(name);

-- 4. Insert Sample High-Yield Subtopics
INSERT INTO syllabus_subtopics (id, topic_id, user_id, name, description, display_order, is_active)
VALUES
-- Subtopics for Percentages (1001)
(10001, 1001, NULL, 'Fraction to Percentage Equivalents (1/1 to 1/25)', 'Memorizing standard fractions for rapid mental calculation.', 1, 1),
(10002, 1001, NULL, 'Successive Percentage Changes (AB Formula)', 'Calculating net effects of consecutive discounts and compounding growths.', 2, 1),
(10003, 1001, NULL, 'Product Constancy Method', 'Handling inverse variations where Expenditure = Price × Consumption.', 3, 1),
(10004, 1001, NULL, 'Base Change Dynamics', 'Handling comparative expressions like "A is x% more than B, B is what % less than A?".', 4, 1),

-- Subtopics for Time & Work (1006)
(10005, 1006, NULL, 'Total Work LCM Method', 'Expressing work as LCM of individual times to determine per-day efficiency.', 1, 1),
(10006, 1006, NULL, 'Efficiency and Wage Sharing Ratios', 'Apportioning compensation in direct proportion to work performed.', 2, 1),
(10007, 1006, NULL, 'Pipes, Cisterns & Negative Work', 'Inlet and leak dynamics with alternating operation schedules.', 3, 1),
(10008, 1006, NULL, 'Men, Women & Children Equivalence', 'Converting composite groups to unified efficiency units.', 4, 1),

-- Subtopics for Quadratic Equations (1008)
(10009, 1008, NULL, 'Sum and Product of Roots (Vieta Relations)', 'Forming equations given root characteristics and relationships.', 1, 1),
(10010, 1008, NULL, 'Location of Roots & Boundary Conditions', 'Determining parameter ranges for roots lying in specified intervals.', 2, 1),
(10011, 1008, NULL, 'Common Roots Conditions', 'Equations sharing one common root or both common roots.', 3, 1),

-- Subtopics for Games & Tournaments (2010)
(10012, 2010, NULL, 'Round-Robin Tournaments with Win/Draw/Loss', 'Points table deductions, minimum/maximum points to qualify.', 1, 1),
(10013, 2010, NULL, 'Knockout Tournaments & Seedings', 'Upset dynamics, matches between specified seeds, and wildcard entries.', 2, 1),

-- Subtopics for Para Jumbles (3006)
(10014, 3006, NULL, 'Acronym and Full Form Pairing', 'Sentences introducing full entities before using shortened titles.', 1, 1),
(10015, 3006, NULL, 'Chronological and Causality Sequences', 'Time markers (firstly, then, subsequently, finally) and consequence words.', 2, 1),
(10016, 3006, NULL, 'Pronoun and Demonstrative Clues', 'Resolving "this", "these", "such", "those" to their introducing nouns.', 3, 1)
ON DUPLICATE KEY UPDATE name = VALUES(name);

-- 5. Insert Curated Learning Resources
INSERT INTO syllabus_resources (id, topic_id, user_id, title, resource_type, url, notes, display_order)
VALUES
(1, 1001, NULL, 'Fraction-to-Percentage Quick Reference Card', 'FormulaSheet', NULL, 'Standard conversions up to 1/25 with decimal representations.', 1),
(2, 1006, NULL, 'Pipes and Cisterns High-Yield Problem Framework', 'Notes', NULL, 'Step-by-step framework for negative efficiency problems.', 1),
(3, 1008, NULL, 'Vieta Formulas & Graph Transformations', 'Article', NULL, 'Visual representation of roots and vertex coordinates.', 1),
(4, 2010, NULL, 'Standard Tournament Deduction Guide', 'Article', NULL, 'Rules for calculating total matches in single and double round-robins.', 1),
(5, 3001, NULL, 'Reading Dense Philosophy Texts for CAT', 'Notes', NULL, 'Techniques for tracking thesis statements across convoluted prose.', 1)
ON DUPLICATE KEY UPDATE title = VALUES(title);

-- Register migration in history table
INSERT IGNORE INTO schema_migrations (version, description) 
VALUES ('004', 'Phase 4: Default CAT Syllabus Catalog Seed Data (QA, DILR, VARC)');

