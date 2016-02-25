using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GuidedMissile.GuidedMissileScript
{

    [StructLayout(LayoutKind.Explicit)]
    struct Int32Converter
    {
        [FieldOffset(0)]
        public int Value;
        [FieldOffset(0)]
        public byte Byte1;
        [FieldOffset(1)]
        public byte Byte2;
        [FieldOffset(2)]
        public byte Byte3;
        [FieldOffset(3)]
        public byte Byte4;

        public Int32Converter(int value)
        {
            Byte1 = Byte2 = Byte3 = Byte4 = 0;
            Value = value;
        }

        public Int32Converter(byte[] byteArray)
        {
            Value = 0;
            Byte1 = byteArray[0];
            Byte2 = byteArray[1];
            Byte3 = byteArray[2];
            Byte4 = byteArray[3];
        }
        public static implicit operator Int32(Int32Converter value)
        {
            return value.Value;
        }

        public static implicit operator Int32Converter(int value)
        {
            return new Int32Converter(value);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct UInt64Converter
    {
        [FieldOffset(0)]
        public ulong Value;
        [FieldOffset(0)]
        public byte Byte1;
        [FieldOffset(1)]
        public byte Byte2;
        [FieldOffset(2)]
        public byte Byte3;
        [FieldOffset(3)]
        public byte Byte4;
        [FieldOffset(4)]
        public byte Byte5;
        [FieldOffset(5)]
        public byte Byte6;
        [FieldOffset(6)]
        public byte Byte7;
        [FieldOffset(7)]
        public byte Byte8;

        public UInt64Converter(ulong value)
        {
            Byte1 = Byte2 = Byte3 = Byte4 = Byte5 = Byte6 = Byte7 = Byte8 = 0;
            Value = value;
        }
        public UInt64Converter(byte[] byteArray)
        {
            Value = 0;
            Byte1 = byteArray[0];
            Byte2 = byteArray[1];
            Byte3 = byteArray[2];
            Byte4 = byteArray[3];
            Byte5 = byteArray[4];
            Byte6 = byteArray[5];
            Byte7 = byteArray[6];
            Byte8 = byteArray[7];
        }
        public static implicit operator UInt64(UInt64Converter value)
        {
            return value.Value;
        }

        public static implicit operator UInt64Converter(ulong value)
        {
            return new UInt64Converter(value);
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    struct UInt16Converter
    {
        [FieldOffset(0)]
        public ushort Value;
        [FieldOffset(0)]
        public byte Byte1;
        [FieldOffset(1)]
        public byte Byte2;


        public UInt16Converter(ushort value)
        {
            Byte1 = Byte2 = 0;
            Value = value;
        }
        public UInt16Converter(byte[] byteArray)
        {
            Value = 0;
            Byte1 = byteArray[0];
            Byte2 = byteArray[1];
        }
        public static implicit operator UInt16(UInt16Converter value)
        {
            return value.Value;
        }

        public static implicit operator UInt16Converter(ushort value)
        {
            return new UInt16Converter(value);
        }
    }
}
