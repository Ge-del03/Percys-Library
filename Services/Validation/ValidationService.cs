// FileName: Services/Validation/ValidationService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ComicReader.Services.Validation
{
    /// <summary>
    /// Sistema centralizado de validación con mensajes de error claros y útiles
    /// </summary>
    public class ValidationService
    {
        private static ValidationService _instance;
        public static ValidationService Instance => _instance ??= new ValidationService();

        private readonly HashSet<string> _supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cbz", ".cbr", ".cbt", ".cb7",  // Comic formats
            ".zip", ".rar", ".tar", ".7z",    // Archive formats
            ".pdf",                            // PDF
            ".djvu", ".djv",                   // DjVu
            ".epub",                           // ePub
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" // Image formats
        };

        private ValidationService() { }

        #region File Validation

        /// <summary>
        /// Valida que un archivo existe y es accesible
        /// </summary>
        public ValidationResult ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return ValidationResult.Fail("La ruta del archivo está vacía");

            if (!File.Exists(filePath))
                return ValidationResult.Fail($"El archivo no existe:\n{filePath}");

            if (!HasReadPermission(filePath))
                return ValidationResult.Fail($"No tienes permisos para leer este archivo:\n{filePath}");

            var extension = Path.GetExtension(filePath);
            if (!_supportedExtensions.Contains(extension))
                return ValidationResult.Fail($"Formato no soportado: {extension}\n\nFormatos válidos: {string.Join(", ", _supportedExtensions.Take(10))}");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
                return ValidationResult.Fail("El archivo está vacío");

            if (fileInfo.Length > 2L * 1024 * 1024 * 1024) // 2GB
                return ValidationResult.Fail("El archivo es demasiado grande (máximo 2GB)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Valida que un directorio existe y es accesible
        /// </summary>
        public ValidationResult ValidateDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return ValidationResult.Fail("La ruta del directorio está vacía");

            if (!Directory.Exists(directoryPath))
                return ValidationResult.Fail($"El directorio no existe:\n{directoryPath}");

            if (!HasReadPermission(directoryPath))
                return ValidationResult.Fail($"No tienes permisos para acceder a este directorio:\n{directoryPath}");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Valida que se puede escribir en una ubicación
        /// </summary>
        public ValidationResult ValidateWriteAccess(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return ValidationResult.Fail("La ruta está vacía");

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    return ValidationResult.Fail($"El directorio no existe:\n{directory}");
                }

                // Intentar crear un archivo temporal
                var testFile = Path.Combine(directory, $".test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return ValidationResult.Success();
            }
            catch (UnauthorizedAccessException)
            {
                return ValidationResult.Fail("No tienes permisos de escritura en esta ubicación");
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Error al verificar permisos de escritura: {ex.Message}");
            }
        }

        #endregion

        #region Comic Validation

        /// <summary>
        /// Valida que un archivo de cómic es válido y puede abrirse
        /// </summary>
        public ValidationResult ValidateComicFile(string filePath)
        {
            var fileValidation = ValidateFile(filePath);
            if (!fileValidation.IsValid)
                return fileValidation;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Validaciones específicas por formato
            switch (extension)
            {
                case ".cbz":
                case ".zip":
                    return ValidateZipArchive(filePath);

                case ".cbr":
                case ".rar":
                    return ValidateRarArchive(filePath);

                case ".pdf":
                    return ValidatePdfFile(filePath);

                case ".djvu":
                case ".djv":
                    return ValidateDjVuFile(filePath);

                default:
                    return ValidationResult.Success();
            }
        }

        private ValidationResult ValidateZipArchive(string filePath)
        {
            try
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
                
                if (archive.Entries.Count == 0)
                    return ValidationResult.Fail("El archivo ZIP está vacío");

                var imageEntries = archive.Entries.Where(e => IsImageFile(e.Name)).ToList();
                
                if (imageEntries.Count == 0)
                    return ValidationResult.Fail("El archivo no contiene imágenes");

                return ValidationResult.Success();
            }
            catch (System.IO.InvalidDataException)
            {
                return ValidationResult.Fail("El archivo ZIP está corrupto o dañado");
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Error al abrir el archivo: {ex.Message}");
            }
        }

        private ValidationResult ValidateRarArchive(string filePath)
        {
            // Verificar que SharpCompress esté disponible
            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = SharpCompress.Archives.Rar.RarArchive.Open(stream);
                
                if (!reader.Entries.Any())
                    return ValidationResult.Fail("El archivo RAR está vacío");

                var imageEntries = reader.Entries.Where(e => !e.IsDirectory && IsImageFile(e.Key)).ToList();
                
                if (imageEntries.Count == 0)
                    return ValidationResult.Fail("El archivo no contiene imágenes");

                return ValidationResult.Success();
            }
            catch (SharpCompress.Common.InvalidFormatException)
            {
                return ValidationResult.Fail("El archivo RAR está corrupto o dañado");
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Error al abrir el archivo RAR: {ex.Message}");
            }
        }

        private ValidationResult ValidatePdfFile(string filePath)
        {
            try
            {
                // Verificar firma PDF
                using var stream = File.OpenRead(filePath);
                var header = new byte[5];
                stream.Read(header, 0, 5);
                
                var pdfSignature = System.Text.Encoding.ASCII.GetString(header);
                if (!pdfSignature.StartsWith("%PDF"))
                    return ValidationResult.Fail("El archivo no es un PDF válido");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Error al validar PDF: {ex.Message}");
            }
        }

        private ValidationResult ValidateDjVuFile(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var header = new byte[8];
                stream.Read(header, 0, 8);
                
                var signature = System.Text.Encoding.ASCII.GetString(header);
                if (!signature.StartsWith("AT&TFORM"))
                    return ValidationResult.Fail("El archivo no es un DjVu válido");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Error al validar DjVu: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private bool HasReadPermission(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (File.OpenRead(path)) { }
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.GetFiles(path);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".png" || extension == ".bmp" || 
                   extension == ".gif" || extension == ".webp";
        }

        /// <summary>
        /// Agrega una extensión soportada personalizada
        /// </summary>
        public void AddSupportedExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                _supportedExtensions.Add(extension.StartsWith(".") ? extension : $".{extension}");
            }
        }

        /// <summary>
        /// Obtiene todas las extensiones soportadas
        /// </summary>
        public IEnumerable<string> GetSupportedExtensions() => _supportedExtensions.ToList();

        #endregion
    }

    #region Validation Result

    /// <summary>
    /// Resultado de una validación
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }
        public ValidationSeverity Severity { get; }

        private ValidationResult(bool isValid, string errorMessage, ValidationSeverity severity)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Severity = severity;
        }

        public static ValidationResult Success() => new ValidationResult(true, null, ValidationSeverity.None);
        
        public static ValidationResult Fail(string errorMessage, ValidationSeverity severity = ValidationSeverity.Error)
            => new ValidationResult(false, errorMessage, severity);

        public static ValidationResult Warning(string message)
            => new ValidationResult(true, message, ValidationSeverity.Warning);
    }

    public enum ValidationSeverity
    {
        None,
        Info,
        Warning,
        Error
    }

    #endregion
}
