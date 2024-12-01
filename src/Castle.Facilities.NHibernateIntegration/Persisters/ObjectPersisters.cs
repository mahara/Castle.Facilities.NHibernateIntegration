#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace Castle.Facilities.NHibernateIntegration.Persisters;

using System.IO;

using Newtonsoft.Json;

using NHibernate.Util;

using JsonSerializer = System.Text.Json.JsonSerializer;

public class ObjectPersisterFactory
{
    public static IObjectPersister<T> Create<T>()
    {
        return new BinaryFormatterObjectPersister<T>();
        //return new NewtonsoftJsonObjectPersister<T>();
        //return new JsonObjectPersister<T>();
    }
}

public interface IObjectPersister<T>
{
    public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate);

    public void Write(T @object, string filePath, FileMode mode = FileMode.Create);
}

/// <summary>
///     <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/> object persister.
/// </summary>
/// <typeparam name="T"></typeparam>
public class BinaryFormatterObjectPersister<T> : IObjectPersister<T>
{
    public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
    {
        using var fileStream = new FileStream(filePath, mode);
        using var memoryStream = new MemoryStream();

        fileStream.CopyTo(memoryStream);

        return (T) SerializationHelper.Deserialize(memoryStream.ToArray());
    }

    public void Write(T @object, string filePath, FileMode mode = FileMode.Create)
    {
        using var fileStream = new FileStream(filePath, mode);

        var buffer = SerializationHelper.Serialize(@object);
#if NET
        fileStream.Write(buffer.AsSpan());
#else
        fileStream.Write(buffer, 0, buffer.Length);
#endif
    }
}

/// <summary>
///     Newtonsoft.Json object persister.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
///     Serialization/deserialization issues:
///     -   Newtonsoft.Json.JsonSerializationException : Error getting value from 'ColumnInsertability' on 'NHibernate.Mapping.OneToMany'.
///           ----> System.InvalidOperationException : Operation is not valid due to the current state of the object.
/// </remarks>
public class NewtonsoftJsonObjectPersister<T> : IObjectPersister<T>
{
    private readonly Newtonsoft.Json.JsonSerializer _serializer = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
    {
        using var fileStream = new FileStream(filePath, mode);
        using var streamReader = new StreamReader(fileStream);
        using var jsonReader = new JsonTextReader(streamReader);

        return (T) _serializer.Deserialize(jsonReader)!;
    }

    public void Write(T @object, string filePath, FileMode mode = FileMode.Create)
    {
        using var fileStream = new FileStream(filePath, mode);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter);

        _serializer.Serialize(streamWriter, @object);
    }
}

/// <summary>
///     System.Text.Json object persister.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
///     Serialization/deserialization issues:
///     -   System.NotSupportedException : Serialization and deserialization of 'System.Type' instances is not supported. Path: $.ClassMappings.MappedClass.
///          ----> System.NotSupportedException : Serialization and deserialization of 'System.Type' instances is not supported.
/// </remarks>
public class JsonObjectPersister<T> : IObjectPersister<T>
{
    public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
    {
        using var fileStream = new FileStream(filePath, mode);

        return JsonSerializer.Deserialize<T>(fileStream)!;
    }

    public void Write(T @object, string filePath, FileMode mode = FileMode.Create)
    {
        using var fileStream = new FileStream(filePath, mode);

        JsonSerializer.Serialize(fileStream, @object);
    }
}
