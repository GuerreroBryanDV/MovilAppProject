# GastoFácil

Aplicación móvil para el control de gastos personales, desarrollada con **.NET 10 MAUI y XAML**
para Android e iOS (también se ejecuta en Windows). Permite registrar ingresos y gastos, ver el
saldo del mes, controlar la regla de presupuesto 50/40/10 con un semáforo por grupo, revisar
estadísticas con gráficos y recibir recomendaciones con inteligencia artificial.

Todos los datos se guardan **solo en el dispositivo** (SQLite, sin servidor) y el acceso se puede
proteger con huella, Face ID o PIN.

Proyecto final de **Aplicaciones Móviles** · Ingeniería en Informática · Universidad Israel · 2026.

| Integrante | Rol |
|---|---|
| Bryan Guerrero | Desarrollo · Scrum Master (Sprints 1 y 4) |
| Fernanda Ushcasina | Desarrollo · Scrum Master (Sprint 2) |
| Mafer Tibanta | Desarrollo · Scrum Master (Sprint 3) |

Docente (Product Owner): Ph.D. Renato Mauricio Toasa Guachi.

---

## Capturas

| Inicio | Nuevo movimiento | Regla 50/40/10 |
|---|---|---|
| ![Inicio](capturas/01-inicio.png) | ![Nuevo movimiento](capturas/02-movimiento.png) | ![Regla](capturas/03-regla.png) |

| Estadísticas | Recomendaciones IA | Ajustes |
|---|---|---|
| ![Estadísticas](capturas/04-estadisticas.png) | ![Recomendaciones IA](capturas/05-recomendaciones.png) | ![Ajustes](capturas/06-ajustes.png) |

| Desbloqueo | Perfiles de porcentajes |
|---|---|
| ![Desbloqueo](capturas/07-desbloqueo.png) | ![Porcentajes](capturas/08-porcentajes.png) |

---

## Funciones de la versión 1.0

- **Registrar gastos e ingresos** con monto, categoría, fecha y nota opcional, en máximo 3 pasos
  desde la pantalla principal.
- **Saldo del mes**, ingresos, egresos y últimos movimientos en la pantalla de inicio.
- **Editar y eliminar** movimientos, con confirmación antes de borrar.
- **Regla 50/40/10**: porcentaje real frente a la meta, dinero restante o excedido y semáforo por
  grupo. Necesidades y Lujos son un tope; Ahorro e inversión es un mínimo a alcanzar.
- **Perfiles de porcentajes**: 50/40/10 (por defecto), Clásica 50/20/30 o Personalizado, con
  validación de que sumen 100 %.
- **Estadísticas**: gasto total del mes y gráfico por categoría.
- **Filtro por mes** compartido entre Inicio, Regla y Estadísticas.
- **Recordatorios locales**: notificación diaria, semanal o mensual, aunque la app esté cerrada.
- **Bloqueo** con huella, Face ID o PIN. El PIN se guarda cifrado; desactivar el bloqueo o
  cambiar el PIN pide verificar la identidad.
- **Recomendaciones con IA** y **preguntas a la IA** a partir de los totales del mes.

Fuera del alcance de la v1.0: cuentas en la nube, varias monedas, categorías personalizadas,
exportar a Excel o PDF y lectura de recibos.

### Categorías predefinidas

| Grupo | Categorías |
|---|---|
| Necesidades | Comida, Vivienda, Salud, Transporte, Servicios |
| Ahorro e inversión | Fondo de emergencia, Educación, Invertir |
| Lujos | Restaurantes, Entretenimiento, Vacaciones, Electrónicos |

### Semáforo de la regla

| Grupo | Verde | Amarillo | Rojo |
|---|---|---|---|
| Necesidades y Lujos (tope) | hasta 100 % de la meta | más de 100 % y hasta 130 % | más de 130 % |
| Ahorro e inversión (mínimo) | 100 % o más de la meta | de 75 % a 99 % | menos de 75 % |

---

## Tecnologías

| Uso | Tecnología |
|---|---|
| Framework | .NET 10 MAUI con XAML (una sola base de código) |
| Base de datos local | SQLite con `sqlite-net-pcl` |
| Configuración | Preferences (ajustes, perfil, recordatorio) y SecureStorage (PIN cifrado) |
| Gráficos | LiveCharts2 (`LiveChartsCore.SkiaSharpView.Maui`) |
| Huella / Face ID | `Plugin.Maui.Biometric` |
| Notificaciones locales | `Plugin.LocalNotification` |
| Inteligencia artificial | API de Google Gemini (modelo Flash-Lite, plan gratuito) |
| Gestión del proyecto | Scrum en Jira · código en GitHub con pull requests |

---

## Requisitos para ejecutar

- Windows 10 u 11.
- **Visual Studio 2026** con la carga de trabajo **Desarrollo de interfaz de usuario de aplicaciones
  multiplataforma de .NET (.NET MAUI)**.
- **SDK de .NET 10**.
- Para Android: un emulador (se probó con Pixel 7, API 36) o un teléfono con **Android 6.0 (API 23)
  o superior**.
- Para iOS: una Mac con Xcode emparejada con Visual Studio (no se probó en este proyecto).
- Para las recomendaciones con IA: conexión a internet y una clave gratuita de Google AI Studio
  (ver más abajo). El resto de la app funciona sin internet.

## Cómo ejecutar

1. Clonar el repositorio:

   ```bash
   git clone https://github.com/GuerreroBryanDV/MovilAppProject.git
   ```

2. Abrir `MovilAppProject/MovilAppProject.slnx` en Visual Studio.
3. Esperar a que se restauren los paquetes NuGet. Si falla, clic derecho en la solución →
   **Restaurar paquetes NuGet**.
4. (Opcional) Configurar la IA con el archivo `.env` (siguiente sección).
5. En la barra superior, junto al botón ▶, elegir dónde ejecutar:
   - **Windows Machine**, o
   - **Android Emulators → Pixel 7 - API 36** (abrir antes el emulador desde
     **Herramientas → Android → Administrador de dispositivos**).
6. **Compilar → Recompilar solución** y presionar **▶ (F5)**.

Si al compilar aparece que un archivo "está siendo utilizado por otro proceso", cerrar Visual
Studio, borrar las carpetas `bin` y `obj` de `MovilAppProject/MovilAppProject/` y volver a abrir.

## Configurar la IA (archivo .env)

La clave de la API **no está en el código ni en el repositorio**. Se lee de un archivo `.env` que
cada persona crea en su equipo y que está en el `.gitignore`.

1. Crear una clave gratis en [Google AI Studio](https://aistudio.google.com) → **Get API key →
   Create API key**. No hace falta agregar tarjeta.
2. En `MovilAppProject/MovilAppProject/Resources/Raw/` copiar `.env.example` con el nombre `.env`.
3. Pegar la clave en la línea `GEMINI_API_KEY=` (sin espacios ni comillas).
4. Comprobar que `.env` **no aparece** en los cambios de Git.
5. **Recompilar la solución** (el archivo se copia dentro de la app al compilar).

Sin `.env` la app funciona normalmente; solo la pantalla de IA muestra
"Falta configurar la clave de la IA (archivo .env).".

Límite conocido: en una app móvil el archivo `.env` va dentro del instalador. Por eso se usa el plan
gratuito, sin tarjeta, y la clave se restringe a la API de Gemini. Si una clave se filtra, se borra
en Google AI Studio y se crea otra.

## Privacidad

- Movimientos, categorías, ajustes y PIN quedan solo en el dispositivo.
- A la IA se envía únicamente un resumen del mes: el perfil de porcentajes y, por grupo, la meta, el
  porcentaje real, el total gastado y el estado del semáforo. **Nunca** notas, movimientos
  individuales ni datos personales.

---

## Estructura del proyecto

```
MovilAppProject/
├── README.md
└── MovilAppProject/                  Proyecto .NET MAUI (solución y proyecto en la misma carpeta)
    ├── Models/                       Movimiento, Categoria, GrupoPresupuesto, PerfilPresupuesto
    ├── Repositories/                 MovimientoRepository (SQLite)
    ├── Services/                     ReglaPresupuestoService, RecordatorioService, IAService
    ├── Utils/                        FileAccessHelper, PinStorage, Biometria, FiltroMes
    ├── Views/                        vInicio, vMovimiento, vRegla, vEstadisticas, vRecomendaciones,
    │                                 vPreguntaIA, vAjustes, vPorcentajes, vDesbloqueo, BarraInferior
    ├── Resources/                    Estilos, fuentes, íconos y Raw/.env.example
    ├── Platforms/                    Código y permisos por plataforma (Android, iOS, Windows...)
    ├── App.xaml / AppShell.xaml      Estilos globales y navegación por pestañas
    └── MauiProgram.cs                Registro de servicios y paquetes
```

Flujo: Vista (XAML y code-behind) → Servicio / Repositorio → SQLite, Preferences o SecureStorage.
La lógica de la regla, los recordatorios y la IA está en servicios sin dependencias de la interfaz.

## Metodología

El proyecto se desarrolló con **Scrum** en 4 sprints de una semana, con el backlog y el tablero en
Jira (proyecto MAP). Cada historia se trabajó en su propia rama y se integró a `main` mediante pull
request. Los commits siguen Conventional Commits en español (`feat:`, `fix:`, `perf:`, `refactor:`).

| Sprint | Objetivo |
|---|---|
| 1 | Proyecto base, registro de gastos e ingresos y saldo actual |
| 2 | Movimientos recientes, editar, eliminar, filtro por mes y panel de la regla con semáforo |
| 3 | Perfiles de porcentajes, estadísticas, ajustes, bloqueo, rendimiento y recordatorios |
| 4 | Servicio de IA, recomendaciones, preguntas a la IA, usabilidad, pulido visual y entrega v1.0 |

## Enlaces

- Repositorio: https://github.com/GuerreroBryanDV/MovilAppProject
- Tablero Scrum (Jira, proyecto MAP): https://uisrael-team-g1.atlassian.net/jira/software/projects/MAP/boards/72
