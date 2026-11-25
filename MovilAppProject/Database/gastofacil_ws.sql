-- Base de respaldo de GastoFácil en MySQL (PB25)
-- Importar en phpMyAdmin: pestaña "Importar" > este archivo > "Continuar".
-- Las tablas tienen los mismos datos que la base SQLite de la app
-- (grupo_presupuesto, categoria, usuario y movimiento) y los mismos Id.
-- Si ya importaste una versión anterior, no hace falta volver a importarla: el servicio web
-- crea la tabla usuario y la columna movimiento.usuario_uid solo (función migrar en utils.php).

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

-- Usuarios del login (uno activo por dispositivo). Borrado lógico: activo = 0 y fecha_baja.
-- La contraseña y el PIN no se guardan aquí: se quedan cifrados en el teléfono.
CREATE TABLE IF NOT EXISTS usuario (
  uid            VARCHAR(36) PRIMARY KEY,
  nombre         VARCHAR(20) NOT NULL,
  activo         TINYINT(1)  NOT NULL DEFAULT 1,
  fecha_creacion DATETIME    NOT NULL,
  fecha_baja     DATETIME    NULL
);
-- Movimientos: codigo = Id del movimiento en SQLite
CREATE TABLE IF NOT EXISTS movimiento (
  codigo       INT PRIMARY KEY,
  tipo         VARCHAR(10) NOT NULL,          -- Gasto / Ingreso
  monto        DECIMAL(10,2) NOT NULL,
  categoria_id INT NOT NULL,
  fecha        DATE NOT NULL,
  nota         VARCHAR(100) NULL,
  usuario_uid  VARCHAR(36) NULL,               -- Dueño (NULL = anterior al login)
  FOREIGN KEY (categoria_id) REFERENCES categoria(codigo),
  FOREIGN KEY (usuario_uid) REFERENCES usuario(uid)
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
