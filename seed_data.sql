-- Insert sample energy readings for last 7 days (50 readings across 5 nodes)
INSERT INTO EnergyReadings (NodeId, UserId, Consumption, Production, Voltage, Current, PowerFactor, Frequency, MeterId, Timestamp)
SELECT 
    node_id,
    1 AS UserId,
    ROUND(150 + RAND() * 250, 2) AS Consumption,
    ROUND(80 + RAND() * 180, 2) AS Production,
    ROUND(215 + RAND() * 28, 2) AS Voltage,
    ROUND(8 + RAND() * 18, 2) AS Current,
    ROUND(0.78 + RAND() * 0.18, 3) AS PowerFactor,
    ROUND(49.7 + RAND() * 0.5, 2) AS Frequency,
    CONCAT('MTR-', LPAD(node_id, 3, '0'), '-', LPAD(FLOOR(RAND() * 9999), 4, '0')) AS MeterId,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL (FLOOR(RAND() * 160)) HOUR) AS Timestamp
FROM (
    SELECT 1 AS node_id UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
) nodes
CROSS JOIN (
    SELECT 1 AS r UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
    UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10
) reps;

-- Insert sample faults (correct columns: FaultType, Description, Severity, Status)
INSERT INTO Faults (NodeId, ReportedByUserId, FaultType, Description, Severity, Status, ReportedAt)
VALUES
  (1, 1, 'Voltage', 'Voltage sag detected - voltage dropped below 210V threshold at Downtown Substation during peak hours. Possible causes: heavy load or transmission line issue.', 'High', 'Reported', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 48 HOUR)),
  (2, 1, 'PowerFactor', 'Power factor measured at 0.71 - below acceptable 0.85 threshold at Industrial Zone. Capacitor bank adjustment required.', 'Medium', 'InProgress', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 24 HOUR)),
  (3, 1, 'Overload', 'Current load approaching 95% of maximum capacity at Residential Area North. Load shedding may be required.', 'Critical', 'Reported', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 4 HOUR)),
  (4, 1, 'Frequency', 'Grid frequency deviation detected: 49.6 Hz at Commercial District. Generator governor adjustment applied.', 'Low', 'Resolved', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 72 HOUR)),
  (1, 1, 'Equipment Failure', 'Main transformer temperature sensor alarm triggered at Downtown Substation. Cooling system inspection needed.', 'Critical', 'InProgress', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 6 HOUR));

-- Set resolved fault time
UPDATE Faults SET ResolvedAt = DATE_SUB(UTC_TIMESTAMP(), INTERVAL 48 HOUR) WHERE Status = 'Resolved';

-- Insert sample outages (correct columns: AffectedArea, AffectedCustomers, Status, OutageType, Cause)
INSERT INTO Outages (NodeId, AffectedArea, AffectedCustomers, Status, OutageType, Cause, StartedAt, EstimatedRestorationTime, ReportedByUserId)
VALUES
  (3, 'Residential Area North - Sectors 4, 5 and 6', 2450, 'Ongoing', 'Unplanned', 'Main feeder circuit breaker tripped. Investigation ongoing - possible equipment failure.', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 HOUR), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 3 HOUR), 1),
  (5, 'Eastern Substation Grid Zone', 850, 'Restored', 'Unplanned', 'Tree fall damaged overhead transmission lines during storm. Lines repaired and energized.', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 50 HOUR), NULL, 1);

-- Set restored outage time
UPDATE Outages SET RestoredAt = DATE_SUB(UTC_TIMESTAMP(), INTERVAL 24 HOUR) WHERE Status = 'Restored';

-- Verify counts
SELECT 'EnergyReadings' AS TableName, COUNT(*) AS RecordCount FROM EnergyReadings
UNION ALL SELECT 'Faults', COUNT(*) FROM Faults
UNION ALL SELECT 'Outages', COUNT(*) FROM Outages
UNION ALL SELECT 'GridNodes', COUNT(*) FROM GridNodes
UNION ALL SELECT 'Users', COUNT(*) FROM Users;
