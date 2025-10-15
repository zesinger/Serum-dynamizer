using System.Runtime.InteropServices;

namespace Serum_dynamizer
{
    public static class BinaryExtensions
    {
        // Vérifie si le type est “blittable” (mémoire contiguë)
        private static void CheckBlittable<T>()
        {
            if (!MemoryMarshal.TryGetArray<T>(new T[0], out _))
                throw new InvalidOperationException($"{typeof(T)} n'est pas un type blittable.");
        }

        // Lecture rapide d'un tableau
        public static T[] ReadArray<T>(this BinaryReader reader, uint count) where T : struct
        {
            T[] result = new T[count];
            int bytesToRead = (int)count * Marshal.SizeOf<T>();
            byte[] buffer = reader.ReadBytes(bytesToRead);

            if (buffer.Length != bytesToRead)
                throw new EndOfStreamException("Impossible de lire tous les éléments demandés.");

            Buffer.BlockCopy(buffer, 0, result, 0, bytesToRead);
            return result;
        }

        // Écriture rapide d'un tableau dans un fichier
        public static void WriteArray<T>(this BinaryWriter writer, T[] values) where T : struct
        {
            int bytesToWrite = values.Length * Marshal.SizeOf<T>();
            byte[] buffer = new byte[bytesToWrite];
            Buffer.BlockCopy(values, 0, buffer, 0, bytesToWrite);
            writer.Write(buffer);
        }
        // Ajout rapide d'un tableau dans un buffer
        public static byte[] AppendArrayToBuffer<T>(byte[] existingBuffer, T[] values, uint index, uint size) where T : struct
        {
            int existingLength = existingBuffer.Length;
            int bytesToWrite = (int)(size * Marshal.SizeOf<T>());

            byte[] newBuffer = new byte[existingLength + bytesToWrite];

            // Copy existing buffer
            Buffer.BlockCopy(existingBuffer, 0, newBuffer, 0, existingLength);

            // Append new array
            Buffer.BlockCopy(values, (int)(index * size), newBuffer, existingLength, bytesToWrite);

            return newBuffer;
        }
        public static byte[] AppendArrayToBuffer<T>(byte[] existingBuffer, T[] values) where T : struct
        {
            int existingLength = existingBuffer.Length;
            int bytesToWrite = values.Length * Marshal.SizeOf<T>();

            byte[] newBuffer = new byte[existingLength + bytesToWrite];

            // Copy existing buffer
            Buffer.BlockCopy(existingBuffer, 0, newBuffer, 0, existingLength);

            // Append new array
            Buffer.BlockCopy(values, 0, newBuffer, existingLength, bytesToWrite);

            return newBuffer;
        }
        // Ajout d'une valeur dans un buffer
        public static byte[] AppendValToBuffer<T>(byte[] existingBuffer, T value) where T : struct
        {
            // Get the size of the struct
            int valueSize = Marshal.SizeOf(value);

            // Create a new byte array with combined length
            byte[] newBuffer = new byte[existingBuffer.Length + valueSize];

            // Copy existing buffer
            Buffer.BlockCopy(existingBuffer, 0, newBuffer, 0, existingBuffer.Length);

            // Convert struct to byte array
            byte[] valueBytes = new byte[valueSize];
            GCHandle handle = GCHandle.Alloc(valueBytes, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
                // Copy value bytes to the end of the new buffer
                Buffer.BlockCopy(valueBytes, 0, newBuffer, existingBuffer.Length, valueSize);
            }
            finally
            {
                handle.Free();
            }

            return newBuffer;
        }
        public static void ModifyFileAtSpecificOffset<T>(BinaryWriter writer, long offset, T[] newValues, bool returnposition) where T : struct
        {
            // Store the current position
            long originalPosition = writer.BaseStream.Position;

            try
            {
                // Move to the specific offset
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);

                // Write the new values
                WriteArray(writer, newValues);

                // Return to the original position
                if (returnposition) writer.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error modifying file: {ex.Message}");
                // Attempt to restore original position in case of error
                writer.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            }
        }

    }
}