using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace YADB
{
    /// <summary> 
    /// 2017-8-25
    /// Read and write to files for information you either don't want public
    /// or will want to change without having to compile another bot.
    /// </summary>
    public static class FileOperations
    {
        /// <summary>
        /// 2017-8-25
        /// Location of all data files
        /// </summary>
        public static string Folder { get; private set; } = "config/";
        
        /// <summary>
        /// 2017-8-25
        /// Returns the fully qualified path to the file.
        /// </summary>
        /// <param name="FileName"/>the local file name, NOT the fully qualified file name</param>
        /// <returns></returns>
        public static string PathToFile(string FileName)
        {
            return Path.Combine(AppContext.BaseDirectory, Folder + FileName);
        }

        /// <summary>
        /// 2017-8-25
        /// Indicates if the file exists.
        /// <param name="FileName"/>the local file name, NOT the fully qualified file name</param>
        /// </summary>
        public static bool Exists(string FileName)
        {
            string filepath = PathToFile(FileName);            
            return File.Exists(filepath);
        }

        /// <summary>
        /// 2017-8-25
        /// Returns a filename based on the class of the object,
        /// e.g., for a Configuration object, the result is "configuration.json"
        /// </summary>
        public static string GetFilename<T>(this T objectToSave)
        {
            return typeof(T).ToString().ToLower() + ".json";
        }

        /// <summary>
        /// 2017-8-25
        /// Save the object in json format.
        /// <return>The file name the object is saved as.</return>
        /// </summary>
        public static string SaveAsJson<T>(T objectToSave)
        {
            string filename = objectToSave.GetFilename();
            string file = PathToFile(filename);

            //  Create config directory if doesn't exist
            string path = Path.GetDirectoryName(file);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            File.WriteAllText(file, ToJson(objectToSave));
            return filename;
        }

        /// <summary>
        /// 2017-8-25
        /// Deserialize a json string back into an object.
        /// <param name="FileName"/>the local file name, NOT the fully qualified file name</param>
        /// </summary>
        public static T Load<T>() where T: new()
        {
            string file = PathToFile(new T().GetFilename());
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(file));
        }

        /// <summary>
        /// 2017-8-25
        /// Convert any object to json string.
        /// </summary>
        public static string ToJson<T>(this T t) => JsonConvert.SerializeObject(t, Formatting.Indented);
    }
}
