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

        // Écriture rapide d'un tableau
        public static void WriteArray<T>(this BinaryWriter writer, T[] values) where T : struct
        {
            int bytesToWrite = values.Length * Marshal.SizeOf<T>();
            byte[] buffer = new byte[bytesToWrite];
            Buffer.BlockCopy(values, 0, buffer, 0, bytesToWrite);
            writer.Write(buffer);
        }
    }
}