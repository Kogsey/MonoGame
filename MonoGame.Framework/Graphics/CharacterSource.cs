// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Original code from SilverSprite Project
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics 
{
    /// <summary>
    /// Union like structure used to combine behaviour for character sequences.
    /// </summary>
    public readonly ref struct CharacterSource 
    {
        private readonly int _offset;
        /// <inheritdoc cref="string.Length"/>
        public readonly int Length;
        private readonly int _type;

        //These 4 are at the end since they have an unknown length at compile time (for AnyCPU)
        private readonly string _string;
        private readonly StringBuilder _builder;
        private readonly ReadOnlySpan<char> _span;

        private CharacterSource(CharacterSource last, int offset, int length)
        {
            _string = last._string;
            _builder = last._builder;
            _span = last._span;
            _offset = offset;
            Length = length;
        }

        public CharacterSource(string s, int offset = 0, int length = -1)
        {
            _string = s;
            _offset = offset;
            Length = length < 0 ? (_string.Length - _offset) : length;
            _type = 0;
        }

        public CharacterSource(StringBuilder builder, int offset = 0, int length = -1)
        {
            _builder = builder;
            _offset = offset;
            Length = length < 0 ? (_builder.Length - _offset) : length;
            _type = 1;
        }
        public CharacterSource(ReadOnlySpan<char> span, int offset = 0, int length = -1)
        {
            _span = span;
            _offset = offset;
            Length = length < 0 ? (_span.Length - _offset) : length;
            _type = 2;
        }

        public static implicit operator CharacterSource(ReadOnlySpan<char> text)
            => new(text);
        public static implicit operator CharacterSource(StringBuilder text)
            => new(text);
        public static implicit operator CharacterSource(string text)
            => new(text);

        public readonly char this[int index] => _type switch
        {
            0 => _string[index + _offset],
            1 => _builder[index + _offset],
            2 => _span[index + _offset],
            _ => throw new NotImplementedException(),
        };

        public readonly CharacterSource this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length);
                offset += _offset;
                return new CharacterSource(this, offset, length);
            }
        }
    }
}
