<?php
// PB25 · Servicio web de categorías (solo lectura: son fijas en v1.0)
// Devuelve cada categoría con el nombre de su grupo de la regla.
include "config.php";
include "utils.php";

$dbConn = connect($db);
header('Content-Type: application/json; charset=utf-8');

if ($_SERVER['REQUEST_METHOD'] == 'GET') {
    $sql = $dbConn->prepare("SELECT c.codigo, c.nombre, c.grupo_id, g.nombre AS grupo
                             FROM categoria c
                             LEFT JOIN grupo_presupuesto g ON g.codigo = c.grupo_id
                             ORDER BY c.codigo");
    $sql->execute();
    $sql->setFetchMode(PDO::FETCH_ASSOC);
    header("HTTP/1.1 200 OK");
    echo json_encode($sql->fetchAll());
    exit();
}

header("HTTP/1.1 400 Bad Request");
?>
