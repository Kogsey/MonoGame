// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Original code from SilverSprite Project
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Xna.Framework.Graphics 
{
    /// <summary>
    /// Represents a font texture.
    /// </summary>
    public sealed class SpriteFont 
    {
        internal static class Errors 
        {
            public const string TextContainsUnresolvableCharacters =
                "Text contains characters that cannot be resolved by this SpriteFont.";
            public const string UnresolvableCharacter =
                "Character cannot be resolved by this SpriteFont.";
        }

        private readonly Glyph[] _glyphs;
        private readonly CharacterRegion[] _regions;
        private char? _defaultCharacter;
        private int _defaultGlyphIndex = -1;
        
        private readonly Texture2D _texture;

        /// <summary>
        /// All the glyphs in this SpriteFont.
        /// </summary>
        public Glyph[] Glyphs { get { return _glyphs; } }

        class CharComparer : IEqualityComparer<char>
        {
            public bool Equals(char x, char y)
            {
                return x == y;
            }

            public int GetHashCode(char b)
            {
                return (b);
            }

            static public readonly CharComparer Default = new CharComparer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteFont" /> class.
        /// </summary>
        /// <param name="texture">The font texture.</param>
        /// <param name="glyphBounds">The rectangles in the font texture containing letters.</param>
        /// <param name="cropping">The cropping rectangles, which are applied to the corresponding glyphBounds to calculate the bounds of the actual character.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="lineSpacing">The line spacing (the distance from baseline to baseline) of the font.</param>
        /// <param name="spacing">The spacing (tracking) between characters in the font.</param>
        /// <param name="kerning">The letters kernings (X - left side bearing, Y - width and Z - right side bearing).</param>
        /// <param name="defaultCharacter">The character that will be substituted when a given character is not included in the font.</param>
        public SpriteFont(
            Texture2D texture, List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters,
            int lineSpacing, float spacing, List<Vector3> kerning, char? defaultCharacter)
        {
            Characters = new ReadOnlyCollection<char>(characters.ToArray());
            _texture = texture;
            LineSpacing = lineSpacing;
            Spacing = spacing;

            _glyphs = new Glyph[characters.Count];
            Stack<CharacterRegion> regions = new Stack<CharacterRegion>();

            for (int i = 0; i < characters.Count; i++) 
            {
                _glyphs[i] = new Glyph 
                {
                    BoundsInTexture = glyphBounds[i],
                    Cropping = cropping[i],
                    Character = characters[i],

                    LeftSideBearing = kerning[i].X,
                    Width = kerning[i].Y,
                    RightSideBearing = kerning[i].Z,

                    WidthIncludingBearings = kerning[i].X + kerning[i].Y + kerning[i].Z
                };
                
                if(regions.Count == 0 || characters[i] > (regions.Peek().End+1))
                {
                    // Start a new region
                    regions.Push(new CharacterRegion(characters[i], i));
                } 
                else if(characters[i] == (regions.Peek().End+1))
                {
                    CharacterRegion currentRegion = regions.Pop();
                    // include character in currentRegion
                    currentRegion.End++;
                    regions.Push(currentRegion);
                }
                else // characters[i] < (regions.Peek().End+1)
                {
                    throw new InvalidOperationException("Invalid SpriteFont. Character map must be in ascending order.");
                }
            }

            _regions = regions.ToArray();
            Array.Reverse(_regions);

            DefaultCharacter = defaultCharacter;
        }

        /// <summary>
        /// Gets the texture that this SpriteFont draws from.
        /// </summary>
        /// <remarks>Can be used to implement custom rendering of a SpriteFont</remarks>
        public Texture2D Texture { get { return _texture; } }

        /// <summary>
        /// Returns a copy of the dictionary containing the glyphs in this SpriteFont.
        /// </summary>
        /// <returns>A new Dictionary containing all of the glyphs in this SpriteFont</returns>
        /// <remarks>Can be used to calculate character bounds when implementing custom SpriteFont rendering.</remarks>
        public Dictionary<char, Glyph> GetGlyphs()
        {
            Dictionary<char, Glyph> glyphsDictionary = new Dictionary<char, Glyph>(_glyphs.Length, CharComparer.Default);
            foreach(Glyph glyph in _glyphs)
                glyphsDictionary.Add(glyph.Character, glyph);
            return glyphsDictionary;
        }

        /// <summary>
        /// Gets a collection of the characters in the font.
        /// </summary>
        public ReadOnlyCollection<char> Characters { get; private set; }

        /// <summary>
        /// Gets or sets the character that will be substituted when a
        /// given character is not included in the font.
        /// </summary>
        public char? DefaultCharacter
        {
            get { return _defaultCharacter; }
            set
            {   
                // Get the default glyph index here once.
                if (value.HasValue)
                {
                    if(!TryGetGlyphIndex(value.Value, out _defaultGlyphIndex))
                        throw new ArgumentException(Errors.UnresolvableCharacter);
                }
                else
                    _defaultGlyphIndex = -1;

                _defaultCharacter = value;
            }
        }

        /// <summary>
        /// Gets or sets the line spacing (the distance from baseline
        /// to baseline) of the font.
        /// </summary>
        public int LineSpacing { get; set; }

        /// <summary>
        /// Gets or sets the spacing (tracking) between characters in
        /// the font.
        /// </summary>
        public float Spacing { get; set; }

        /// <summary>
        /// Returns the size of a string when rendered in this font.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="offset">The start index of the text to measure.</param>
        /// <param name="length">The start length of the text to measure.</param>
        /// <returns>The size, in pixels, of 'text' when rendered in
        /// this font.</returns>
        public Vector2 MeasureString(string text, int offset = 0, int length = -1)
        {
            CharacterSource source = new(text, offset, length);
            MeasureString(source, out Vector2 size);
            return size;
        }

        /// <summary>
        /// Returns the size of a string when rendered in this font.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="offset">The start index of the text to measure.</param>
        /// <param name="length">The start length of the text to measure.</param>
        /// <returns>The size, in pixels, of 'text' when rendered in
        /// this font.</returns>
        public Vector2 MeasureString(ReadOnlySpan<char> text, int offset = 0, int length = -1)
        {
            CharacterSource source = new(text, offset, length);
            MeasureString(source, out Vector2 size);
            return size;
        }

        /// <summary>
        /// Returns the size of the contents of a StringBuilder when
        /// rendered in this font.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="offset">The start index of the text to measure.</param>
        /// <param name="length">The start length of the text to measure.</param>
        /// <returns>The size, in pixels, of 'text' when rendered in
        /// this font.</returns>
        public Vector2 MeasureString(StringBuilder text, int offset = 0, int length = -1)
        {
            CharacterSource source = new(text, offset, length);
            MeasureString(source, out Vector2 size);
            return size;
        }

        /// <summary>
        /// Run repeatedly to measure the size of a string up to a character. Initial values values specified in docs for params.
        /// </summary>
        /// <param name="c"> The character to add the the running measure. </param>
        /// <param name="width"> The current with of the text. Initial = 0 </param>
        /// <param name="finalLineHeight"> The line height. Increases based on tall characters. Initial = <see cref="LineSpacing"/> </param>
        /// <param name="offset"> The current draw offset of the string. The X dimension offset is included in the width. Initial = <see cref="Vector2.Zero"/> </param>
        /// <param name="firstGlyphOfLine"> If the current character is the beginning of the string or after a newline. Initial = <see langword="true"/></param>
        /// <returns></returns>
        public Vector2 RunningMeasureString(char c, ref float width, ref float finalLineHeight, ref Vector2 offset, ref bool firstGlyphOfLine)
        {
            unsafe
            {
                fixed (Glyph* pGlyphs = Glyphs)
                {
                    MeasureChar(c, ref width, ref finalLineHeight, ref offset, ref firstGlyphOfLine, pGlyphs);
                }

                return new(width, offset.Y + finalLineHeight);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void MeasureChar(char c, ref float width, ref float finalLineHeight, ref Vector2 offset, ref bool firstGlyphOfLine, Glyph* pGlyphs)
        {
            if (c == '\r')
                return;

            if (c == '\n')
            {
                finalLineHeight = LineSpacing;

                offset.X = 0;
                offset.Y += LineSpacing;
                firstGlyphOfLine = true;
                return;
            }

            if (!TryGetGlyphIndexOrDefault(c, out int currentGlyphIndex))
                throw new ArgumentException(Errors.TextContainsUnresolvableCharacters, nameof(c));
            Debug.Assert(currentGlyphIndex >= 0 && currentGlyphIndex < Glyphs.Length, "currentGlyphIndex was outside the bounds of the array.");
            Glyph* pCurrentGlyph = pGlyphs + currentGlyphIndex;

            // The first character on a line might have a negative left side bearing.
            // In this scenario, SpriteBatch/SpriteFont normally offset the text to the right,
            //  so that text does not hang off the left side of its rectangle.
            if (firstGlyphOfLine)
            {
                offset.X = Math.Max(pCurrentGlyph->LeftSideBearing, 0);
                firstGlyphOfLine = false;
            }
            else
            {
                offset.X += Spacing + pCurrentGlyph->LeftSideBearing;
            }

            offset.X += pCurrentGlyph->Width;

            float proposedWidth = offset.X + Math.Max(pCurrentGlyph->RightSideBearing, 0);
            if (proposedWidth > width)
                width = proposedWidth;

            offset.X += pCurrentGlyph->RightSideBearing;

            if (pCurrentGlyph->Cropping.Height > finalLineHeight)
                finalLineHeight = pCurrentGlyph->Cropping.Height;
        }

        internal unsafe void MeasureString(CharacterSource text, out Vector2 size)
        {
            if (text.Length == 0)
            {
                size = Vector2.Zero;
                return;
            }

            float width = 0.0f;
            float finalLineHeight = (float)LineSpacing;

            Vector2 offset = Vector2.Zero;
            bool firstGlyphOfLine = true;

            fixed (Glyph* pGlyphs = Glyphs)
            {
                for (int i = 0; i < text.Length; ++i)
                {
                    MeasureChar(text[i], ref width, ref finalLineHeight, ref offset, ref firstGlyphOfLine, pGlyphs);
                }
            }

            size.X = width;
            size.Y = offset.Y + finalLineHeight;
        }
        
        internal unsafe bool TryGetGlyphIndex(char c, out int index)
        {
            fixed (CharacterRegion* pRegions = _regions)
            {
                // Get region Index 
                int regionIdx = -1;
                int l = 0;
                int r = _regions.Length - 1;
                while (l <= r)
                {
                    int m = (l + r) >> 1;                    
                    Debug.Assert(m >= 0 && m < _regions.Length, "Index was outside the bounds of the array.");
                    if (pRegions[m].End < c)
                    {
                        l = m + 1;
                    }
                    else if (pRegions[m].Start > c)
                    {
                        r = m - 1;
                    }
                    else
                    {
                        regionIdx = m;
                        break;
                    }
                }

                if (regionIdx == -1)
                {
                    index = -1;
                    return false;
                }

                index = pRegions[regionIdx].StartIndex + (c - pRegions[regionIdx].Start);
            }

            return true;
        }

        internal bool TryGetGlyphIndexOrDefault(char c, out int glyphIdx)
        {
            if (!TryGetGlyphIndex(c, out glyphIdx))
            {
                glyphIdx = _defaultGlyphIndex;
                return _defaultGlyphIndex == -1;
            }
            else
                return true;
        }

        /// <summary>
        /// Struct that defines the spacing, Kerning, and bounds of a character.
        /// </summary>
        /// <remarks>Provides the data necessary to implement custom SpriteFont rendering.</remarks>
        public struct Glyph

        {
            /// <summary>
            /// The char associated with this glyph.
            /// </summary>
            public char Character;
            /// <summary>
            /// Rectangle in the font texture where this letter exists.
            /// </summary>
            public Rectangle BoundsInTexture;
            /// <summary>
            /// Cropping applied to the BoundsInTexture to calculate the bounds of the actual character.
            /// </summary>
            public Rectangle Cropping;
            /// <summary>
            /// The amount of space between the left side of the character and its first pixel in the X dimension.
            /// </summary>
            public float LeftSideBearing;
            /// <summary>
            /// The amount of space between the right side of the character and its last pixel in the X dimension.
            /// </summary>
            public float RightSideBearing;
            /// <summary>
            /// Width of the character before kerning is applied. 
            /// </summary>
            public float Width;
            /// <summary>
            /// Width of the character before kerning is applied. 
            /// </summary>
            public float WidthIncludingBearings;

            /// <summary>
            /// Returns an empty glyph.
            /// </summary>
            public static readonly Glyph Empty = new();

            /// <summary>
            /// Returns a string representation of this <see cref="Glyph"/>.
            /// </summary>
            public override readonly string ToString()
                => "CharacterIndex=" + Character + ", Glyph=" + BoundsInTexture + ", Cropping=" + Cropping + ", Kerning=" + LeftSideBearing + "," + Width + "," + RightSideBearing;
        }

        private struct CharacterRegion(char start, int startIndex)
        {
            public char Start = start;
            public char End = start;
            public int StartIndex = startIndex;
        }
    }
}
