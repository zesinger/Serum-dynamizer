using Microsoft.VisualBasic;
using System;
using System.IO.Compression;
using System.Numerics;

namespace Serum_dynamizer
{
    internal class Serum
    {
        public const int MAX_DYNA_SETS_PER_FRAMEN = 32; // max number of dynamic sets per frame
        public const int MAX_SPRITES_PER_FRAME = 32;  // maximum amount of sprites to look for per frame
        public const int MAX_SPRITE_WIDTH = 256; // maximum width of the new sprites
        public const int MAX_SPRITE_HEIGHT = 64; // maximum height of the new sprites
        public const int MAX_COLOR_ROTATIONN = 4; // maximum number of new color rotations per frame
        public const int MAX_LENGTH_COLOR_ROTATION = 64; // maximum number of new colors in a rotation
        public const int MAX_SPRITE_DETECT_AREAS = 4;  // maximum number of areas to detect the sprite
        public const int MAX_DYNA_SETS_PER_SPRITE = 9;	// max number of color sets for dynamic content for each sprite (new version)

        public uint LengthHeader = 0; // file version
        public string FilePath = ""; // path of the loaded file
        public char[] name = new char[64]; // ROM name (no .zip, no path, example: afm_113b)
        public uint fWidth;  // Frame width=fW
        public uint fHeight; // Frame height=fH
        public uint fWidthX; // Frame width for extra frames=fWX
        public uint fHeightX;    // Frame height for extra frames=fHX
        public uint nFrames; // Number of frames=nF
        public uint noColors;    // Number of colors in palette of original ROM=noC
        public uint ncColors;    // Number of colors in colorization ROM=nC
        public uint nMovMasks; // Number of moving masks=nM (not used anymore)
        public uint nCompMasks; // Number of dynamic masks=nM
        public uint nSprites; // Number of sprites=nS (max 255)
        public ushort nBackgrounds; // Number of background images=nB
        public int is256x64 = 0; // is the original resolution 256x64?
                       // data
                       // part for comparison
        public uint[] HashCode;   // uint[nF] hashcode/checksum
        public byte[] ShapeCompMode;   // byte[nF] FALSE - full comparison (all 4 colors) TRUE - shape mode (we just compare black 0 against all the 3 other colors as if it was 1 color)
        public byte[] CompMaskID;  // byte[nF] Comparison mask ID per frame (255 if no mask for this frame)
                            // HashCode take into account the ShapeCompMode parameter converting any '2' or '3' into a '1'
                            //byte*		MovRctID;	// byte[nF] Horizontal moving comparison rectangle ID per frame (255 if no rectangle for this frame)
        public byte[] CompMasks;   // byte[nM*fW*fH] Mask for comparison
                            // byte[nM*256*64] if is256x64 is TRUE
                            //byte*		MovRcts; // byte[nMR*4] Rect for Moving Comparision rectangle [x,y,w,h]. The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
                            // part for colorization
                            //byte*		cPal;		// byte[3*nC*nF] Palette for each colorized frames
        public byte[] isExtraFrame;    // byte[nF] is the extra frame available for that frame (1) or not (0)?
        public ushort[] cFrames;    // unsigned short[nF*fW*fH] Colorized frames color indices, if this frame has sprites, it is the colorized frame of the static scene, with no sprite
        public ushort[] cFramesX;   // unsigned short[nF*fWX*fHX] Colorized extra frames color indices, if this frame has sprites, it is the colorized frame of the static scene, with no sprite
        public byte[] DynaMasks;   // byte[nF*fW*fH] Mask for dynamic content for each frame.  The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
        public byte[] DynaMasksX;  // byte[nF*fWX*fHX] Mask for dynamic content for each extra frame.  The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
        public ushort[] Dyna4Cols;  // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN*noC] Color sets used to fill the dynamic content of frames
        public ushort[] Dyna4ColsX;  // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN*noC] Color sets used to fill the dynamic content of extra frames
        public byte[] FrameSprites; // byte[nF*MAX_SPRITES_PER_FRAME] Sprite numbers to look for in this frame max=MAX_SPRITES_PER_FRAME 255 if no sprite
        public ushort[] FrameSpriteBB; // unsigned short[nF*MAX_SPRITES_PER_FRAME*4] The bounding boxes of the sprites described above [minx,miny,maxx,maxy]
        public byte[] isExtraSprite;   // byte[nS] is the extra sprite available for that frame (1) or not (0)?
        public byte[] SpriteOriginal; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] original aspect of each sprite (4-or-16-color drawing) =255 if this part is out of the sprite
        public ushort[] SpriteColored; // unsigned short[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] Sprite drawing
        public byte[] SpriteMaskX; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] equivalent to SpriteOriginal for extra resolution only with mask 255 if out of the sprite
        public ushort[] SpriteColoredX; // unsigned short[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] Sprite extra resolution drawing
                                           // - the first is the 4-or-16-color sprite original drawing (255 means this is a transparent=ignored pixel) for Comparison step
                                           // - the second is the 64-color sprite for Colorization step
        public ushort[] ColorRotations; // unsigned short[MAX_COLOR_ROTATION*MAX_LENGTH_COLOR_ROTATION*nF] MAX_COLOR_ROTATION color rotations per frame and the maximum number of colors in the rotation MAX_LENGTH_COLOR_ROTATION-2 (first value is the length, second value is the length in milliseconds between 2 shifts)
        public ushort[] ColorRotationsX; // unsigned short[MAX_COLOR_ROTATION*MAX_LENGTH_COLOR_ROTATION*nF] MAX_COLOR_ROTATION color rotations per extra frame and the maximum number of colors in the rotation MAX_LENGTH_COLOR_ROTATION-2 (first value is the length, second value is the length in milliseconds between 2 shifts)
        public ushort[] SpriteDetAreas; // unsigned short[nS*4*MAX_SPRITE_DETECT_AREAS] rectangles (left, top, width, height) as areas to detect sprites (left=0xffff -> no zone)
        public uint[] SpriteDetDwords; // uint[nS*MAX_SPRITE_DETECT_AREAS] dword to quickly detect 4 consecutive distinctive pixels inside the original drawing of a sprite for optimized detection
        public ushort[] SpriteDetDwordPos; // unsigned short[nS*MAX_SPRITE_DETECT_AREAS] offset of the above qword in the sprite description
        public uint[] TriggerID; // uint[nF] does this frame triggers any event ID, 0xFFFFFFFF if not
        public byte[] isExtraBackground;   // byte[nB] is the extra background available for that frame (1) or not (0)?
        public ushort[] BackgroundFrames; // unsigned short[nB*fW*fH] Background frame images
        public ushort[] BackgroundFramesX; // unsigned short[nB*fWX*fHX] Background extra frame images
        public ushort[] BackgroundID; // unsigned short[nF] Indices of the backgrounds for each frame 0xffff if no background
        public byte[] BackgroundMask; // byte[nF*fW*fH] Mask to apply backgrounds for each frame (make BackgroundBB obsolete)
        public byte[] BackgroundMaskX; // byte[nF*fWX*fHX] Mask to apply backgrounds for each extra frame (make BackgroundBB obsolete)
        public byte[] DynaShadowsDirO; // byte[nF*MAX_DYNA_SETS_PER_FRAMEN] Flags giving the direction of the dynamic content shadows for original frame, can be OR-ed
                                // 0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
        public ushort[] DynaShadowsColO; // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN] Color of the shadow for this dynamic set of the original frame
        public byte[] DynaShadowsDirX; // byte[nF*MAX_DYNA_SETS_PER_FRAMEN] Flags giving the direction of the dynamic content shadows for the extra frame, can be OR-ed
                                // 0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
        public ushort[] DynaShadowsColX; // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN] Color of the shadow for this dynamic set of the extra frame
        public byte[] DynaSpriteMasks; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] is this sprite pixel dynamically colored (<255) or not (255)
        public byte[] DynaSpriteMasksX; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] is this sprite pixel dynamically colored (<255) or not (255) for extra res sprites
        public ushort[] DynaSprite4Cols; // unsigned short[nS*MAX_DYNA_SETS_PER_SPRITE*noC] color sets used to colorize the dynamic content of sprites
        public ushort[] DynaSprite4ColsX; // unsigned short[nS*MAX_DYNA_SETS_PER_SPRITE*noC] color sets used to colorize the dynamic content of sprites for extra res sprites

        public byte[] SpriteShapeMode; // byte[nS] is this sprite detected in shape mode?

        private bool Crz_Uncompress(string sourcefilepath, string destdirectory)
        {
            try
            {
                string destfile = Path.Combine(Path.GetDirectoryName(sourcefilepath)!, Path.GetFileNameWithoutExtension(sourcefilepath) + ".crom");
                if (File.Exists(destfile))
                {
                    if (MessageBox.Show("The file " + destfile + " already exists. Do you want to overwrite it?", "File exists", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return false;
                    }
                }
                ZipFile.ExtractToDirectory(sourcefilepath, destdirectory, overwriteFiles: true);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public Serum(string filePath, Form1 frm)
        {
            FilePath = filePath;
            // Load the Serum file from the specified file path
            // Implement the loading logic here
            string filepath = filePath;
            if (Path.GetExtension(filePath).ToLower() == ".crz")
            {
                Crz_Uncompress(filePath, Path.GetDirectoryName(filePath));
                filepath = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath) + ".crom");
            }
            try
            {
                using (var stream = File.Open(filepath, FileMode.Open))
                {
                    frm.tLog.Text= "Loading " + Path.GetFileName(filepath) + Environment.NewLine;
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        frm.tLog.AppendText("Size: " + Form1.FormatSize(stream.Length) + Environment.NewLine);
                        // Read the header
                        name = reader.ReadChars(64);
                        frm.tLog.AppendText("  ROM name: " + new string(name).TrimEnd('\0') + Environment.NewLine);
                        LengthHeader = reader.ReadUInt32();
                        bool isNewFormat = (LengthHeader >= 14 * sizeof(uint));
                        if (!isNewFormat)
                        {
                            MessageBox.Show("This only works with Serum v2 files, sorry!");
                            Application.Exit();
                        }
                        frm.tLog.AppendText("  Serum v2 file" + Environment.NewLine);
                        fWidth = reader.ReadUInt32();
                        fHeight = reader.ReadUInt32();
                        fWidthX = reader.ReadUInt32();
                        fHeightX = reader.ReadUInt32();
                        frm.tLog.AppendText("  Frame low res size: " + fWidth + "x" + fHeight + Environment.NewLine);
                        frm.tLog.AppendText("  Frame high res size: " + fWidthX + "x" + fHeightX + Environment.NewLine);
                        nFrames = reader.ReadUInt32();
                        frm.tLog.AppendText("  Number of frames: " + nFrames + Environment.NewLine);
                        noColors = reader.ReadUInt32();
                        frm.tLog.AppendText("  Number of ROM colors: " + noColors + Environment.NewLine);
                        if (!isNewFormat) ncColors = reader.ReadUInt32();
                        nCompMasks = reader.ReadUInt32();
                        frm.tLog.AppendText("  Number of comparison masks: " + nCompMasks + Environment.NewLine);
                        if (!isNewFormat) nMovMasks = reader.ReadUInt32();
                        nSprites = reader.ReadUInt32();
                        frm.tLog.AppendText("  Number of sprites: " + nSprites + Environment.NewLine);
                        nBackgrounds = reader.ReadUInt16();
                        frm.tLog.AppendText("  Number of backgrounds: " + nBackgrounds + Environment.NewLine);
                        if (LengthHeader >= 20 * sizeof(uint)) is256x64 = reader.ReadInt32();
                        if (is256x64 > 0) frm.tLog.AppendText("  Original resolution is 256x64" + Environment.NewLine);
                        // Allocate memory for arrays based on the read values
                        if (is256x64 == 1) CompMasks = new byte[nCompMasks * 256 * 64];
                        else CompMasks = new byte[nCompMasks * fWidth * fHeight];
                        HashCode = new uint[nFrames];
                        ShapeCompMode = new byte[nFrames];
                        CompMaskID = new byte[nFrames];
                        isExtraFrame = new byte[nFrames];
                        cFrames = new ushort[nFrames * fWidth * fHeight];
                        cFramesX = new ushort[nFrames * fWidthX * fHeightX];
                        DynaMasks = new byte[nFrames * fWidth * fHeight];
                        DynaMasksX = new byte[nFrames * fWidthX * fHeightX];
                        Dyna4Cols = new ushort[nFrames * MAX_DYNA_SETS_PER_FRAMEN * noColors];
                        Dyna4ColsX = new ushort[nFrames * MAX_DYNA_SETS_PER_FRAMEN * noColors];
                        isExtraSprite = new byte[nSprites];
                        FrameSprites = new byte[nFrames * MAX_SPRITES_PER_FRAME];
                        FrameSpriteBB = new ushort[nFrames * MAX_SPRITES_PER_FRAME * 4];
                        SpriteOriginal = new byte[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        SpriteColored = new ushort[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        SpriteMaskX = new byte[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        SpriteColoredX = new ushort[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        ColorRotations = new ushort[MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION * nFrames];
                        ColorRotationsX = new ushort[MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION * nFrames];
                        SpriteDetAreas = new ushort[nSprites * MAX_SPRITE_DETECT_AREAS * 4];
                        SpriteDetDwords = new uint[nSprites * MAX_SPRITE_DETECT_AREAS];
                        SpriteDetDwordPos = new ushort[nSprites * MAX_SPRITE_DETECT_AREAS * 4];
                        TriggerID = new uint[nFrames];
                        isExtraBackground = new byte[nBackgrounds];
                        BackgroundFrames = new ushort[nBackgrounds * fWidth * fHeight];
                        BackgroundFramesX = new ushort[nBackgrounds * fWidthX * fHeightX];
                        BackgroundID = new ushort[nFrames];
                        BackgroundMask = new byte[nFrames * fWidth * fHeight];
                        BackgroundMaskX = new byte[nFrames * fWidthX * fHeightX];
                        DynaShadowsDirO = new byte[nFrames * MAX_DYNA_SETS_PER_FRAMEN];
                        DynaShadowsColO = new ushort[nFrames * MAX_DYNA_SETS_PER_FRAMEN];
                        DynaShadowsDirX = new byte[nFrames * MAX_DYNA_SETS_PER_FRAMEN];
                        DynaShadowsColX = new ushort[nFrames * MAX_DYNA_SETS_PER_FRAMEN];
                        DynaSprite4Cols= new ushort[nSprites * MAX_DYNA_SETS_PER_SPRITE * noColors];
                        DynaSprite4ColsX = new ushort[nSprites * MAX_DYNA_SETS_PER_SPRITE * noColors];
                        DynaSpriteMasks = new byte[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        DynaSpriteMasksX = new byte[nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT];
                        SpriteShapeMode = new byte[nSprites];
                        HashCode = BinaryExtensions.ReadArray<uint>(reader, nFrames);
                        ShapeCompMode = reader.ReadBytes((int)nFrames);
                        CompMaskID = reader.ReadBytes((int)nFrames);
                        if (is256x64 > 0) CompMasks = reader.ReadBytes((int)(nCompMasks * 256 * 64));
                        else CompMasks = reader.ReadBytes((int)(nCompMasks * fWidth * fHeight));
                        isExtraFrame = reader.ReadBytes((int)nFrames);
                        cFrames = BinaryExtensions.ReadArray<ushort>(reader, nFrames * fWidth * fHeight);
                        cFramesX = BinaryExtensions.ReadArray<ushort>(reader, nFrames * fWidthX * fHeightX);
                        DynaMasks = reader.ReadBytes((int)(nFrames * fWidth * fHeight));
                        DynaMasksX = reader.ReadBytes((int)(nFrames * fWidthX * fHeightX));
                        Dyna4Cols = BinaryExtensions.ReadArray<ushort>(reader, nFrames * MAX_DYNA_SETS_PER_FRAMEN * noColors);
                        Dyna4ColsX = BinaryExtensions.ReadArray<ushort>(reader, nFrames * MAX_DYNA_SETS_PER_FRAMEN * noColors);
                        isExtraSprite = reader.ReadBytes((int)nSprites);
                        FrameSprites = reader.ReadBytes((int)(nFrames * MAX_SPRITES_PER_FRAME));
                        SpriteOriginal = reader.ReadBytes((int)(nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT));
                        SpriteColored = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                        SpriteMaskX = reader.ReadBytes((int)(nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT));
                        SpriteColoredX = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT);
                        reader.ReadBytes((int)nFrames ); // Ignore the active frame content
                        ColorRotations = BinaryExtensions.ReadArray<ushort>(reader, MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION * nFrames);
                        ColorRotationsX = BinaryExtensions.ReadArray<ushort>(reader, MAX_COLOR_ROTATIONN * MAX_LENGTH_COLOR_ROTATION * nFrames);
                        SpriteDetDwords = BinaryExtensions.ReadArray<uint>(reader, nSprites * MAX_SPRITE_DETECT_AREAS);
                        SpriteDetDwordPos = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_SPRITE_DETECT_AREAS);
                        SpriteDetAreas = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_SPRITE_DETECT_AREAS * 4);
                        TriggerID = BinaryExtensions.ReadArray<uint>(reader, nFrames);
                        FrameSpriteBB = BinaryExtensions.ReadArray<ushort>(reader, nFrames * MAX_SPRITES_PER_FRAME * 4);
                        isExtraBackground = reader.ReadBytes((int)nBackgrounds);
                        BackgroundFrames = BinaryExtensions.ReadArray<ushort>(reader, nBackgrounds * fWidth * fHeight);
                        BackgroundFramesX = BinaryExtensions.ReadArray<ushort>(reader, nBackgrounds * fWidthX * fHeightX);
                        for (int i = 0; i < nFrames; i++) BackgroundID[i] = reader.ReadUInt16();
                            BackgroundMask = reader.ReadBytes((int)(nFrames * fWidth * fHeight));
                            BackgroundMaskX = reader.ReadBytes((int)(nFrames * fWidthX * fHeightX));
                        if (LengthHeader >= 15 * sizeof(uint))
                        {
                            DynaShadowsDirO = reader.ReadBytes((int)(nFrames * MAX_DYNA_SETS_PER_FRAMEN));
                            DynaShadowsColO = BinaryExtensions.ReadArray<ushort>(reader, nFrames * MAX_DYNA_SETS_PER_FRAMEN);
                            DynaShadowsDirX = reader.ReadBytes((int)(nFrames * MAX_DYNA_SETS_PER_FRAMEN));
                            DynaShadowsColX = BinaryExtensions.ReadArray<ushort>(reader, nFrames * MAX_DYNA_SETS_PER_FRAMEN);
                            if (LengthHeader >= 18 * sizeof(uint))
                            {
                                DynaSprite4Cols = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_DYNA_SETS_PER_SPRITE * noColors);
                                DynaSprite4ColsX = BinaryExtensions.ReadArray<ushort>(reader, nSprites * MAX_DYNA_SETS_PER_SPRITE * noColors);
                                DynaSpriteMasks = reader.ReadBytes((int)(nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT));
                                DynaSpriteMasksX = reader.ReadBytes((int)(nSprites * MAX_SPRITE_WIDTH * MAX_SPRITE_HEIGHT));
                                if (LengthHeader >= 19 * sizeof(uint))
                                {
                                    SpriteShapeMode = reader.ReadBytes((int)nSprites);
                                }
                            }
                        }
                        frm.tLog.AppendText("File loaded." + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                nFrames = 0;
                frm.tLog.AppendText("Error loading file: " + ex.Message + Environment.NewLine);
            }
        }
    }

}
