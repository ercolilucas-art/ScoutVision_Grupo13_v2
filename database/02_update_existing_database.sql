-- ============================================================
-- ScoutVision v2 - ACTUALIZACIÓN PARA LA BASE QUE YA TENÉS
-- Ejecutar conectado a la base scoutvision.
-- Agrega registro de usuarios y actualiza equipos/jugadores.
-- ============================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1) Actualizar tabla de usuarios existente
ALTER TABLE app_user ADD COLUMN IF NOT EXISTS email VARCHAR(160);

UPDATE app_user
SET email = 'scout@scoutvision.local'
WHERE email IS NULL;

ALTER TABLE app_user ALTER COLUMN email SET NOT NULL;
ALTER TABLE app_user DROP COLUMN IF EXISTS full_name;

CREATE UNIQUE INDEX IF NOT EXISTS ux_app_user_email_lower
ON app_user (LOWER(email));

CREATE UNIQUE INDEX IF NOT EXISTS ux_app_user_username_lower
ON app_user (LOWER(username));

-- 2) Reemplazar datos deportivos de demostración
TRUNCATE TABLE player_performance, football_match, player, team RESTART IDENTITY CASCADE;

INSERT INTO team (name, city) VALUES
('Boca Juniors', 'Buenos Aires'),
('River Plate', 'Buenos Aires'),
('Newell''s Old Boys', 'Rosario'),
('Unión de Santa Fe', 'Santa Fe');

INSERT INTO player (full_name, position, team_id) VALUES
('Leandro Brey', 'Arquero', (SELECT id FROM team WHERE name = 'Boca Juniors')),
('Lautaro Blanco', 'Defensor', (SELECT id FROM team WHERE name = 'Boca Juniors')),
('Carlos Palacios', 'Mediocampista', (SELECT id FROM team WHERE name = 'Boca Juniors')),
('Enner Valencia', 'Delantero', (SELECT id FROM team WHERE name = 'Boca Juniors')),
('Marcos Acuña', 'Defensor', (SELECT id FROM team WHERE name = 'River Plate')),
('Thiago Almada', 'Mediocampista', (SELECT id FROM team WHERE name = 'River Plate')),
('Sebastián Driussi', 'Delantero', (SELECT id FROM team WHERE name = 'River Plate')),
('Ángel Correa', 'Delantero', (SELECT id FROM team WHERE name = 'River Plate')),
('Gabriel Arias', 'Arquero', (SELECT id FROM team WHERE name = 'Newell''s Old Boys')),
('Franco Escobar', 'Defensor', (SELECT id FROM team WHERE name = 'Newell''s Old Boys')),
('Alan Soñora', 'Mediocampista', (SELECT id FROM team WHERE name = 'Newell''s Old Boys')),
('Matías Cóccaro', 'Delantero', (SELECT id FROM team WHERE name = 'Newell''s Old Boys')),
('Federico Gomes Gerth', 'Arquero', (SELECT id FROM team WHERE name = 'Unión de Santa Fe')),
('Ignacio Malcorra', 'Mediocampista', (SELECT id FROM team WHERE name = 'Unión de Santa Fe')),
('Cristian Tarragona', 'Delantero', (SELECT id FROM team WHERE name = 'Unión de Santa Fe')),
('Eric Ramírez', 'Delantero', (SELECT id FROM team WHERE name = 'Unión de Santa Fe'));

-- Métricas ficticias/de demostración para probar la semaforización.
INSERT INTO player_performance
(player_id, season, goals, assists, minutes_played, performance_index)
SELECT id, 2026,
       CASE (id % 4) WHEN 0 THEN 8 WHEN 1 THEN 2 WHEN 2 THEN 5 ELSE 4 END,
       CASE (id % 4) WHEN 0 THEN 3 WHEN 1 THEN 5 WHEN 2 THEN 2 ELSE 6 END,
       900 + id * 55,
       CASE
           WHEN id IN (2,6,10,14) THEN 82.00
           WHEN id IN (3,7,11,15) THEN 67.00
           WHEN id IN (4,8,12,16) THEN 44.00
           ELSE 74.00
       END
FROM player;

INSERT INTO football_match (match_date, home_team_id, away_team_id, competition) VALUES
('2026-08-10', (SELECT id FROM team WHERE name='Boca Juniors'), (SELECT id FROM team WHERE name='River Plate'), 'Liga Profesional'),
('2026-08-17', (SELECT id FROM team WHERE name='Newell''s Old Boys'), (SELECT id FROM team WHERE name='Unión de Santa Fe'), 'Liga Profesional'),
('2026-09-06', (SELECT id FROM team WHERE name='River Plate'), (SELECT id FROM team WHERE name='Newell''s Old Boys'), 'Liga Profesional'),
('2026-09-12', (SELECT id FROM team WHERE name='Unión de Santa Fe'), (SELECT id FROM team WHERE name='Boca Juniors'), 'Liga Profesional');

-- Comprobaciones opcionales:
-- SELECT id, email, username, password_hash FROM app_user;
-- SELECT p.full_name, t.name, pr.performance_index
-- FROM player p JOIN team t ON t.id=p.team_id
-- JOIN player_performance pr ON pr.player_id=p.id
-- ORDER BY t.name, p.full_name;
