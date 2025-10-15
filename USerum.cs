using K4os.Compression.LZ4;
using System.Runtime.InteropServices;

namespace Serum_dynamizer
{
    internal class USerum
    {
        public const int MAX_DYNA_SETS_PER_FRAMEN = 32; // max number of dynamic sets per frame
        public const int MAX_SPRITES_PER_FRAME = 32;  // maximum amount of sprites to look for per frame
        public const int MAX_SPRITE_WIDTH = 256; // maximum width of the new sprites
        public const int MAX_SPRITE_HEIGHT = 64; // maximum height of the new sprites
        public const int MAX_COLOR_ROTATIONN = 4; // maximum number of new color rotations per frame
        public const int MAX_LENGTH_COLOR_ROTATION = 64; // maximum number of new colors in a rotation
        public const int MAX_SPRITE_DETECT_AREAS = 4;  // maximum number of areas to detect the sprite
        public const int MAX_DYNA_SETS_PER_SPRITE = 9;	// max number of color sets for dynamic content for each sprite (new version)

        public USerum(Serum nS, Form1 frm)
        {
            string filepath = Path.Combine(Path.GetDirectoryName(nS.FilePath), Path.GetFileNameWithoutExtension(nS.FilePath) + ".ucR");
            if (File.Exists(filepath))
            {
                if (MessageBox.Show("The file " + Path.GetFileName(filepath) + " already exists. Do you want to overwrite it?", "File exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
                File.Delete(filepath); // mandatory as my program append to the existing file
            }
            using (Stream stream = File.Open(filepath, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    frm.tLog.AppendText(Environment.NewLine + "Writing " + Path.GetFileName(filepath) + Environment.NewLine);
                    // 64 char: name of the Serum
                    BinaryExtensions.WriteArray<char>(writer, nS.name);
                    ushort version = (ushort)(nS.LengthHeader / sizeof(uint));
                    // 1 ushort: version of the Serum (former length of header / 4)
                    writer.Write(version);
                    // 1 ushort: width of the frames
                    writer.Write((ushort)nS.fWidth);
                    // 1 ushort: height of the frames
                    writer.Write((ushort)nS.fHeight);
                    // 1 ushort: width of the extra frames
                    writer.Write((ushort)nS.fWidthX);
                    // 1 ushort: height of the extra frames
                    writer.Write((ushort)nS.fHeightX);
                    // 1 ushort: number of frames
                    writer.Write((ushort)nS.nFrames);
                    // 1 ushort: number of colors in the original ROM
                    writer.Write((ushort)nS.noColors);
                    // 1 ushort: number of comparision masks
                    writer.Write((ushort)nS.nCompMasks);
                    // 1 ushort: number of sprites
                    writer.Write((ushort)nS.nSprites);
                    // 1 ushort: number of backgrounds
                    writer.Write((ushort)nS.nBackgrounds);
                    // if versions >= 20 : 1 byte: is256x64
                    if (version >= 20) writer.Write((byte)nS.is256x64);
                    // ---------------------- Speed optimized data = not compressed (for identification) -------------------
                    // nframes bytes: hash code of each frame
                    BinaryExtensions.WriteArray(writer, nS.HashCode);
                    // (nframes + 7)/8 bytes: shape comparison mode of each frame (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.ShapeCompMode));
                    // nFrames ushorts: IDs of the comparison masks for each frame
                    BinaryExtensions.WriteArray(writer, nS.CompMaskID);
                    // (ncompmasks * fWidth * fHeight + 7) / 8 bytes : bitmasks for comparison (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.CompMasks));
                    // nsprites * MAX_SPRITE_HEIGHT * MAX_SPRITE_WIDTH bytes: 4-or-16 color image of the original sprite (255 if out of the sprite)
                    BinaryExtensions.WriteArray(writer, nS.SpriteOriginal);
                    // nsprites * 4 * MAX_SPRITE_DETECT_AREAS ushorts : rectangles (left, top, width, height) as areas to detect sprites (left=0xffff -> no zone)
                    BinaryExtensions.WriteArray(writer, nS.SpriteDetAreas);
                    // nsprites * MAX_SPRITE_DETECT_AREAS uint : dword to quickly detect 4 consecutive distinctive pixels inside the original drawing of a sprite for optimized detection
                    BinaryExtensions.WriteArray(writer, nS.SpriteDetDwords);
                    // nsprites * MAX_SPRITE_DETECT_AREAS ushorts : offset of the previous dword in the sprite description
                    BinaryExtensions.WriteArray(writer, nS.SpriteDetDwordPos);
                    // if version >= 19, (nsprites + 7) / 8 bytes : shape detection mode for each sprite (bit-compressed)
                    if (version >= 19) BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.SpriteShapeMode));
                    // (nFrames + 7) / 8 bytes : is Extra Frame (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.isExtraFrame));
                    // (nSprites + 7) / 8 bytes : is Extra Sprite (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.isExtraSprite));
                    // (nBackgrounds + 7) / 8 bytes : is Extra Background (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.isExtraBackground));
                    // --------------------------- Full Lz4 compressed block : frames -------------------------
                    // we store the positions of the data for each frame
                    long[] framepositions = new long[nS.nFrames];
                    // nFrames uint : positions of the frames in the file frame data (initially all sets to 0, but updated when everything is calculated)
                    long idxposition = writer.BaseStream.Position;
                    BinaryExtensions.WriteArray(writer, framepositions);
                    long initialframeposition = writer.BaseStream.Position;
                    for (uint i = 0; i < nS.nFrames; i++)
                    {
                        framepositions[i] = writer.BaseStream.Position - initialframeposition;
                        byte[] framedata = Array.Empty<byte>();
                        // fWidth * fHeight ushort : colorized frame
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.cFrames, i, nS.fWidth * nS.fHeight);
                        // if this frame has an extra frame, fWidthX * fHeightX ushort : colorized extra frame
                        if (nS.isExtraFrame[i] > 0) BinaryExtensions.AppendArrayToBuffer(framedata, nS.cFramesX, i, nS.fWidthX * nS.fHeightX);
                        // fwidth * fheight bytes : dynamic colorization masks (can't be bit compressed as this contains values)
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaMasks, i, nS.fWidth * nS.fHeight);
                        // MAX_DYNA_SETS_PER_FRAMEN * noColors ushort : dynamic color sets
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.Dyna4Cols, i, MAX_DYNA_SETS_PER_FRAMEN * nS.noColors);
                        if (version >= 15)
                        {
                            // if version >= 15, DynaShadows are implemented :
                            //      MAX_DYNA_SETS_PER_FRAMEN bytes : flag for dynamic shadow directions
                            //      0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaShadowsDirO, i, MAX_DYNA_SETS_PER_FRAMEN);
                            //      MAX_DYNA_SETS_PER_FRAMEN ushort : colors of the dynamic shadows
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaShadowsColO, i, MAX_DYNA_SETS_PER_FRAMEN);
                        }
                        if (nS.isExtraFrame[i] > 0)
                        {
                            // if this frame has an extra frame:
                            //      fWidthX * fHeightX bytes : dynamic colorization masks for extra frames (can't be bit compressed as this contains values)
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaMasksX, i, nS.fWidthX * nS.fHeightX);
                            //      MAX_DYNA_SETS_PER_FRAMEN * noColors ushorts : dynamic color sets for extra frames
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.Dyna4ColsX, i, MAX_DYNA_SETS_PER_FRAMEN * nS.noColors);
                            if (version >= 15)
                            {
                                // if version >= 15, DynaShadows are implemented :
                                //      MAX_DYNA_SETS_PER_FRAMEN bytes : flag for dynamic shadow directions
                                //      0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
                                BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaShadowsDirX, i, MAX_DYNA_SETS_PER_FRAMEN);
                                //      MAX_DYNA_SETS_PER_FRAMEN ushort : colors of the dynamic shadows
                                BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaShadowsColX, i, MAX_DYNA_SETS_PER_FRAMEN);
                            }
                        }
                        // MAX_SPRITES_PER_FRAME bytes : sprites to be detected
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.FrameSprites, i, MAX_SPRITES_PER_FRAME);
                        // 4 * MAX_SPRITES_PER_FRAME ushort : bounding boxes for each sprite given above [minx,miny,maxx,maxy]
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.FrameSpriteBB, i, MAX_SPRITES_PER_FRAME * 4);
                        // Compressed data for colorizations:
                        // for up to 4 rotations:
                        // - 1 ushort : length of the rotation in number of colors (ncR)
                        // - 1 ushort : number of ms between 2 rotations
                        // - ncR ushorts : colors of the rotation
                        // then again for next rotation...
                        // final "0" (only if there are less than MAX_COLOR_ROTATIONN rotations)
                        BinaryExtensions.AppendArrayToBuffer(framedata, PackColorisations(nS.ColorRotations, i));
                        // same as above for extra frame, if available:
                        if (nS.isExtraFrame[i] > 0) BinaryExtensions.AppendArrayToBuffer(framedata, PackColorisations(nS.ColorRotationsX, i));
                        // 1 ushort : Pup Pack trigger ID
                        BinaryExtensions.AppendValToBuffer(framedata, (ushort)nS.TriggerID[i]);
                        // 1 ushort : Background ID (0xFFFF if no background)
                        BinaryExtensions.AppendValToBuffer(framedata, nS.BackgroundID[i]);
                        if (nS.BackgroundID[i] != 0xFFFF)
                        {
                            // if there is a background:
                            //      fWidth * fHeight / 8 : Mask for the application of the background (bit-compressed)
                            byte[] mask = new byte[nS.fWidth * nS.fHeight];
                            Array.Copy(nS.BackgroundMask, (int)(i * nS.fWidth * nS.fHeight), mask, 0, (int)(nS.fWidth * nS.fHeight));
                            BinaryExtensions.AppendArrayToBuffer(framedata, ConvertByteToBit(mask));
                            //      if there is an extra frame, fWidthX * fHeightX / 8 : Mask for the application of the background (bit-compressed)
                            if (nS.isExtraFrame[i] > 0)
                            {
                                mask = new byte[nS.fWidthX * nS.fHeightX];
                                Array.Copy(nS.BackgroundMaskX, (int)(i * nS.fWidthX * nS.fHeightX), mask, 0, (int)(nS.fWidthX * nS.fHeightX));
                                BinaryExtensions.AppendArrayToBuffer(framedata, ConvertByteToBit(mask));
                            }
                        }
                        // compress each frame using fast lz4
                        (int compsize, byte[] compbuf) = Lz4_Compress(framedata);
                        // 1 int : in the file, we store the size of the frame once lz4-compressed
                        writer.Write(compsize);
                        // compsize bytes : then we store the Lz4-compressed frame
                        BinaryExtensions.WriteArray(writer, compbuf);
                    }
                    // When all frames are processed, we update the position of the frames
                    BinaryExtensions.ModifyFileAtSpecificOffset(writer, idxposition, framepositions, true);
                    // --------------------------- Full Lz4 compressed block : sprites -------------------------
                    // we store the positions of the data for each sprite
                    framepositions = new long[nS.nSprites];
                    // nFrames uint : positions of the sprites in the file sprite data (initially all sets to 0, but updated when everything is calculated)
                    idxposition = writer.BaseStream.Position;
                    BinaryExtensions.WriteArray(writer, framepositions);
                    initialframeposition = writer.BaseStream.Position;
                    for (uint i = 0; i < nS.nSprites; i++)
                    {
                        framepositions[i] = writer.BaseStream.Position - initialframeposition;
                        byte[] framedata = Array.Empty<byte>();
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.SpriteColored, i, MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                        if (nS.isExtraSprite[i] > 0)
                        {
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.SpriteMaskX, i, MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.SpriteColoredX, i, MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                        }
                        if (version >= 18)
                        {
                            // if version >= 18, dynamic colored sprites are implemented
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaSpriteMasks, i, MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                            BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaSprite4Cols, i, MAX_DYNA_SETS_PER_SPRITE * nS.noColors);
                            if (nS.isExtraSprite[i] > 0)
                            {
                                BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaSpriteMasksX, i, MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                                BinaryExtensions.AppendArrayToBuffer(framedata, nS.DynaSprite4ColsX, i, MAX_DYNA_SETS_PER_SPRITE * nS.noColors);
                            }
                        }
                        // compress each sprite using fast lz4
                        (int compsize, byte[] compbuf) = Lz4_Compress(framedata);
                        // 1 int : in the file, we store the size of the sprite once lz4-compressed
                        writer.Write(compsize);
                        // compsize bytes : then we store the Lz4-compressed sprite
                        BinaryExtensions.WriteArray(writer, compbuf);
                    }
                    // When all sprites are processed, we update the position of the sprites
                    BinaryExtensions.ModifyFileAtSpecificOffset(writer, idxposition, framepositions, true);
                    // --------------------------- Full Lz4 compressed block : backgrounds -------------------------
                    // we store the positions of the data for each background
                    framepositions = new long[nS.nBackgrounds];
                    // nFrames uint : positions of the backgrounds in the file BG data (initially all sets to 0, but updated when everything is calculated)
                    idxposition = writer.BaseStream.Position;
                    BinaryExtensions.WriteArray(writer, framepositions);
                    initialframeposition = writer.BaseStream.Position;
                    for (uint i = 0; i < nS.nBackgrounds; i++)
                    {
                        framepositions[i] = writer.BaseStream.Position - initialframeposition;
                        byte[] framedata = Array.Empty<byte>();
                        // fWidth * fHeight ushort : colorized background
                        BinaryExtensions.AppendArrayToBuffer(framedata, nS.BackgroundFrames, i, nS.fWidth * nS.fHeight);
                        // if this background has an extra background, fWidthX * fHeightX ushort : colorized extra background
                        if (nS.isExtraBackground[i] > 0) BinaryExtensions.AppendArrayToBuffer(framedata, nS.BackgroundFramesX, i, nS.fWidthX * nS.fHeightX);
                        // compress each background using fast lz4
                        (int compsize, byte[] compbuf) = Lz4_Compress(framedata);
                        // 1 int : in the file, we store the size of the background once lz4-compressed
                        writer.Write(compsize);
                        // compsize bytes : then we store the Lz4-compressed background
                        BinaryExtensions.WriteArray(writer, compbuf);
                    }
                    // When all backgrounds are processed, we update the position of the backgrounds
                    BinaryExtensions.ModifyFileAtSpecificOffset(writer, idxposition, framepositions, true);

                    frm.tLog.AppendText("File written successfully." + Environment.NewLine);
                    frm.tLog.AppendText("Size of the µSerum is: " + Form1.FormatSize(stream.Length));
                }
            }
        }
        private ushort[] PackColorisations(ushort[] colorisations, uint index)
        {
            List<ushort> coldata = new List<ushort>();
            int nrot = 0;
            for (int i = 0; i < MAX_COLOR_ROTATIONN; i++)
            {
                if (colorisations[index * MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION + i * MAX_LENGTH_COLOR_ROTATION] == 0) continue;
                for (int j = 0; j < colorisations[index * MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION + i * MAX_LENGTH_COLOR_ROTATION] + 2; j++)
                    coldata.Add(colorisations[index * MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION + i * MAX_LENGTH_COLOR_ROTATION + j]);
                nrot++;
            }
            // !!! if there is MAX_COLOR_ROTATIONN rotations, there is no final "0"
            if (nrot < MAX_COLOR_ROTATIONN) coldata.Add(0);
            return coldata.ToArray();
        }
        // Compress a buffer using LZ4 algorithm
        public static (int lz4bufsize, byte[] lz4buffer) Lz4_Compress<T>(T[] buffer)
            where T : unmanaged // pour dire que les données du buffer sont des types standards à longueur fixe (par exemple une string n'est pas unmanaged)
        {
            if (buffer == null || buffer.Length == 0) return (0, Array.Empty<byte>());

            // Convertit le buffer vers ReadOnlySpan<byte> sans copie
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(buffer.AsSpan());

            // Calculer la taille max compressée
            int maxCompressedSize = LZ4Codec.MaximumOutputSize(bytes.Length);
            byte[] compressed = new byte[maxCompressedSize];

            // Compression
            int compressedLength = LZ4Codec.Encode(bytes, compressed.AsSpan());

            // Redimensionner le tableau pour n’avoir que les octets utiles
            Array.Resize(ref compressed, compressedLength);

            return (compressedLength, compressed);
        }

        public static byte[] ConvertByteToBit(byte[] input)
        {
            int outputLength = (input.Length + 7) / 8;
            byte[] output = new byte[outputLength]; // each byte is initialized to 0

            for (int i = 0; i < input.Length; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = 7 - (i % 8); // on met le bit de poids fort à gauche
                output[byteIndex] |= (byte)(input[i] << bitIndex);
            }
            return output;
        }
    }
}
