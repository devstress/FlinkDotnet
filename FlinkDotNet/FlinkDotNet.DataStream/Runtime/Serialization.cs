//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System.Text.Json;

namespace FlinkDotNet.DataStream.Runtime
{
    /// <summary>
    /// JSON-based implementation of <see cref="IDeserializationSchema{T}"/>.
    /// Deserializes byte arrays to objects using System.Text.Json.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    public sealed class JsonDeserializationSchema<T> : IDeserializationSchema<T>
    {
        private readonly JsonSerializerOptions? _options;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonDeserializationSchema{T}"/>.
        /// </summary>
        /// <param name="options">Optional JSON serializer options</param>
        public JsonDeserializationSchema(JsonSerializerOptions? options = null)
        {
            _options = options;
        }

        /// <inheritdoc/>
        public T Deserialize(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes, _options)
                ?? throw new JsonException($"Deserialization returned null for type {typeof(T).Name}");
        }

        /// <inheritdoc/>
        public bool IsEndOfStream(T element) => false;

        /// <inheritdoc/>
        public TypeInformation<T> GetProducedType() => TypeInformation.Of<T>();
    }

    /// <summary>
    /// JSON-based implementation of <see cref="ISerializationSchema{T}"/>.
    /// Serializes objects to byte arrays using System.Text.Json.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    public sealed class JsonSerializationSchema<T> : ISerializationSchema<T>
    {
        private readonly JsonSerializerOptions? _options;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonSerializationSchema{T}"/>.
        /// </summary>
        /// <param name="options">Optional JSON serializer options</param>
        public JsonSerializationSchema(JsonSerializerOptions? options = null)
        {
            _options = options;
        }

        /// <inheritdoc/>
        public byte[] Serialize(T element)
        {
            return JsonSerializer.SerializeToUtf8Bytes(element, _options);
        }
    }

    /// <summary>
    /// JSON-based implementation of <see cref="ISimpleVersionedSerializer{T}"/>.
    /// Serializes and deserializes objects using System.Text.Json with versioning support.
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    public sealed class JsonSimpleVersionedSerializer<T> : ISimpleVersionedSerializer<T>
    {
        private readonly JsonSerializerOptions? _options;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonSimpleVersionedSerializer{T}"/>.
        /// </summary>
        /// <param name="version">The version of this serializer</param>
        /// <param name="options">Optional JSON serializer options</param>
        public JsonSimpleVersionedSerializer(int version = 1, JsonSerializerOptions? options = null)
        {
            Version = version;
            _options = options;
        }

        /// <inheritdoc/>
        public int Version { get; }

        /// <inheritdoc/>
        public byte[] Serialize(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, _options);
        }

        /// <inheritdoc/>
        public T Deserialize(int version, byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes, _options)
                ?? throw new JsonException($"Deserialization returned null for type {typeof(T).Name}");
        }
    }
}
