<?php
// Servicio web de movimientos (PB25)
//   GET    -> lista todos o uno (?codigo=)
//   POST   -> inserta (datos en $_POST)
//   PUT    -> actualiza (datos en la URL, $_GET)
//   DELETE -> elimina (?codigo=)
include "config.php";
include "utils.php";

$dbConn = connect($db);
migrar($dbConn);  // Tabla usuario y columna usuario_uid
header('Content-Type: application/json; charset=utf-8');

// Columnas que se pueden guardar (evita nombres de columna inventados)
// usuario_uid = dueño del movimiento (tabla usuario)
$columnas = ['codigo', 'tipo', 'monto', 'categoria_id', 'fecha', 'nota', 'usuario_uid'];

function soloColumnas($input, $columnas)
{
    $datos = [];
    foreach ($columnas as $columna) {
        if (isset($input[$columna])) {
            $datos[$columna] = $input[$columna];
        }
    }
    return $datos;
}

if ($_SERVER['REQUEST_METHOD'] == 'GET') {
    if (isset($_GET['codigo'])) {
        $sql = $dbConn->prepare("SELECT * FROM movimiento WHERE codigo=:codigo");
        $sql->bindValue(':codigo', $_GET['codigo']);
        $sql->execute();
        header("HTTP/1.1 200 OK");
        echo json_encode($sql->fetch(PDO::FETCH_ASSOC));
        exit();
    } else {
        $sql = $dbConn->prepare("SELECT * FROM movimiento ORDER BY fecha DESC, codigo DESC");
        $sql->execute();
        $sql->setFetchMode(PDO::FETCH_ASSOC);
        header("HTTP/1.1 200 OK");
        echo json_encode($sql->fetchAll());
        exit();
    }
}

if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    $input = soloColumnas($_POST, $columnas);
    $sql = "INSERT INTO movimiento (codigo, tipo, monto, categoria_id, fecha, nota, usuario_uid)
            VALUES (:codigo, :tipo, :monto, :categoria_id, :fecha, :nota, :usuario_uid)";
    $statement = $dbConn->prepare($sql);
    $statement->bindValue(':codigo', $input['codigo']);
    $statement->bindValue(':tipo', $input['tipo']);
    $statement->bindValue(':monto', $input['monto']);
    $statement->bindValue(':categoria_id', $input['categoria_id']);
    $statement->bindValue(':fecha', $input['fecha']);
    $statement->bindValue(':nota', isset($input['nota']) && $input['nota'] !== '' ? $input['nota'] : null);
    $statement->bindValue(':usuario_uid', isset($input['usuario_uid']) && $input['usuario_uid'] !== '' ? $input['usuario_uid'] : null);
    $statement->execute();
    header("HTTP/1.1 200 OK");
    echo json_encode($input);
    exit();
}

if ($_SERVER['REQUEST_METHOD'] == 'PUT') {
    $input = soloColumnas($_GET, $columnas);
    $codigo = $input['codigo'];
    unset($input['codigo']);
    // Vacío = sin dueño (movimiento anterior al login) o sin nota
    foreach (['usuario_uid', 'nota'] as $opcional) {
        if (isset($input[$opcional]) && $input[$opcional] === '') {
            $input[$opcional] = null;
        }
    }
    $fields = getParams($input);
    $sql = "UPDATE movimiento SET $fields WHERE codigo=:codigo";
    $statement = $dbConn->prepare($sql);
    bindAllValues($statement, $input);
    $statement->bindValue(':codigo', $codigo);
    $statement->execute();
    header("HTTP/1.1 200 OK");
    exit();
}

if ($_SERVER['REQUEST_METHOD'] == 'DELETE') {
    $codigo = $_GET['codigo'];
    $statement = $dbConn->prepare("DELETE FROM movimiento WHERE codigo=:codigo");
    $statement->bindValue(':codigo', $codigo);
    $statement->execute();
    header("HTTP/1.1 200 OK");
    exit();
}

header("HTTP/1.1 400 Bad Request");
?>
