namespace MovilAppProject.Utils
{
    // Devuelve la ruta donde se guarda la base de datos dentro del teléfono
    public class FileAccessHelper
    {
        public static string GetLocalFilePath(string fileName)
        {
            return System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);
        }
    }
}
