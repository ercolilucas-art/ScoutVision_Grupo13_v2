-- ScoutVision - Grupo 13
-- Agustin Dalton / Lucas Ercoli
-- PostgreSQL

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP TABLE IF EXISTS player_performance CASCADE;
DROP TABLE IF EXISTS football_match CASCADE;
DROP TABLE IF EXISTS player CASCADE;
DROP TABLE IF EXISTS team CASCADE;
DROP TABLE IF EXISTS app_user CASCADE;

CREATE TABLE app_user (
    id SERIAL PRIMARY KEY,
    email VARCHAR(160) NOT NULL,
    username VARCHAR(60) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX ux_app_user_email_lower ON app_user (LOWER(email));
CREATE UNIQUE INDEX ux_app_user_username_lower ON app_user (LOWER(username));

CREATE TABLE team (
    id SERIAL PRIMARY KEY,
    name VARCHAR(120) NOT NULL UNIQUE,
    city VARCHAR(120)
);

CREATE TABLE player (
    id SERIAL PRIMARY KEY,
    full_name VARCHAR(120) NOT NULL,
    position VARCHAR(60) NOT NULL,
    age INTEGER CHECK (age BETWEEN 15 AND 50),
    team_id INTEGER NOT NULL REFERENCES team(id)
);

CREATE TABLE football_match (
    id SERIAL PRIMARY KEY,
    match_date DATE NOT NULL,
    home_team_id INTEGER NOT NULL REFERENCES team(id),
    away_team_id INTEGER NOT NULL REFERENCES team(id),
    competition VARCHAR(120) NOT NULL
);

CREATE TABLE player_performance (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL REFERENCES player(id) ON DELETE CASCADE,
    season INTEGER NOT NULL,
    goals INTEGER NOT NULL DEFAULT 0,
    assists INTEGER NOT NULL DEFAULT 0,
    minutes_played INTEGER NOT NULL DEFAULT 0,
    performance_index NUMERIC(5,2) NOT NULL CHECK (performance_index BETWEEN 0 AND 100),
    UNIQUE(player_id, season)
);

-- Cuenta demo. La contraseña se almacena con bcrypt.
INSERT INTO app_user (email, username, password_hash)
VALUES ('scout@scoutvision.local', 'scout_analista', crypt('Scout2026!', gen_salt('bf', 10)));

INSERT INTO team (name, city) VALUES
('Boca Juniors', 'Buenos Aires'),
('River Plate', 'Buenos Aires'),
('Newell''s Old Boys', 'Rosario'),
('Unión de Santa Fe', 'Santa Fe');

-- Jugadores actuales consultados en septiembre de 2026.
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

-- IMPORTANTE: estas métricas son datos de demostración para el trabajo académico.
-- No representan estadísticas oficiales de los futbolistas.
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
