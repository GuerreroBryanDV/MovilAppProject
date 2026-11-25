using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace MovilAppProject.Services
{
    // Notificaciones locales para los recordatorios (PB20); llegan aunque la app esté cerrada.
    public class RecordatorioService
    {
        // Mensajes exactos
        public const string MensajeTexto = "Escribe un recordatorio de 3 a 50 caracteres.";                                        // V20
        public const string MensajeFrecuencia = "Selecciona la frecuencia del recordatorio.";                                       // V22
        public const string MensajePermiso = "Activa las notificaciones en la configuración del teléfono para recibir recordatorios."; // V23

        // Frecuencias del Picker (V22)
        public const string Diaria = "Diaria";
        public const string Semanal = "Semanal";
        public const string Mensual = "Mensual";
        public static readonly List<string> Frecuencias = new() { Diaria, Semanal, Mensual };

        // V21: hora por defecto 20:00
        public static readonly TimeSpan HoraPorDefecto = new TimeSpan(20, 0, 0);

        // Claves de Preferences
        public const string ClaveTexto = "RecordatorioTexto";
        public const string ClaveHora = "RecordatorioHora";
        public const string ClaveFrecuencia = "RecordatorioFrecuencia";

        // Id fijo de la notificación: así un recordatorio nuevo reemplaza al anterior
        const int IdRecordatorio = 2020;

        public string StatusMessage { get; set; } = string.Empty;

        // V20: entre 3 y 50 caracteres, sin contar espacios al inicio y al final
        public static bool TextoValido(string? texto)
        {
            string limpio = (texto ?? "").Trim();
            return limpio.Length >= 3 && limpio.Length <= 50;
        }

        // V22: debe elegirse una opción de la lista
        public static bool FrecuenciaValida(string? frecuencia)
        {
            return frecuencia is not null && Frecuencias.Contains(frecuencia);
        }

        public bool HayRecordatorio => Preferences.ContainsKey(ClaveTexto);

        public string ObtenerTexto() => Preferences.Get(ClaveTexto, "");

        public TimeSpan ObtenerHora() => TimeSpan.FromTicks(Preferences.Get(ClaveHora, HoraPorDefecto.Ticks));

        public string ObtenerFrecuencia() => Preferences.Get(ClaveFrecuencia, "");

        // Valida (V20, V22), pide el permiso (V23) y programa la notificación.
        // Si algo falla no se guarda nada y StatusMessage tiene el mensaje para el usuario.
        public async Task<bool> GuardarRecordatorioAsync(string? texto, TimeSpan hora, string? frecuencia)
        {
            try
            {
                string limpio = (texto ?? "").Trim();

                // V20
                if (!TextoValido(limpio))
                {
                    StatusMessage = MensajeTexto;
                    return false;
                }

                // V22
                if (!FrecuenciaValida(frecuencia))
                {
                    StatusMessage = MensajeFrecuencia;
                    return false;
                }

                // V23: se pide el permiso al guardar; si el usuario lo niega no se guarda
                if (!await TienePermisoAsync())
                {
                    StatusMessage = MensajePermiso;
                    return false;
                }

                // Reemplaza el recordatorio anterior (si existía)
                LocalNotificationCenter.Current.Cancel(new[] { IdRecordatorio });

                NotificationRequest request = new()
                {
                    NotificationId = IdRecordatorio,
                    Title = "GastoFácil",
                    Description = limpio,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = ProximaFecha(hora),
                        RepeatType = TipoRepeticion(frecuencia!)
                    }
                };

                await LocalNotificationCenter.Current.Show(request);

                // Se guarda en Preferences solo si se programó
                Preferences.Set(ClaveTexto, limpio);
                Preferences.Set(ClaveHora, hora.Ticks);
                Preferences.Set(ClaveFrecuencia, frecuencia!);

                StatusMessage = "Recordatorio guardado";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        // Cancela la notificación y borra el recordatorio guardado
        public void CancelarRecordatorio()
        {
            try
            {
                LocalNotificationCenter.Current.Cancel(new[] { IdRecordatorio });
                Preferences.Remove(ClaveTexto);
                Preferences.Remove(ClaveHora);
                Preferences.Remove(ClaveFrecuencia);
                StatusMessage = "Recordatorio cancelado";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        // V23: true si las notificaciones están permitidas (si no, se pide el permiso)
        private static async Task<bool> TienePermisoAsync()
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled(new NotificationPermission()))
            {
                return true;
            }

            return await LocalNotificationCenter.Current.RequestNotificationPermission(new NotificationPermission());
        }

        // Próxima vez que llega la hora elegida: hoy si todavía no pasa, si no, mañana
        private static DateTime ProximaFecha(TimeSpan hora)
        {
            DateTime fecha = DateTime.Today.Add(hora);
            if (fecha <= DateTime.Now)
            {
                fecha = fecha.AddDays(1);
            }
            return fecha;
        }

        // Diaria / Semanal / Mensual -> repetición del plugin
        private static NotificationRepeat TipoRepeticion(string frecuencia)
        {
            if (frecuencia == Semanal)
            {
                return NotificationRepeat.Weekly;
            }

            if (frecuencia == Mensual)
            {
                return NotificationRepeat.Monthly;
            }

            return NotificationRepeat.Daily;
        }
    }
}
