using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoorOverhaul.Destruction
{
    internal sealed class MeshCopy
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Vector2[] Uv0;
        public Vector2[] Uv1;
        public int[][] SubmeshTriangles;
        public string Note = string.Empty;

        public int VertexCount => Positions?.Length ?? 0;
    }

    internal static class MeshReadback
    {
        public static void Read(Mesh mesh, Action<MeshCopy, string> done)
        {
            if (mesh == null)
            {
                done(null, "no mesh");
                return;
            }

            if (mesh.isReadable)
            {
                done(FromReadable(mesh), null);
                return;
            }

            GraphicsBuffer indexBuffer = null;
            var vertexBuffers = new GraphicsBuffer[mesh.vertexBufferCount];
            try
            {
                for (int s = 0; s < vertexBuffers.Length; s++)
                {
                    vertexBuffers[s] = mesh.GetVertexBuffer(s);
                }

                indexBuffer = mesh.GetIndexBuffer();
            }
            catch (Exception ex)
            {
                Dispose(vertexBuffers, indexBuffer);
                done(null, $"buffer access failed: {ex.Message}");
                return;
            }

            if (indexBuffer == null || Array.Exists(vertexBuffers, b => b == null))
            {
                Dispose(vertexBuffers, indexBuffer);
                done(null, "GetVertexBuffer/GetIndexBuffer returned null");
                return;
            }

            var streams = new byte[vertexBuffers.Length][];
            byte[] indices = null;
            int pending = vertexBuffers.Length + 1;
            string error = null;

            void Finish()
            {
                if (--pending > 0)
                {
                    return;
                }

                Dispose(vertexBuffers, indexBuffer);
                if (error != null)
                {
                    done(null, error);
                    return;
                }

                try
                {
                    done(Decode(mesh, streams, indices), null);
                }
                catch (Exception ex)
                {
                    done(null, $"decode failed: {ex.Message}");
                }
            }

            for (int s = 0; s < vertexBuffers.Length; s++)
            {
                int stream = s;
                AsyncGPUReadback.Request(vertexBuffers[s], request =>
                {
                    if (request.hasError)
                    {
                        error = error ?? $"vertex stream {stream} readback failed";
                    }
                    else
                    {
                        streams[stream] = request.GetData<byte>().ToArray();
                    }

                    Finish();
                });
            }

            AsyncGPUReadback.Request(indexBuffer, request =>
            {
                if (request.hasError)
                {
                    error = error ?? "index readback failed";
                }
                else
                {
                    indices = request.GetData<byte>().ToArray();
                }

                Finish();
            });
        }

        private static MeshCopy FromReadable(Mesh mesh)
        {
            var copy = new MeshCopy
            {
                Positions = mesh.vertices,
                Normals = mesh.normals,
                Tangents = mesh.tangents,
                Uv0 = mesh.uv,
                Uv1 = mesh.uv2,
                SubmeshTriangles = new int[mesh.subMeshCount][],
                Note = "readable"
            };
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                copy.SubmeshTriangles[i] = mesh.GetTriangles(i);
            }

            return copy;
        }

        private static MeshCopy Decode(Mesh mesh, byte[][] streams, byte[] indexData)
        {
            int count = mesh.vertexCount;
            var copy = new MeshCopy();
            foreach (VertexAttributeDescriptor attribute in mesh.GetVertexAttributes())
            {
                int stream = attribute.stream;
                int stride = mesh.GetVertexBufferStride(stream);
                int offset = mesh.GetVertexAttributeOffset(attribute.attribute);
                byte[] data = streams[stream];
                switch (attribute.attribute)
                {
                    case VertexAttribute.Position:
                        copy.Positions = Read3(data, count, stride, offset, attribute);
                        break;
                    case VertexAttribute.Normal:
                        copy.Normals = Read3(data, count, stride, offset, attribute);
                        break;
                    case VertexAttribute.Tangent:
                        copy.Tangents = Read4(data, count, stride, offset, attribute);
                        break;
                    case VertexAttribute.TexCoord0:
                        copy.Uv0 = Read2(data, count, stride, offset, attribute);
                        break;
                    case VertexAttribute.TexCoord1:
                        copy.Uv1 = Read2(data, count, stride, offset, attribute);
                        break;
                }
            }

            bool wide = mesh.indexFormat == IndexFormat.UInt32;
            copy.SubmeshTriangles = new int[mesh.subMeshCount][];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                SubMeshDescriptor sub = mesh.GetSubMesh(i);
                var triangles = new int[sub.topology == MeshTopology.Triangles ? sub.indexCount : 0];
                for (int k = 0; k < triangles.Length; k++)
                {
                    int at = sub.indexStart + k;
                    int index = wide ? (int)BitConverter.ToUInt32(indexData, at * 4) : BitConverter.ToUInt16(indexData, at * 2);
                    triangles[k] = index + sub.baseVertex;
                }

                copy.SubmeshTriangles[i] = triangles;
            }

            copy.Note = $"{mesh.vertexBufferCount} stream(s), index {mesh.indexFormat}, attributes " +
                string.Join(",", Array.ConvertAll(mesh.GetVertexAttributes(), a => $"{a.attribute}:{a.format}x{a.dimension}"));
            return copy;
        }

        public static string Validate(Mesh mesh, MeshCopy copy)
        {
            if (copy?.Positions == null || copy.Positions.Length != mesh.vertexCount)
            {
                return "position count mismatch";
            }

            Bounds bounds = mesh.bounds;
            bounds.Expand(bounds.size.magnitude * 0.01f + 1e-4f);
            int outside = 0;
            foreach (Vector3 p in copy.Positions)
            {
                if (!bounds.Contains(p))
                {
                    outside++;
                }
            }

            if (outside > 0)
            {
                return $"{outside}/{copy.Positions.Length} positions outside the mesh bounds";
            }

            foreach (int[] triangles in copy.SubmeshTriangles)
            {
                foreach (int index in triangles)
                {
                    if (index < 0 || index >= copy.Positions.Length)
                    {
                        return $"triangle index {index} out of range";
                    }
                }
            }

            return null;
        }

        private static Vector2[] Read2(byte[] data, int count, int stride, int offset, VertexAttributeDescriptor a)
        {
            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                int at = i * stride + offset;
                result[i] = new Vector2(Component(data, at, a.format, 0), a.dimension > 1 ? Component(data, at, a.format, 1) : 0f);
            }

            return result;
        }

        private static Vector3[] Read3(byte[] data, int count, int stride, int offset, VertexAttributeDescriptor a)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int at = i * stride + offset;
                result[i] = new Vector3(Component(data, at, a.format, 0),
                    a.dimension > 1 ? Component(data, at, a.format, 1) : 0f,
                    a.dimension > 2 ? Component(data, at, a.format, 2) : 0f);
            }

            return result;
        }

        private static Vector4[] Read4(byte[] data, int count, int stride, int offset, VertexAttributeDescriptor a)
        {
            var result = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                int at = i * stride + offset;
                result[i] = new Vector4(Component(data, at, a.format, 0),
                    a.dimension > 1 ? Component(data, at, a.format, 1) : 0f,
                    a.dimension > 2 ? Component(data, at, a.format, 2) : 0f,
                    a.dimension > 3 ? Component(data, at, a.format, 3) : 1f);
            }

            return result;
        }

        private static float Component(byte[] data, int at, VertexAttributeFormat format, int index)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                    return BitConverter.ToSingle(data, at + index * 4);
                case VertexAttributeFormat.Float16:
                    return Mathf.HalfToFloat(BitConverter.ToUInt16(data, at + index * 2));
                case VertexAttributeFormat.UNorm8:
                    return data[at + index] / 255f;
                case VertexAttributeFormat.SNorm8:
                    return Mathf.Max((sbyte)data[at + index] / 127f, -1f);
                case VertexAttributeFormat.UNorm16:
                    return BitConverter.ToUInt16(data, at + index * 2) / 65535f;
                case VertexAttributeFormat.SNorm16:
                    return Mathf.Max(BitConverter.ToInt16(data, at + index * 2) / 32767f, -1f);
                default:
                    throw new NotSupportedException($"vertex format {format}");
            }
        }

        private static void Dispose(GraphicsBuffer[] vertexBuffers, GraphicsBuffer indexBuffer)
        {
            foreach (GraphicsBuffer buffer in vertexBuffers)
            {
                buffer?.Dispose();
            }

            indexBuffer?.Dispose();
        }
    }
}
