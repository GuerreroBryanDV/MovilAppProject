<?php
// Servicio web de usuarios del inicio de sesión (PB27)
//   GET  -> lista los usuarios (activos e inactivos)
//   POST -> inserta o actualiza un usuario por su uid (alta, o inactivación = borrado lógico)
// La contraseña y el PIN no se envían: se quedan cifrados en el teléfono.
include "config.php";
include "utils.php";
$dbConn = connect($db);
migrar($dbConn);
header('Content-Type: application/json; charset=utf-8');

if ($_SERVER['REQUEST_METHOD'] == 'GET') {
    $sql = $dbConn->prepare("SELECT uid, nombre, activo, fecha_creacion, fecha_baja
                             FROM usuario ORDER BY fecha_creacion");
    $sql->execute();
    $sql->setFetchMode(PDO::FETCH_ASSOC);
    header("HTTP/1.1 200 OK");
    echo json_encode($sql->fetchAll());
    exit();
}

if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    if (empty($_POST['uid']) || empty($_POST['nombre']) || empty($_POST['fecha_creacion'])) {
        header("HTTP/1.1 400 Bad Request");
        echo json_encode(['error' => 'Faltan datos del usuario']);
        exit();
    }
    $activo = (isset($_POST['activo']) && $_POST['activo'] == '1') ? 1 : 0;
    $baja = (isset($_POST['fecha_baja']) && $_POST['fecha_baja'] !== '') ? $_POST['fecha_baja'] : null;
    $sql = "INSERT INTO usuario (uid, nombre, activo, fecha_creacion, fecha_baja)
            VALUES (:uid, :nombre, :activo, :fecha_creacion, :fecha_baja)
            ON DUPLICATE KEY UPDATE nombre = VALUES(nombre), activo = VALUES(activo),
                                    fecha_baja = VALUES(fecha_baja)";
    $statement = $dbConn->prepare($sql);
    $statement->bindValue(':uid', $_POST['uid']);
    $statement->bindValue(':nombre', $_POST['nombre']);
    $statement->bindValue(':activo', $activo, PDO::PARAM_INT);
    $statement->bindValue(':fecha_creacion', $_POST['fecha_creacion']);
    $statement->bindValue(':fecha_baja', $baja);
    $statement->execute();
    header("HTTP/1.1 200 OK");
    echo json_encode(['uid' => $_POST['uid']]);
    exit();
}

header("HTTP/1.1 400 Bad Request");
?>