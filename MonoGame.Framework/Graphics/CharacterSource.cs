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
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public readonly ref struct CharacterSource 
    {
        [FieldOffset(0)]
        private readonly int _offset;
        /// <inheritdoc cref="string.Length"/>
        [FieldOffset(1)]
        public readonly int Length;
        [FieldOffset(2)]
        private readonly int _type;

        //These 4 are at the end since they have an unknown length at compile time (for AnyCPU)
        [FieldOffset(3)]
        private unsafe readonly nint _value;
        [FieldOffset(3)]
        private readonly string _string;
        [FieldOffset(3)]
        private readonly StringBuilder _builder;
        [FieldOffset(3)]
        private readonly ReadOnlySpan<char> _span;

        [SkipLocalsInit]
        private CharacterSource(nint value, int offset, int length)
        {
            _value = value;
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
                return new CharacterSource(_value, offset, length);
            }
        }
    }
}
