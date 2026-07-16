using System;
using System.IO;
using System.Text.Json;

namespace EmpresaChat
{
    /// <summary>
    /// Gestor de configuración del servidor para el cliente WPF
    /// </summary>
    public class ServerConfig
    {
        private const string ConfigFileName = "server.config";
        private readonly string _configPath;

        public string? ServerUrl { get; set; }
        public string Mode { get; set; } = "CLIENT";
        public bool AutoConnect { get; set; } = true;

        public ServerConfig()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatAgenda",
                ConfigFileName
            );
        }

        /// <summary>
        /// Carga la configuración desde archivo
        /// </summary>
        public bool Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ServerConfigData>(json);

                    if (config != null)
                    {
                        ServerUrl = config.ServerUrl;
                        Mode = config.Mode ?? "CLIENT";
                        AutoConnect = config.AutoConnect ?? true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Guarda la configuración en archivo
        /// </summary>
        public bool Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new ServerConfigData
                {
                    ServerUrl = ServerUrl,
                    Mode = Mode,
                    AutoConnect = AutoConnect,
                    LastUpdated = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Limpia la configuración
        /// </summary>
        public bool Clear()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    File.Delete(_configPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la ruta del archivo de configuración
        /// </summary>
        public string GetConfigPath()
        {
            return _configPath;
        }

        /// <summary>
        /// Comprueba si el archivo de configuración existe
        /// </summary>
        public bool ConfigExists()
        {
            return File.Exists(_configPath);
        }

        private class ServerConfigData
        {
            public string? ServerUrl { get; set; }
            public string? Mode { get; set; }
            public bool? AutoConnect { get; set; }
            public DateTime? LastUpdated { get; set; }
        }
    }
}
