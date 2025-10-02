using System;
using System.Numerics;

namespace Serum_dynamizer
{
    internal class Serum
    {
        char[] name = new char[64]; // ROM name (no .zip, no path, example: afm_113b)
        uint fWidth;  // Frame width=fW
        uint fHeight; // Frame height=fH
        uint fWidthX; // Frame width for extra frames=fWX
        uint fHeightX;    // Frame height for extra frames=fHX
        uint nFrames; // Number of frames=nF
        uint noColors;    // Number of colors in palette of original ROM=noC
                          //	uint		ncColors;	// Number of colors in palette of colorized ROM=nC
        uint nCompMasks; // Number of dynamic masks=nM
                         //uint		nMovMasks; // Number of moving rects=nMR
        uint nSprites; // Number of sprites=nS (max 255)
        ushort nBackgrounds; // Number of background images=nB
        bool is256x64; // is the original resolution 256x64?
                       // data
                       // part for comparison
        uint[] HashCode;   // uint[nF] hashcode/checksum
        byte[] ShapeCompMode;   // byte[nF] FALSE - full comparison (all 4 colors) TRUE - shape mode (we just compare black 0 against all the 3 other colors as if it was 1 color)
        byte[] CompMaskID;  // byte[nF] Comparison mask ID per frame (255 if no mask for this frame)
                            // HashCode take into account the ShapeCompMode parameter converting any '2' or '3' into a '1'
                            //byte*		MovRctID;	// byte[nF] Horizontal moving comparison rectangle ID per frame (255 if no rectangle for this frame)
        byte[] CompMasks;   // byte[nM*fW*fH] Mask for comparison
                            // byte[nM*256*64] if is256x64 is TRUE
                            //byte*		MovRcts; // byte[nMR*4] Rect for Moving Comparision rectangle [x,y,w,h]. The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
                            // part for colorization
                            //byte*		cPal;		// byte[3*nC*nF] Palette for each colorized frames
        byte[] isExtraFrame;    // byte[nF] is the extra frame available for that frame (1) or not (0)?
        ushort[] cFrames;    // unsigned short[nF*fW*fH] Colorized frames color indices, if this frame has sprites, it is the colorized frame of the static scene, with no sprite
        ushort[] cFramesX;   // unsigned short[nF*fWX*fHX] Colorized extra frames color indices, if this frame has sprites, it is the colorized frame of the static scene, with no sprite
        byte[] DynaMasks;   // byte[nF*fW*fH] Mask for dynamic content for each frame.  The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
        byte[] DynaMasksX;  // byte[nF*fWX*fHX] Mask for dynamic content for each extra frame.  The value (<MAX_DYNA_SETS_PER_FRAME) points to a sequence of 4/16 colors in Dyna4Cols. 255 means not a dynamic content.
        ushort[] Dyna4Cols;  // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN*noC] Color sets used to fill the dynamic content of frames
        ushort[] Dyna4ColsX;  // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN*noC] Color sets used to fill the dynamic content of extra frames
        byte[] FrameSprites; // byte[nF*MAX_SPRITES_PER_FRAME] Sprite numbers to look for in this frame max=MAX_SPRITES_PER_FRAME 255 if no sprite
        ushort[] FrameSpriteBB; // unsigned short[nF*MAX_SPRITES_PER_FRAME*4] The bounding boxes of the sprites described above [minx,miny,maxx,maxy]
                                //ushort[]		SpriteDescriptions; // unsigned short[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] Sprite drawing on 2 bytes per pixel:
                                // - the first is the 4-or-16-color sprite original drawing (255 means this is a transparent=ignored pixel) for Comparison step
                                // - the second is the 64-color sprite for Colorization step
        byte[] isExtraSprite;   // byte[nS] is the extra sprite available for that frame (1) or not (0)?
        byte[] SpriteOriginal; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] original aspect of each sprite (4-or-16-color drawing) =255 if this part is out of the sprite
        ushort[] SpriteColored; // unsigned short[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] Sprite drawing
        byte[] SpriteMaskX; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] equivalent to SpriteOriginal for extra resolution only with mask 255 if out of the sprite
        ushort[] SpriteColoredX; // unsigned short[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] Sprite extra resolution drawing
        ushort[] ColorRotations; // unsigned short[MAX_COLOR_ROTATION*MAX_LENGTH_COLOR_ROTATION*nF] MAX_COLOR_ROTATION color rotations per frame and the maximum number of colors in the rotation MAX_LENGTH_COLOR_ROTATION-2 (first value is the length, second value is the length in milliseconds between 2 shifts)
        ushort[] ColorRotationsX; // unsigned short[MAX_COLOR_ROTATION*MAX_LENGTH_COLOR_ROTATION*nF] MAX_COLOR_ROTATION color rotations per extra frame and the maximum number of colors in the rotation MAX_LENGTH_COLOR_ROTATION-2 (first value is the length, second value is the length in milliseconds between 2 shifts)
        ushort[] SpriteDetAreas; // unsigned short[nS*4*MAX_SPRITE_DETECT_AREAS] rectangles (left, top, width, height) as areas to detect sprites (left=0xffff -> no zone)
        uint[] SpriteDetDwords; // uint[nS*MAX_SPRITE_DETECT_AREAS] dword to quickly detect 4 consecutive distinctive pixels inside the original drawing of a sprite for optimized detection
        ushort[] SpriteDetDwordPos; // unsigned short[nS*MAX_SPRITE_DETECT_AREAS] offset of the above qword in the sprite description
        uint[] TriggerID; // uint[nF] does this frame triggers any event ID, 0xFFFFFFFF if not
        byte[] isExtraBackground;   // byte[nB] is the extra background available for that frame (1) or not (0)?
        ushort[] BackgroundFrames; // unsigned short[nB*fW*fH] Background frame images
        ushort[] BackgroundFramesX; // unsigned short[nB*fWX*fHX] Background extra frame images
        ushort[] BackgroundID; // unsigned short[nF] Indices of the backgrounds for each frame 0xffff if no background
                              //ushort[]		BackgroundBB; // unsigned short[4*nF] Bounding boxes of the backgrounds for each frame [minx,miny,maxx,maxy]
        byte[] BackgroundMask; // byte[nF*fW*fH] Mask to apply backgrounds for each frame (make BackgroundBB obsolete)
        byte[] BackgroundMaskX; // byte[nF*fWX*fHX] Mask to apply backgrounds for each extra frame (make BackgroundBB obsolete)
        byte[] DynaShadowsDirO; // byte[nF*MAX_DYNA_SETS_PER_FRAMEN] Flags giving the direction of the dynamic content shadows for original frame, can be OR-ed
                                // 0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
        ushort[] DynaShadowsColO; // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN] Color of the shadow for this dynamic set of the original frame
        byte[] DynaShadowsDirX; // byte[nF*MAX_DYNA_SETS_PER_FRAMEN] Flags giving the direction of the dynamic content shadows for the extra frame, can be OR-ed
                                // 0b1 - left, 0b10 - top left, 0b100 - top, 0b1000 - top right, 0b10000 - right, 0b100000 - bottom right, 0b1000000 - bottom, 0b10000000 - bottom left
        ushort[] DynaShadowsColX; // unsigned short[nF*MAX_DYNA_SETS_PER_FRAMEN] Color of the shadow for this dynamic set of the extra frame
        byte[] DynaSpriteMasks; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] is this sprite pixel dynamically colored (<255) or not (255)
        byte[] DynaSpriteMasksX; // byte[nS*MAX_SPRITE_WIDTH*MAX_SPRITE_HEIGHT] is this sprite pixel dynamically colored (<255) or not (255) for extra res sprites
        ushort[] DynaSprite4Cols; // unsigned short[nS*MAX_DYNA_SETS_PER_SPRITE*noC] color sets used to colorize the dynamic content of sprites
        ushort[] DynaSprite4ColsX; // unsigned short[nS*MAX_DYNA_SETS_PER_SPRITE*noC] color sets used to colorize the dynamic content of sprites for extra res sprites

        byte[] SpriteShapeMode; // byte[nS] is this sprite detected in shape mode?
    }


}
