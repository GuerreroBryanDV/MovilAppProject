<?php
  //Abrir conexion a la base de datos
  function connect($db)
  {
      try {
          $conn = new PDO("mysql:host={$db['host']};dbname={$db['db']};charset=utf8mb4", $db['username'], $db['password']);
          // set the PDO error mode to exception
          $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
          return $conn;
      } catch (PDOException $exception) {
          exit($exception->getMessage());
      }
  }
//Obtener parámetros para updates
 function getParams($input)
 {
    $filterParams = [];
    foreach($input as $param => $value)
    {
            $filterParams[] = "$param=:$param";
    }
    return implode(", ", $filterParams);
 }
  //Asociar todos los parametros a un sql
 function bindAllValues($statement, $params)
  {
  foreach($params as $param => $value)
    {
  $statement->bindValue(':'.$param, $value);
 }
  return $statement;
   }

  // Crea la tabla usuario y la columna movimiento.usuario_uid si todavía no existen.
  // Así funciona tanto con una base nueva como con una importada antes del login,
  // sin tener que volver a importar el .sql.
  function migrar($conn)
  {
      $conn->exec("CREATE TABLE IF NOT EXISTS usuario (
          uid            VARCHAR(36) PRIMARY KEY,
          nombre         VARCHAR(20) NOT NULL,
          activo         TINYINT(1)  NOT NULL DEFAULT 1,
          fecha_creacion DATETIME    NOT NULL,
          fecha_baja     DATETIME    NULL
      ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
      $sql = $conn->prepare("SELECT COUNT(*) FROM information_schema.COLUMNS
                             WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'movimiento'
                               AND COLUMN_NAME = 'usuario_uid'");
      $sql->execute();
      if ((int)$sql->fetchColumn() == 0) {
          $conn->exec("ALTER TABLE movimiento ADD COLUMN usuario_uid VARCHAR(36) NULL");
          $conn->exec("ALTER TABLE movimiento ADD CONSTRAINT fk_movimiento_usuario
                       FOREIGN KEY (usuario_uid) REFERENCES usuario(uid)");
      }
  }
 ?>