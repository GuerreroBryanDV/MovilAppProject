-- PB25 · MAP-25 · Respaldo de la base de GastoFácil en MySQL (XAMPP)
-- Importar en phpMyAdmin: pestaña "Importar" > este archivo > "Continuar".
-- Las tablas tienen los mismos datos que la base SQLite de la app
-- (grupo_presupuesto, categoria y movimiento) y los mismos Id.

CREATE DATABASE IF NOT EXISTS gastofacil_ws
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE gastofacil_ws;

-- Grupos de la regla 50/30/20 (fijos)
CREATE TABLE IF NOT EXISTS grupo_presupuesto (
  codigo INT PRIMARY KEY,
  nombre VARCHAR(40) NOT NULL,
  orden  INT NOT NULL
);

-- Categorías predefinidas (fijas, mismo Id que en SQLite)
CREATE TABLE IF NOT EXISTS categoria (
  codigo   INT PRIMARY KEY,
  nombre   VARCHAR(40) NOT NULL UNIQUE,
  grupo_id INT NULL,
  FOREIGN KEY (grupo_id) REFERENCES grupo_presupuesto(codigo)
);

-- Movimientos: codigo = Id del movimiento en SQLite
CREATE TABLE IF NOT EXISTS movimiento (
  codigo       INT PRIMARY KEY,
  tipo         VARCHAR(10) NOT NULL,          -- Gasto / Ingreso
  monto        DECIMAL(10,2) NOT NULL,
  categoria_id INT NOT NULL,
  fecha        DATE NOT NULL,
  nota         VARCHAR(100) NULL,
  FOREIGN KEY (categoria_id) REFERENCES categoria(codigo)
);

INSERT IGNORE INTO grupo_presupuesto (codigo, nombre, orden) VALUES
  (1, 'Necesidades', 1),
  (2, 'Ahorro e inversión', 2),
  (3, 'Lujos', 3);

INSERT IGNORE INTO categoria (codigo, nombre, grupo_id) VALUES
  (1,  'Comida', 1),
  (2,  'Vivienda', 1),
  (3,  'Salud', 1),
  (4,  'Transporte', 1),
  (5,  'Servicios', 1),
  (6,  'Fondo de emergencia', 2),
  (7,  'Educación', 2),
  (8,  'Invertir', 2),
  (9,  'Restaurantes', 3),
  (10, 'Entretenimiento', 3),
  (11, 'Vacaciones', 3),
  (12, 'Electrónicos', 3),
  (13, 'Ingreso', NULL);
