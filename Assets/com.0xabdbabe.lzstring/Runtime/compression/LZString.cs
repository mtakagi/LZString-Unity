using System;
using System.Collections.Generic;
using System.Linq;

namespace Compression
{
    public static class LZString
    {
        private const string Base64StringKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        private const string UriSafeStringKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-$";

        private static readonly Dictionary<string, Dictionary<char, int>> BaseReserveDict = new Dictionary<string, Dictionary<char, int>>();

        private static int GetBaseValue(string alphabet, char c)
        {
            if (BaseReserveDict.TryGetValue(alphabet, out var charDict) && charDict.TryGetValue(c, out var intValue))
            {
                return intValue;
            }
            else
            {
                BaseReserveDict[alphabet] = new Dictionary<char, int>();

                foreach (var (value, index) in alphabet.Select((value, index) => (value, index)))
                {
                    BaseReserveDict[alphabet][value] = index;
                }

                return BaseReserveDict[alphabet][c];
            }
        }

        public static byte[] Compress(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new byte[]{ };
            }

            var result = _Compress<List<byte[]>, byte[]>(input, 16, i => new [] { (byte)(i % 256), (byte)(i >> 8) });

            return result.SelectMany(b => b).ToArray();
        }

        public static string CompressToUTF16(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            var result = _Compress<List<char>, char>(input, 15, i => Convert.ToChar(i + 32));

            return new string(result.ToArray()) + " ";
        }

        public static byte[] CompressToUInt8Array(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new byte[] { };
            }

            var compress = Compress(input);
            var array = new byte[compress.Length];

            for (var i = 0; i < compress.Length / 2; i++)
            {
                array[i * 2] = compress[i * 2 + 1];
                array[i * 2 + 1] = compress[i * 2];
            }

            return array;
        }

        public static string CompressToBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            var result = _Compress<List<char>, char>(input, 6, i => Base64StringKey[i]);
            var str = new string(result.ToArray());

            switch (str.Length % 4)
            {
                case 0:
                    return str;
                case 1:
                    return str + "===";
                case 2:
                    return str + "==";
                case 3:
                    return str + "=";
                default:
                    return "";
            }
        }

        public static string CompressToEncodedeUriComponent(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            var result = _Compress<List<char>, char>(input, 6, i => UriSafeStringKey[i]);
            return new string(result.ToArray());
        }

        private static T _Compress<T, TU>(string input, int bitPerChar, Func<int, TU> intToValue) where T : ICollection<TU>, new()
        {
            if (string.IsNullOrEmpty(input))
            {
                return default;
            }

            int value;
            var w = "";
            var enlargeIn = 2;
            var dictSize = 3;
            var numBits = 2;
            var context = (dict: new Dictionary<string, int>(), dictCreate: new Dictionary<string, bool>(), data: new T(), val: 0, position: 0);

            void Rotate1(ref (Dictionary<string, int> dict, Dictionary<string, bool> dictCreate, T data, int val, int position) c, int bits)
            {
                for (var i = 0; i < bits; i++)
                {
                    c.val <<= 1;


                    if (c.position == bitPerChar - 1)
                    {
                        c.position = 0;
                        c.data.Add(intToValue(c.val));
                        c.val = 0;
                    }
                    else
                    {
                        c.position += 1;
                    }
                }
            }

            void Rotate2(ref (Dictionary<string, int> dict, Dictionary<string, bool> dictCreate, T data, int val, int position) c, int bits)
            {
                for (var i = 0; i < bits; i++)
                {
                    c.val = (c.val << 1) | (value & 1);

                    if (c.position == bitPerChar - 1)
                    {
                        c.position = 0;
                        c.data.Add(intToValue(c.val));
                        c.val = 0;
                    }
                    else
                    {
                        c.position += 1;
                    }

                    value >>= 1;
                }
            }

            void Rotate3(ref (Dictionary<string, int> dict, Dictionary<string, bool> dictCreate, T data, int val, int position) c, int bits)
            {
                value = 1;

                for (var i = 0; i < bits; i++)
                {
                    c.val = (c.val << 1) | value;

                    if (c.position == bitPerChar - 1)
                    {
                        c.position = 0;
                        c.data.Add(intToValue(c.val));
                        c.val = 0;
                    }
                    else
                    {
                        c.position += 1;
                    }
                    value = 0;
                }
            }

            void Rotate4(ref (Dictionary<string, int> dict, Dictionary<string, bool> dictCreate, T data, int val, int position) c, int bits)
            {
                value = c.dict[w];

                for (var i = 0; i < bits; i++)
                {
                    c.val = (c.val << 1) | (value & 1);

                    if (c.position == bitPerChar - 1)
                    {
                        c.position = 0;
                        c.data.Add(intToValue(c.val));
                        c.val = 0;
                    }
                    else
                    {
                        c.position += 1;
                    }

                    value >>= 1;
                }
            }

            void Rotate5(ref (Dictionary<string, int> dict, Dictionary<string, bool> dictCreate, T data, int val, int position) c, int bits)
            {
                value = 2;

                for (var i = 0; i < bits; i++)
                {
                    c.val = (c.val << 1) | (value & 1);

                    if (c.position == bitPerChar - 1)
                    {
                        c.position = 0;
                        c.data.Add(intToValue(c.val));
                        c.val = 0;
                    }
                    else
                    {
                        c.position += 1;
                    }

                    value >>= 1;
                }
            }

            foreach (var c in input)
            {
                var s = c.ToString();

                if (!context.dict.ContainsKey(s))
                {
                    context.dict[s] = dictSize++;
                    context.dictCreate[s] = true;
                }

                var wc = w + s;

                if (context.dict.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {
                    if (context.dictCreate.ContainsKey(w))
                    {
                        if (w[0] < 256)
                        {
                            Rotate1(ref context, numBits);

                            value = w[0];
                            Rotate2(ref context, 8);
                        }
                        else
                        {
                            Rotate3(ref context, numBits);

                            value = w[0];

                            Rotate2(ref context, 16);
                        }

                        enlargeIn -= 1;


                        if (enlargeIn == 0)
                        {
                            enlargeIn = 2 << (numBits - 1);
                            numBits += 1;
                        }

                        context.dictCreate.Remove(w);
                    }
                    else
                    {
                        Rotate4(ref context, numBits);
                    }
                    enlargeIn -= 1;


                    if (enlargeIn == 0)
                    {
                        enlargeIn = 2 << (numBits - 1);
                        numBits += 1;
                    }
                    context.dict[wc] = dictSize++;
                    w = s;
                }
            }

            if (!string.IsNullOrEmpty(w))
            {
                if (context.dictCreate.ContainsKey(w))
                {
                    if (w[0] < 256)
                    {
                        Rotate1(ref context, numBits);

                        value = w[0];

                        Rotate2(ref context, 8);
                    }
                    else
                    {
                        Rotate3(ref context, numBits);

                        value = w[0];

                        Rotate2(ref context, 16);
                    }
                    
                    enlargeIn -= 1;

                    if (enlargeIn == 0)
                    {
                        enlargeIn = 2 << (numBits - 1);
                        numBits += 1;
                    }

                    context.dictCreate.Remove(w);
                } 
                else
                {
                    Rotate4(ref context, numBits);
                }
                
                enlargeIn -= 1;

                if (enlargeIn == 0)
                {
                    numBits += 1;
                }
            }
            
            Rotate5(ref context, numBits);

            while (true)
            {
                context.val <<= 1;

                if (context.position == bitPerChar - 1)
                {
                    context.data.Add(intToValue(context.val));
                    break;
                }
                else
                {
                    context.position++;
                }
            }

            return context.data;
        }

        public static string Decompress(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return "";
            }

            return _Decompress(input.Length / 2, 32768, i =>
            {
                var lower = input[i * 2];
                var higher = input[i * 2 + 1] * 256;

                return higher + lower;
            });
        }

        public static string DecompressFromUTF16(byte[] input)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (input.Length == 0)
            {
                return "";
            }

            return _Decompress(input.Length / 2, 16384, i =>
            {
                var lower = input[i * 2];
                var higher = input[i * 2 + 1] * 256;

                return higher + lower - 32;
            });
        }

        public static string DecompressFromBase64(string input)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            return _Decompress(input.Length, 32, i => GetBaseValue(Base64StringKey, input[i]));
        }

        public static string DecompressFromEncodedzUriComponent(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            input = input.Replace(" ", "+");

            return _Decompress(input.Length, 32, i => GetBaseValue(UriSafeStringKey, input[i]));
        }

        public static string DecompressFromUInt8Array(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return "";
            }

            return _Decompress(input.Length / 2, 32768, i =>
            {
                var lower = input[i * 2] * 256;
                var higher = (int)input[i * 2 + 1];

                return higher + lower;
            });
        }

        private static string _Decompress(int length, int resetValue, Func<int, int> nextValue)
        {
            var dict = new Dictionary<int, List<ushort>>()
            {
                { 0, new List<ushort>() { 0 } },
                { 1, new List<ushort>() { 1 } },
                { 2, new List<ushort>() { 2 } }
            };
            var enlargeIn = 4;
            var dictSize = 4;
            var numBits = 3;
            var c = 0;
            var entry = new List<ushort>();
            var result = new List<char>();
            var data = (value: nextValue(0), position: resetValue, index: 1);

            int Slide(ref (int value, int position, int index) slideData, int maxpower)
            {
                var bit = 0;
                var power = 1;



                while (power != maxpower)
                {
                    var resb = slideData.value & slideData.position;
                    slideData.position >>= 1;


                    if (slideData.position == 0)
                    {
                        slideData.position = resetValue;
                        slideData.value = nextValue(slideData.index);
                        slideData.index += 1;
                    }

                    bit |= (resb > 0 ? 1 : 0) * power;
                    power <<= 1;
                }

                return bit;
            }

            var bits = Slide(ref data, maxpower: 2 << 1);
            var next = bits;


            if (next == 0)
            {
                bits = Slide(ref data, maxpower: 2 << 7);
                c = bits;
            }
            else if (next == 1)
            {
                bits = Slide(ref data, maxpower: 2 << 15);
                c = bits;
            }
            else if (next == 2)
            {
                return "";
            }

            var w = new List<ushort>() { (ushort)c };
            dict[3] = w;
            result.AddRange(w.Select(Convert.ToChar));

            while (true)
            {
                if (data.index > length)
                {
                    return "";
                }

                bits = Slide(ref data, maxpower: 2 << (numBits - 1));
                c = bits;


                if (c == 0)
                {
                    bits = Slide(ref data, maxpower: 2 << 7);
                    dict[dictSize] = new List<UInt16>() { (UInt16)bits };
                    dictSize += 1;
                    c = dictSize - 1;
                    enlargeIn -= 1;
                }
                else if (c == 1)
                {
                    bits = Slide(ref data, maxpower: 2 << 15);
                    dict[dictSize] = new List<UInt16>() { (UInt16)bits };
                    dictSize += 1;
                    c = dictSize - 1;
                    enlargeIn -= 1;
                }
                else if (c == 2)
                {
                    return new string(result.ToArray());
                }

                if (enlargeIn == 0)
                {
                    enlargeIn = 2 << (numBits - 1);
                    numBits += 1;
                }

                if (dict.TryGetValue(c, out var e))
                {
                    entry = e;
                }
                else
                {
                    if (c == dictSize)
                    {
                        var temp = new List<ushort>(w) {entry[0]};
                        entry = temp;
                    }
                    else
                    {
                        return "";
                    }
                }

                result.AddRange(entry.Select(Convert.ToChar));
                var tmp = new List<ushort>(w) {entry[0]};
                dict[dictSize] = tmp;
                dictSize += 1;
                enlargeIn -= 1;
                w = entry;


                if (enlargeIn == 0)
                {
                    enlargeIn = 2 << (numBits - 1);
                    numBits += 1;
                }
            }
        }
    }
}
