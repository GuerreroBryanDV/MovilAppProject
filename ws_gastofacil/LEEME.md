# PB25 · Servicio web de GastoFácil (XAMPP + PHP + MySQL)

Respaldo automático de la base SQLite de la app en MySQL.
Mismo patrón que el ejemplo `ws_estudiante` de la Semana 6.

## Archivos

| Archivo | Para qué sirve |
|---|---|
| `config.php` | Datos de conexión a MySQL (host, usuario, contraseña, base) |
| `utils.php` | Funciones del profesor: `connect`, `getParams`, `bindAllValues` |
| `movimiento_ws.php` | GET (listar), POST (insertar), PUT (actualizar), DELETE (eliminar) |
| `categoria_ws.php` | GET de categorías con el nombre de su grupo |
| `../MovilAppProject/Database/gastofacil_ws.sql` | Crea la base `gastofacil_ws` con sus 3 tablas |

## Cómo configurarlo en tu computadora

1. Copia **esta carpeta `ws_gastofacil` completa** a `C:\xampp\htdocs\`
   (debe quedar `C:\xampp\htdocs\ws_gastofacil\movimiento_ws.php`).
2. Abre el panel de XAMPP y da **Start** a **Apache** y **MySQL**.
3. Si tu MySQL tiene contraseña, ábrela en `C:\xampp\htdocs\ws_gastofacil\config.php`
   y escríbela en `'password'`. **No subas tu contraseña a GitHub**:
   cambia solo la copia de `htdocs`, no la del repositorio.
4. Entra a `http://localhost/phpmyadmin` → pestaña **Importar** →
   elige `MovilAppProject\Database\gastofacil_ws.sql` → **Continuar**.
5. Prueba en el navegador: `http://localhost/ws_gastofacil/movimiento_ws.php`
   (debe mostrar `[]` o la lista de movimientos en JSON).
6. Ejecuta la app (Windows Machine o emulador Android). Al guardar, editar o
   eliminar un movimiento, se envía al servicio web. Al abrir Inicio se
   sincroniza lo que haya quedado pendiente.

## Notas

- En Windows la app usa `http://localhost/...` y en el emulador Android `http://10.0.2.2/...`.
- **Celular Android real:** en la PC ejecuta `ipconfig` y copia la "Dirección IPv4" del Wi-Fi.
  Ponla en `IpComputadora` dentro de `Services/ServicioWeb.cs`. El celular y la PC deben
  estar en el mismo Wi-Fi, y el Firewall de Windows debe permitir Apache (redes privadas).
  Prueba primero en el navegador del celular: `http://TU_IP/ws_gastofacil/movimiento_ws.php`.
- Si XAMPP está apagado, la app funciona igual con SQLite y se pone al día después.
- El PIN y los ajustes no se suben: son datos del dispositivo (SecureStorage / Preferences).
