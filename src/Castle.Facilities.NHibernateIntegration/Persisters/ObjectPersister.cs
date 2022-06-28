namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Castle.MicroKernel.Registration;

    using Newtonsoft.Json;

    public class ObjectPersister<T>
    {
        readonly JsonSerializer _serializer;

        public ObjectPersister()
        {
            _serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
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
