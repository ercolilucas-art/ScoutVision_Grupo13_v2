-- ScoutVision - creación de la base
-- Ejecutar este archivo conectado a la base de mantenimiento "postgres".
-- Luego conectarse a la base "scoutvision" y ejecutar 01_create_database.sql.

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'scoutvision'
  AND pid <> pg_backend_pid();

DROP DATABASE IF EXISTS scoutvision;
CREATE DATABASE scoutvision;
