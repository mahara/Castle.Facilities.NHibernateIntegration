namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    using System.IO;

    using Newtonsoft.Json;

    public interface IObjectPersister<T>
    {
        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate);

        public void Write(string filePath, T @object, FileMode mode = FileMode.OpenOrCreate);
    }

    public class BinaryObjectPersister<T> : IObjectPersister<T>
    {
        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            throw new System.NotImplementedException();
        }

        public void Write(string filePath, T @object, FileMode mode = FileMode.OpenOrCreate)
        {
            throw new System.NotImplementedException();
        }
    }

    public class NewtonsoftJsonObjectPersister<T> : IObjectPersister<T>
    {
        private readonly JsonSerializer _serializer;

        public NewtonsoftJsonObjectPersister()
        {
            _serializer = new JsonSerializer
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            };
        }

        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return _serializer.Deserialize<T>(jsonReader);
        }

        public void Write(string filePath, T @object, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            _serializer.Serialize(jsonWriter, @object);
        }
    }

    public class JsonObjectPersister<T> : IObjectPersister<T>
    {
        private readonly System.Text.Json.JsonSerializerOptions _options;

        public JsonObjectPersister()
        {
            _options = new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = false,
            };
        }

        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            return System.Text.Json.JsonSerializer.Deserialize<T>(stream, _options);
        }

        public void Write(string filePath, T @object, FileMode mode = FileMode.OpenOrCreate)
        {
            System.Text.Json.JsonSerializer.Serialize(@object, _options);
        }
    }
}
