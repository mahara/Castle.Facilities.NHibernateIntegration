namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    using System.IO;

    using Newtonsoft.Json;

    public class ObjectPersister<T>
    {
        private readonly JsonSerializer _serializer;

        public ObjectPersister()
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
}
