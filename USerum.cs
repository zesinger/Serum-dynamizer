using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    frm.tLog.Text += Environment.NewLine + "Writing " + Path.GetFileName(filepath) + Environment.NewLine;
                    BinaryExtensions.WriteArray<char>(writer, nS.name);
                    writer.Write((UInt16)(nS.LengthHeader / sizeof(uint)));
                    bool isNewFormat = (nS.LengthHeader >= 14 * sizeof(uint));
                    writer.Write((UInt16)nS.fWidth);
                    writer.Write((UInt16)nS.fHeight);
                    if (isNewFormat)
                    {
                        writer.Write((UInt16)nS.fWidthX);
                        writer.Write((UInt16)nS.fHeightX);
                    }
                    writer.Write((UInt16)nS.nFrames);
                    UInt16 nxfrs = 0;
                    UInt16[]? frmidXs = null;
                    UInt16[]? frmXs = null;
                    if (isNewFormat)
                    {
                        (nxfrs, frmidXs, frmXs) = ListActiveXFrames(nS.isExtraFrame, nS.cFramesX, nS.fWidthX, nS.fHeightX);
                    }
                    writer.Write(nxfrs);
                    writer.Write((UInt16)nS.noColors);
                    if (!isNewFormat) writer.Write((UInt16)nS.ncColors);
                    writer.Write((UInt16)nS.nCompMasks);
                    writer.Write((UInt16)nS.nSprites);
                    ushort[]? XSprID = null;
                    byte[]? XSprMsk = null;
                    ushort[]? XSprCol = null;
                    if (isNewFormat)
                    {
                        (ushort nXSpr, XSprID, XSprMsk, XSprCol) = ListActiveXSprites(nS.isExtraSprite, nS.SpriteMaskX, nS.SpriteColoredX, MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT);
                        writer.Write(nXSpr);
                    }
                    if (nS.LengthHeader >= 13 * sizeof(uint)) writer.Write((UInt16)nS.nBackgrounds);
                    UInt16 nxBGs = 0;
                    UInt16[]? BGidXs = null;
                    UInt16[]? BGXs = null;
                    UInt16 nBGM = 0, nBGXM;
                    UInt16[]? BGMId = null, BGXMId = null;
                    byte[]? BGMBuf = null, BGXMBuf = null;
                    if (isNewFormat)
                    {
                        (nxBGs, BGidXs, BGXs) = ListActiveXFrames(nS.isExtraBackground, nS.BackgroundFramesX, nS.fWidthX, nS.fHeightX);
                        writer.Write(nxBGs);
                        (nBGM, BGMId, BGMBuf) = PackBackgroundMasks(nS.BackgroundID, nS.BackgroundMask, nS.fWidth, nS.fHeight);
                        (nBGXM, BGXMId, BGXMBuf) = PackBackgroundMasks(nS.BackgroundID, nS.BackgroundMaskX, nS.fWidthX, nS.fHeightX);
                        writer.Write(nBGM);
                        writer.Write(nBGXM);
                    }

                    if (nS.LengthHeader >= 20 * sizeof(uint)) writer.Write((byte)nS.is256x64);
                    (UInt16 nmsk, UInt16 nmskx, UInt16[] pdmaskIDs, byte[] pdmasks, byte[] pdmaskXs, byte[] pdcolv1s, ushort[] pdcols, ushort[] pdcolXs) = ConvertDMasks(nS.fWidth, nS.fHeight, nS.fWidthX, nS.fHeightX, nS.noColors, isNewFormat, nS.DynaMasks, nS.DynaMasksX, nS.v1Dyna4Cols, nS.Dyna4Cols, nS.Dyna4ColsX);
                    writer.Write(nmsk);
                    uint lenColRotBuf = 0, lenColRotBufX = 0;
                    uint[]? colRotDef = null, colRotDefX = null;
                    UInt16[]? colRotBuf = null, colRotBufX = null;
                    if (isNewFormat)
                    {
                        writer.Write(nmskx);
                        (lenColRotBuf, colRotDef, colRotBuf) = PackColorRotations(nS.ColorRotations);
                        (lenColRotBufX, colRotDefX, colRotBufX) = PackColorRotations(nS.ColorRotationsX);
                        writer.Write(lenColRotBuf);
                        writer.Write(lenColRotBufX);
                    }
                    UInt16 nsmsk=0, nsmskx=0;
                    UInt16[]? pdsmaskIDs = null;
                    byte[]? pdsmasks = null, pdsmaskXs = null;
                    byte[]? pdscolv1s = null;
                    ushort[]? pdscols = null, pdscolXs = null;
                    if (nS.LengthHeader >= 18 * sizeof(uint))
                    {
                        (nsmsk, nsmskx, pdsmaskIDs, pdsmasks, pdsmaskXs, pdscolv1s, pdscols, pdscolXs) = ConvertDMasks(MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT, MAX_SPRITE_WIDTH, MAX_SPRITE_HEIGHT, nS.noColors, true, nS.DynaSpriteMasks, nS.DynaSpriteMasksX, new byte[0], nS.DynaSprite4Cols, nS.DynaSprite4ColsX);
                        writer.Write(nsmsk);
                        writer.Write(nsmskx);
                    }

                    BinaryExtensions.WriteArray(writer, nS.HashCode);
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.ShapeCompMode));
                    BinaryExtensions.WriteArray(writer, nS.CompMaskID);
                    BinaryExtensions.WriteArray(writer, ConvertByteToBit(nS.CompMasks));
                    if (!isNewFormat)
                    {
                        BinaryExtensions.WriteArray(writer, nS.Palettes);
                        BinaryExtensions.WriteArray(writer, nS.v1cFrames);
                    }
                    else
                    {
                        BinaryExtensions.WriteArray(writer, nS.cFrames);
                        BinaryExtensions.WriteArray(writer, frmidXs!);
                        BinaryExtensions.WriteArray(writer, frmXs!);
                    }
                    // pdmaskIDs contains the IDs of the dynamic masks (0xFFFF for empty masks)
                    // for Serum v2, 2 UInt16 per frame: first is the ID of the dynamic mask for low res
                    // second is the ID of the dynamic mask for high res
                    // for Serum v1, 1 UInt16 per frame: ID of the dynamic mask v1 for low res
                    BinaryExtensions.WriteArray(writer, pdmaskIDs);
                    BinaryExtensions.WriteArray(writer, pdmasks); // these mask buffers contain values, they can't be bit-compressed
                    if (!isNewFormat) BinaryExtensions.WriteArray(writer, pdcolv1s);
                    else
                    {
                        BinaryExtensions.WriteArray(writer, pdmaskXs);
                        BinaryExtensions.WriteArray(writer, pdcols);
                        BinaryExtensions.WriteArray(writer, pdcolXs);
                    }
                    BinaryExtensions.WriteArray(writer, nS.FrameSprites);
                    if (!isNewFormat) BinaryExtensions.WriteArray(writer, nS.v1SpriteDescription);
                    else
                    {
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
                                        if (!isNewFormat)
                                            BinaryExtensions.WriteArray(writer, nS.v1BackgroundFrames);
                                        else
                                        {
                                            BinaryExtensions.WriteArray(writer, nS.BackgroundFrames);
                                            BinaryExtensions.WriteArray(writer, BGidXs!);
                                            BinaryExtensions.WriteArray(writer, BGXs!);
                                        }
                                        BinaryExtensions.WriteArray(writer, nS.BackgroundID);
                                        if (!isNewFormat) BinaryExtensions.WriteArray(writer, nS.v1BackgroundBB);
                                        else
                                        {
                                            BinaryExtensions.WriteArray(writer, BGMId!);
                                            BinaryExtensions.WriteArray(writer, BGMBuf!);
                                            BinaryExtensions.WriteArray(writer, BGXMId!);
                                            BinaryExtensions.WriteArray(writer, BGXMBuf!);
                                        }
                                        if (nS.LengthHeader >= 15 * sizeof(uint))
                                        {
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsDirO));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsColO));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsDirX));
                                            BinaryExtensions.WriteArray(writer, PackDynaShadows(pdmaskIDs, nS.DynaShadowsColX));
                                            if (nS.LengthHeader >= 18 * sizeof(uint))
                                            {
                                                BinaryExtensions.WriteArray(writer, pdsmaskIDs!);
                                                BinaryExtensions.WriteArray(writer, pdsmasks!);
                                                BinaryExtensions.WriteArray(writer, pdscols!);
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
                    frm.tLog.Text += "File written successfully." + Environment.NewLine;
                    frm.tLog.Text += "Size of the µSerum is:" + stream.Length.ToString();
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
                    for (int j = 0; j < MAX_DYNA_SETS_PER_FRAMEN; j++)
                    {
                        dsbuf.Add(DSElt[i * MAX_DYNA_SETS_PER_FRAMEN + j]);
                    }
                }
            }
            return dsbuf.ToArray();
        }
        /// <summary>
        /// pack an array of bytes containing only 0 and 1 into an array of bytes where each bit represents a value from the input array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] ConvertByteToBit(byte[] input)
        {
            int outputLength = input.Length / 8;
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
        (UInt16,UInt16, UInt16[], byte[], byte[], byte[], ushort[], ushort[]) ConvertDMasks(uint width, uint height, uint widthx, uint heightx, uint nocolors, bool isv2, byte[] DynaMasks, byte[] DynaMasksX, byte[] v1Dyna4Cols, ushort[] Dyna4Cols, ushort[] Dyna4ColsX)
        {
            List<UInt16> pdmaskIDs = new List<UInt16>();
            List<byte> pdmasks = new List<byte>();
            List<byte> pdmaskXs = new List<byte>();
            List<byte> pdcolv1s = new List<byte>();
            List<ushort> pdcols = new List<ushort>();
            List<ushort> pdcolXs = new List<ushort>();
            uint posmask = 0, posmaskX=0;
            uint poscolv1 = 0, poscol = 0;
            ushort acmsk = 0, acmskx = 0;
            while (posmask <= DynaMasks.Length - width * height)
            {
                bool isempty = true;
                byte bitmask = 0x80;
                byte valb = 0;
                for (int i = 0; i < width * height; i++)
                {
                    if (DynaMasks[posmask + i] > 0)
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
                posmask+= width * height;
                if (isempty)
                {
                    pdmaskIDs.Add(0xFFFF);
                    pdmasks.RemoveRange(pdmasks.Count - (int)(width * height / 8), (int)(width * height / 8));
                }
                else
                {
                    pdmaskIDs.Add(acmsk);
                    if (!isv2)
                    {
                        pdcolv1s.AddRange(new ArraySegment<byte>(v1Dyna4Cols, (int)poscolv1, (int)nocolors * 16));
                    }
                    else
                    {
                        pdcols.AddRange(new ArraySegment<ushort>(Dyna4Cols, (int)poscol, (int)nocolors * MAX_DYNA_SETS_PER_FRAMEN));
                    }
                    acmsk++;
                }
                poscolv1 += nocolors * 16;
                if (isv2)
                {
                    isempty = true;
                    bitmask = 0x80;
                    valb = 0;
                    for (int i = 0; i < widthx * heightx; i++)
                    {
                        if (DynaMasksX[posmaskX + i] > 0)
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
                    posmaskX += widthx * heightx;
                    if (isempty)
                    {
                        pdmaskIDs.Add(0xFFFF);
                        pdmaskXs.RemoveRange(pdmaskXs.Count - (int)(widthx * heightx / 8), (int)(widthx * heightx / 8));
                    }
                    else
                    {
                        pdmaskIDs.Add(acmskx);
                        pdcolXs.AddRange(new ArraySegment<ushort>(Dyna4ColsX, (int)poscol, (int)nocolors * MAX_DYNA_SETS_PER_FRAMEN));
                        acmskx++;
                    }
                    poscol += nocolors * MAX_DYNA_SETS_PER_FRAMEN;
                }
            }
            return (acmsk, acmskx, pdmaskIDs.ToArray(), pdmasks.ToArray(), pdmaskXs.ToArray(), pdcolv1s.ToArray(), pdcols.ToArray(), pdcolXs.ToArray());
        }
        /// <summary>
        /// Pack thr color rotations into a more compact form
        /// </summary>
        /// <param name="CR"></param>
        /// <returns>first int is the length of the definition buffer
        /// second int is the length of the content buffer
        /// third is the definition buffer MAX_COLOR_ROTATIONN entries per frame, an index per rotation in the content buffer)
        /// fourth is the content buffer with all the rotations in a row, the first value per rotation is the length in colors, the second is the delay between 2 shifts and after are the colors to rotate</returns>
        (uint, uint[], UInt16[]) PackColorRotations(UInt16[] CR)
        {
            List<uint> def = new List<uint>();
            List<UInt16> buf = new List<UInt16>();
            int acpos = 0;
            while (acpos<=CR.Length- MAX_COLOR_ROTATIONN*MAX_LENGTH_COLOR_ROTATION)
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
