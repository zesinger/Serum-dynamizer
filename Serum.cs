using System;
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

        private uint LengthHeader = 0; // file version

        public char[] name = new char[64]; // ROM name (no .zip, no path, example: afm_113b)
        public uint fWidth;  // Frame width=fW
        public uint fHeight; // Frame height=fH
        public uint fWidthX; // Frame width for extra frames=fWX
        public uint fHeightX;    // Frame height for extra frames=fHX
        public uint nFrames; // Number of frames=nF
        public uint noColors;    // Number of colors in palette of original ROM=noC
        public uint nCompMasks; // Number of dynamic masks=nM
        public uint nSprites; // Number of sprites=nS (max 255)
        public ushort nBackgrounds; // Number of background images=nB
        public int is256x64; // is the original resolution 256x64?
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

        public Serum(string filePath)
        {
            // Load the Serum file from the specified file path
            // Implement the loading logic here
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Read the header
                        name = reader.ReadChars(64);
                        LengthHeader = reader.ReadUInt32();
                        bool isNewFormat = (LengthHeader >= 14 * sizeof(uint));
                        fWidth = reader.ReadUInt32();
                        fHeight = reader.ReadUInt32();
                        if (isNewFormat)
                        {
                            fWidthX = reader.ReadUInt32();
                            fHeightX = reader.ReadUInt32();
                        }
                        else
                        {
                            if (fHeight == 64)
                            {
                                fWidthX = fWidth/2;
                                fHeightX = fHeight/2;
                            }
                            else
                            {
                                fWidthX = fWidth * 2;
                                fHeightX = fHeight * 2;
                            }
                        }
                        nFrames = reader.ReadUInt32();
                        noColors = reader.ReadUInt32();
                        if (!isNewFormat) reader.ReadUInt32(); // skip ncColors
                        nCompMasks = reader.ReadUInt32();
                        if (!isNewFormat) reader.ReadUInt32(); // skip nMovMasks
                        nSprites = reader.ReadUInt32();
                        if (LengthHeader >= 13 * sizeof(uint)) nBackgrounds = reader.ReadUInt16();
                        if (LengthHeader >= 20 * sizeof(uint)) is256x64 = reader.ReadInt32();
                        // Allocate memory for arrays based on the read values
                        if (is256x64 == 1) CompMasks = new byte[nCompMasks * 256 * 64];
                        else CompMasks = new byte[nCompMasks * fWidth * fHeight];
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
                        SpriteDetAreas = new ushort[nSprites * MAX_SPRITE_DETECT_AREAS];
                        SpriteDetDwords = new uint[nSprites * MAX_SPRITE_DETECT_AREAS];
                        SpriteDetDwordPos = new ushort[nSprites * MAX_SPRITE_DETECT_AREAS * 4];
                        TriggerID = new uint[nFrames];
                        if (nBackgrounds > 0)
                        {
                            isExtraBackground = new byte[nBackgrounds];
                            BackgroundFrames = new ushort[nBackgrounds * fWidth * fHeight];
                            BackgroundFramesX = new ushort[nBackgrounds * fWidthX * fHeightX];
                            BackgroundID = new ushort[nFrames];
                            BackgroundMask = new byte[nFrames * fWidth * fHeight];
                            BackgroundMaskX = new byte[nFrames * fWidthX * fHeightX];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while loading the Serum file: " + ex.Message);
            }
        }
    }

}
