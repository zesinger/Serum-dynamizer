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
            }
            using (Stream stream = File.Open(filepath, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    frm.tLog.AppendText(Environment.NewLine + "Writing " + Path.GetFileName(filepath) + Environment.NewLine);
                    // 64 char: name of the Serum
                    BinaryExtensions.WriteArray<char>(writer, nS.name);
                    UInt16 version= (UInt16)(nS.LengthHeader / sizeof(uint));
                    // 1 ushort: version of the Serum (former length of header / 4)
                    writer.Write(version);
                    bool isNewFormat = (version >= 14);
                    // 1 ushort: width of the frames
                    writer.Write((UInt16)nS.fWidth);
                    // 1 ushort: height of the frames
                    writer.Write((UInt16)nS.fHeight);
                    if (isNewFormat)
                    {
                        // if new format:
                        // 1 ushort: width of the extra frames
                        writer.Write((UInt16)nS.fWidthX);
                        // 1 ushort: height of the extra frames
                        writer.Write((UInt16)nS.fHeightX);
                    }
                    // 1 ushort: number of frames
                    writer.Write((UInt16)nS.nFrames);
                    // 1 ushort: number of colors in the original ROM
                    writer.Write((UInt16)nS.noColors);
                    // if old format:
                    // 1 ushort: number of colors in the colorized ROM
                    if (!isNewFormat) writer.Write((UInt16)nS.ncColors);
                    // 1 ushort: number of comparision masks
                    writer.Write((UInt16)nS.nCompMasks);
                    // 1 ushort: number of sprites
                    writer.Write((UInt16)nS.nSprites);
                    // if versions >= 13 : 1 ushort: number of backgrounds
                    if (version >= 13) writer.Write((UInt16)nS.nBackgrounds);
                    // if versions >= 20 : 1 byte: is256x64
                    if (version >= 20) writer.Write((byte)nS.is256x64);
                    // nframes * 1 byte: hash code of each frame
                    BinaryExtensions.WriteArray(writer, nS.HashCode);
                    // (nframes + 7)/8 * 1 byte: shape comparison mode of each frame (bit-compressed)
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.ShapeCompMode));
                    // ncompmasks * 1 ushort: IDs of the comparison masks for each frame
                    BinaryExtensions.WriteArray(writer, nS.CompMaskID);
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.CompMasks));
                    if (!isNewFormat)
                    {
                        BinaryExtensions.WriteArray(writer, nS.Palettes);
                        BinaryExtensions.WriteArray(writer, nS.v1cFrames);
                    }
                    else
                    {
                        (UInt16 nxfrs, UInt16[] frmidXs, UInt16[] frmXs) = ListActiveXFrames(nS.isExtraFrame, nS.cFramesX, nS.fWidthX, nS.fHeightX);
                        frm.tLog.AppendText("Number of extra frames kept: " + nxfrs.ToString() + " out of: " + nS.nFrames.ToString() + Environment.NewLine);
                        writer.Write(nxfrs);
                        BinaryExtensions.WriteArray(writer, nS.cFrames);
                        BinaryExtensions.WriteArray(writer, frmidXs!);
                        BinaryExtensions.WriteArray(writer, frmXs!);
                    }
                    // pdmaskIDs contains the IDs of the dynamic masks (0xFFFF for empty masks)
                    (UInt16 nmsk, UInt16 nmskx, UInt16[] pdmaskIDs, UInt16[] pdmaskIDXs, byte[] pdmasks, byte[] pdmaskXs, byte[] pdcolv1s, ushort[] pdcols, ushort[] pdcolXs) = 
                        ConvertDMasks(nS.nFrames, MAX_DYNA_SETS_PER_FRAMEN, nS.fWidth, nS.fHeight, nS.fWidthX, nS.fHeightX, nS.noColors, isNewFormat, nS.DynaMasks, nS.DynaMasksX, nS.v1Dyna4Cols, nS.Dyna4Cols, nS.Dyna4ColsX);
                    writer.Write(nmsk);
                    BinaryExtensions.WriteArray(writer, pdmaskIDs);
                    BinaryExtensions.WriteArray(writer, pdmasks); // these mask buffers contain values, they can't be bit-compressed
                    if (!isNewFormat) BinaryExtensions.WriteArray(writer, pdcolv1s);
                    else
                    {
                        writer.Write(nmskx);
                        BinaryExtensions.WriteArray(writer, pdmaskIDXs);
                        BinaryExtensions.WriteArray(writer, pdmaskXs);
                        BinaryExtensions.WriteArray(writer, pdcols);
                        BinaryExtensions.WriteArray(writer, pdcolXs);
                    }
                    BinaryExtensions.WriteArray(writer, nS.FrameSprites);
                    if (!isNewFormat) BinaryExtensions.WriteArray(writer, nS.v1SpriteDescription);
                    else
                    {
                        (ushort nXSpr, ushort[] XSprID, byte[] XSprMsk, ushort[] XSprCol) =
                            ListActiveXSprites(nS.isExtraSprite, nS.SpriteMaskX, nS.SpriteColoredX, MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT);
                        frm.tLog.AppendText("Number of extra sprites kept: " + nXSpr.ToString() + " out of: " + nS.nSprites.ToString() + Environment.NewLine);
                        writer.Write(nXSpr);
                        BinaryExtensions.WriteArray(writer, nS.SpriteOriginal);
                        BinaryExtensions.WriteArray(writer, nS.SpriteColored);
                        BinaryExtensions.WriteArray(writer, XSprID!);
                        BinaryExtensions.WriteArray(writer, XSprMsk!);
                        BinaryExtensions.WriteArray(writer, XSprCol!);
                    }
                    if (nS.LengthHeader >= 9 * sizeof(uint))
                    {
                        if (!isNewFormat)
                        {
                            BinaryExtensions.WriteArray(writer, nS.v1ColorRotations);
                        }
                        else
                        {
                            (uint lenColRotBuf, uint[] colRotDef, UInt16[] colRotBuf) = PackColorRotations(nS.ColorRotations);
                            (uint lenColRotBufX, uint[] colRotDefX, UInt16[] colRotBufX) = PackColorRotations(nS.ColorRotationsX);
                            writer.Write(lenColRotBuf);
                            writer.Write(lenColRotBufX);
                            BinaryExtensions.WriteArray(writer, colRotDef!);
                            BinaryExtensions.WriteArray(writer, colRotBuf!);
                            BinaryExtensions.WriteArray(writer, colRotDefX!);
                            BinaryExtensions.WriteArray(writer, colRotBufX!);
                        }
                        if (nS.LengthHeader >= 10 * sizeof(uint))
                        {
                            BinaryExtensions.WriteArray(writer, nS.SpriteDetDwords);
                            BinaryExtensions.WriteArray(writer, nS.SpriteDetDwordPos);
                            BinaryExtensions.WriteArray(writer, nS.SpriteDetAreas);
                            if (nS.LengthHeader >= 11 * sizeof(uint))
                            {
                                BinaryExtensions.WriteArray(writer, nS.TriggerID);
                                if (nS.LengthHeader >= 12 * sizeof(uint))
                                {
                                    BinaryExtensions.WriteArray(writer, nS.FrameSpriteBB);
                                    if (nS.LengthHeader >= 13 * sizeof(uint))
                                    {
                                        BinaryExtensions.WriteArray(writer, nS.BackgroundID);
                                        if (!isNewFormat)
                                        {
                                            BinaryExtensions.WriteArray(writer, nS.v1BackgroundFrames);
                                            BinaryExtensions.WriteArray(writer, nS.v1BackgroundBB);
                                        }
                                        else
                                        {
                                            (UInt16 nxBGs, UInt16[] BGidXs, UInt16[] BGXs) = 
                                                ListActiveXFrames(nS.isExtraBackground, nS.BackgroundFramesX, nS.fWidthX, nS.fHeightX);
                                            frm.tLog.AppendText("Number of extra backgrounds kept: " + nxBGs.ToString() + " out of: " + nS.nBackgrounds.ToString() + Environment.NewLine);
                                            writer.Write(nxBGs);
                                            (UInt16 nBGM, UInt16[] BGMId, byte[] BGMBuf) = 
                                                PackBackgroundMasks(nS.BackgroundID, nS.BackgroundMask, nS.fWidth, nS.fHeight);
                                            (UInt16 nBGXM, UInt16[] BGXMId, byte[] BGXMBuf) = 
                                                PackBackgroundMasks(nS.BackgroundID, nS.BackgroundMaskX, nS.fWidthX, nS.fHeightX);
                                            writer.Write(nBGM);
                                            writer.Write(nBGXM);
                                            BinaryExtensions.WriteArray(writer, nS.BackgroundFrames);
                                            BinaryExtensions.WriteArray(writer, BGidXs!);
                                            BinaryExtensions.WriteArray(writer, BGXs!);
                                            BinaryExtensions.WriteArray(writer, BGMId!);
                                            BinaryExtensions.WriteArray(writer, BGMBuf!);
                                            BinaryExtensions.WriteArray(writer, BGXMId!);
                                            BinaryExtensions.WriteArray(writer, BGXMBuf!);
                                        }
                                        if (nS.LengthHeader >= 15 * sizeof(uint))
                                        {
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsDirO));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsColO));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDXs, nS.DynaShadowsDirX));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDXs, nS.DynaShadowsColX));
                                            if (nS.LengthHeader >= 18 * sizeof(uint))
                                            {
                                                (UInt16 nsmsk, UInt16 nsmskx, UInt16[] pdsmaskIDs, UInt16[] pdsmaskIDXs, byte[] pdsmasks, byte[] pdsmaskXs, byte[] pdscolv1s, ushort[] pdscols, ushort[] pdscolXs) =
                                                    ConvertDMasks(nS.nSprites, MAX_DYNA_SETS_PER_SPRITE, MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT, MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT, nS.noColors, true, nS.DynaSpriteMasks, nS.DynaSpriteMasksX, new byte[0], nS.DynaSprite4Cols, nS.DynaSprite4ColsX);
                                                writer.Write(nsmsk);
                                                writer.Write(nsmskx);
                                                BinaryExtensions.WriteArray(writer, pdsmaskIDs!);
                                                BinaryExtensions.WriteArray(writer, pdsmasks!);
                                                BinaryExtensions.WriteArray(writer, pdscols!);
                                                BinaryExtensions.WriteArray(writer, pdsmaskIDXs!);
                                                BinaryExtensions.WriteArray(writer, pdsmaskXs!);
                                                BinaryExtensions.WriteArray(writer, pdscolXs!);
                                                if (nS.LengthHeader >= 19 * sizeof(uint))
                                                {
                                                    BinaryExtensions.WriteArray(writer, nS.SpriteShapeMode);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    frm.tLog.AppendText("File written successfully." + Environment.NewLine);
                    frm.tLog.AppendText("Size of the µSerum is: " + Form1.FormatSize(stream.Length));
                }
            }
        }


        T[] PackDynaShadows<T>(UInt16[] pdmaskIDs, T[] DSElt)
        {
            List<T> dsbuf = new List<T>();
            for (int i = 0; i < pdmaskIDs.Length; i++)
            {
                if (pdmaskIDs[i] != 0xFFFF)
                {
                    for (int j = 0; j < MAX_DYNA_SETS_PER_FRAMEN; j++) // forcément du v2 pour les dyna shadows
                    {
                        dsbuf.Add(DSElt[i * MAX_DYNA_SETS_PER_FRAMEN + j]);
                    }
                }
            }
            return dsbuf.ToArray();
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
        (UInt16, UInt16[], UInt16[]) ListActiveXFrames(byte[] isXFrame, UInt16[] Xframes, uint width, uint height)
        {
            List<UInt16> frmids = new List<UInt16>();
            List<UInt16> frms = new List<UInt16>();
            UInt16 nXfrms = 0;
            for (int i = 0; i < isXFrame.Length; i++)
            {
                if (isXFrame[i] > 0)
                {
                    frmids.Add(nXfrms);
                    for (int j = 0; j < width * height; j++)
                    {
                        frms.Add(Xframes[i * width * height + j]);
                    }
                    nXfrms++;
                }
                else frmids.Add(0xFFFF);
            }
            return (nXfrms, frmids.ToArray(), frms.ToArray());
        }
        (UInt16, UInt16[], byte[], UInt16[]) ListActiveXSprites(byte[] isXSprite, byte[] xsprmsk, UInt16[] xsprcol, uint width, uint height)
        {
            List<UInt16> sprids = new List<UInt16>();
            List<byte> sprmsk = new List<byte>();
            List<UInt16> sprcol = new List<UInt16>();
            UInt16 nXsprs = 0;
            for (int i = 0; i < isXSprite.Length; i++)
            {
                if (isXSprite[i] > 0)
                {
                    sprids.Add(nXsprs);
                    for (int j = 0; j < width * height; j++)
                    {
                        sprmsk.Add(xsprmsk[i * width * height + j]);
                        sprcol.Add(xsprcol[i * width * height + j]);
                    }
                    nXsprs++;
                }
                else sprids.Add(0xFFFF);
            }
            return (nXsprs, sprids.ToArray(), ConvertByteToBit(sprmsk.ToArray()), sprcol.ToArray());
        }
        (UInt16, UInt16, UInt16[], UInt16[], byte[], byte[], byte[], ushort[], ushort[]) ConvertDMasks(uint nframes, int max_n_sets, uint width, uint height, uint widthx, uint heightx, uint nocolors, bool isv2, byte[] DynaMasks, byte[] DynaMasksX, byte[] v1Dyna4Cols, ushort[] Dyna4Cols, ushort[] Dyna4ColsX)
        {
            List<UInt16> pdmaskIDs = new List<UInt16>();
            List<UInt16> pdmaskIDXs = new List<UInt16>();
            List<byte> pdmasks = new List<byte>();
            List<byte> pdmaskXs = new List<byte>();
            List<byte> pdcolv1s = new List<byte>();
            List<ushort> pdcols = new List<ushort>();
            List<ushort> pdcolXs = new List<ushort>();
            //uint posmask = 0, posmaskX=0;
            //uint poscolv1 = 0, poscol = 0;
            ushort acmsk = 0, acmskx = 0;
            for (uint k = 0; k < nframes; k++)
            {
                bool isempty = true;
                byte bitmask = 0x80;
                byte valb = 0;
                for (int i = 0; i < width * height; i++)
                {
                    if (DynaMasks[k * width * height + i] > 0)
                    {
                        isempty = false;
                        valb |= bitmask;
                    }
                    bitmask >>= 1;
                    if (bitmask == 0)
                    {
                        pdmasks.Add(valb);
                        valb = 0;
                        bitmask = 0x80;
                    }
                }
                if (isempty)
                {
                    pdmaskIDs.Add(0xFFFF);
                    pdmasks.RemoveRange(pdmasks.Count - (int)(width * height / 8), (int)(width * height / 8));
                }
                else
                {
                    pdmaskIDs.Add(acmsk);
                    if (!isv2) // jamais vrai pour les sprites dynamiques (v>=18)
                    {
                        pdcolv1s.AddRange(new ArraySegment<byte>(v1Dyna4Cols, (int)(k * nocolors * 16), (int)nocolors * 16));
                    }
                    else
                    {
                        pdcols.AddRange(new ArraySegment<ushort>(Dyna4Cols, (int)(k * nocolors * max_n_sets), (int)nocolors * max_n_sets));
                    }
                    acmsk++;
                }
                if (isv2)
                {
                    isempty = true;
                    bitmask = 0x80;
                    valb = 0;
                    for (int i = 0; i < widthx * heightx; i++)
                    {
                        if (DynaMasksX[k * widthx * heightx + i] > 0)
                        {
                            isempty = false;
                            valb |= bitmask;
                        }
                        bitmask >>= 1;
                        if (bitmask == 0)
                        {
                            pdmaskXs.Add(valb);
                            valb = 0;
                            bitmask = 0x80;
                        }
                    }
                    if (isempty)
                    {
                        pdmaskIDXs.Add(0xFFFF);
                        pdmaskXs.RemoveRange(pdmaskXs.Count - (int)(widthx * heightx / 8), (int)(widthx * heightx / 8));
                    }
                    else
                    {
                        pdmaskIDXs.Add(acmskx);
                        pdcolXs.AddRange(new ArraySegment<ushort>(Dyna4ColsX, (int)(k * nocolors * max_n_sets), (int)nocolors * max_n_sets));
                        acmskx++;
                    }
                }
            }
            return (acmsk, acmskx, pdmaskIDs.ToArray(), pdmaskIDXs.ToArray(), pdmasks.ToArray(), pdmaskXs.ToArray(), pdcolv1s.ToArray(), pdcols.ToArray(), pdcolXs.ToArray());
        }
        (uint, uint[], UInt16[]) PackColorRotations(UInt16[] CR)
        {
            List<uint> def = new List<uint>();
            List<UInt16> buf = new List<UInt16>();
            int acpos = 0;
            while (acpos <= CR.Length - MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION)
            {
                uint nrot = 0;
                for (int i = 0; i < MAX_COLOR_ROTATIONN; i++)
                {
                    if (CR[acpos + i * MAX_LENGTH_COLOR_ROTATION] > 0) nrot++;
                }
                if (nrot > 0)
                {
                    for (int i = 0; i < MAX_COLOR_ROTATIONN; i++)
                    {
                        if (CR[acpos + i * MAX_LENGTH_COLOR_ROTATION] > 0)
                        {
                            def.Add((uint)buf.Count);
                            for (int j = 0; j < CR[acpos + i * MAX_LENGTH_COLOR_ROTATION] + 2; j++)
                                buf.Add(CR[acpos + i * MAX_LENGTH_COLOR_ROTATION + j]);
                        }
                    }
                }
                // fill the rest of the rotation definitions with empty rotations
                for (int i = 0; i < MAX_COLOR_ROTATIONN - nrot; i++) def.Add(0xFFFFFFFF);
                acpos += MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION;
            }
            return ((uint)buf.Count, def.ToArray(), buf.ToArray());
        }
        (UInt16, UInt16[], byte[]) PackBackgroundMasks(ushort[] BGid, byte[] BGmsk, uint width, uint height)
        {
            List<UInt16> mbgids = new List<UInt16>();
            List<byte> mbuf = new List<byte>();
            UInt16 nm = 0;
            for (int i = 0; i < BGid.Length; i++)
            {
                if (BGid[i] != 0xFFFF)
                {
                    bool hasvalues = false;
                    byte[] acBGM = new byte[width * height];
                    for (int j = 0; j < width * height; j++)
                    {
                        acBGM[j] = BGmsk[i * width * height + j];
                        if (acBGM[j] > 0) hasvalues = true;
                    }
                    if (hasvalues)
                    {
                        // store the mask ID and the mask itself
                        mbgids.Add(nm);
                        mbuf.AddRange(ConvertByteToBit(acBGM));
                        nm++;
                    }
                    else mbgids.Add(0xFFFE); // 0xFFFE empty mask
                }
                else mbgids.Add(0xFFFF); // 0xFFFF no mask
            }
            return (nm, mbgids.ToArray(), mbuf.ToArray());
        }
    }
}
