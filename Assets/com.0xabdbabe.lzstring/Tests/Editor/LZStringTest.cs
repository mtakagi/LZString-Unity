using System.Text;
using Compression;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class LZStringTest
    {
        private const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        [Test]
        public void LZStringDecompression()
        {
            var compress = Resources.Load<TextAsset>("compress");
            var actual = LZString.Decompress(compress.bytes);

            Assert.AreEqual(Text, actual);
        }

        [Test]
        public void LZStringUTF16Decompression()
        {
            var compress = Resources.Load<TextAsset>("compress_utf16");
            var actual = LZString.DecompressFromUTF16(compress.bytes);

            Assert.AreEqual(Text, actual);
        }

        [Test]
        public void LZStringBase64Decompression()
        {
            var compress = Resources.Load<TextAsset>("compress_base64");
            var actual = LZString.DecompressFromBase64(compress.text);

            Assert.AreEqual(Text, actual);
        }

        [Test]
        public void LZStringURiDecompression()
        {
            var compress = Resources.Load<TextAsset>("compress_uri");
            var actual = LZString.DecompressFromEncodedzUriComponent(compress.text);

            Assert.AreEqual(Text, actual);
        }

        [Test]
        public void LZStringUInt8Decompression()
        {
            var compress = Resources.Load<TextAsset>("compress_uint8");
            var actual = LZString.DecompressFromUInt8Array(compress.bytes);

            Assert.AreEqual(Text, actual);
        }

        [Test]
        public void LZStringCompression()
        {
            var actual = LZString.Compress(Text);
            var expect = Resources.Load<TextAsset>("compress");

            Assert.AreEqual(expect.bytes, actual);
        }

        [Test]
        public void LZStringUTF16Compression()
        {
            var actual = LZString.CompressToUTF16(Text);
            var expect = Resources.Load<TextAsset>("compress_utf16");
            var bytes = Encoding.Unicode.GetBytes(actual);

            Assert.AreEqual(expect.bytes, bytes);
        }

        [Test]
        public void LZStringBase64Compression()
        {
            var actual = LZString.CompressToBase64(Text);
            var expect = Resources.Load<TextAsset>("compress_base64");

            Assert.AreEqual(expect.text, actual);
        }

        [Test]
        public void LZStringURiCompression()
        {
            var actual = LZString.CompressToEncodedeUriComponent(Text);
            var expect = Resources.Load<TextAsset>("compress_uri");

            Assert.AreEqual(expect.text, actual);
        }

        [Test]
        public void LZStringUInt8Compression()
        {
            var actual = LZString.CompressToUInt8Array(Text);
            var expect = Resources.Load<TextAsset>("compress_uint8");

            Assert.AreEqual(expect.bytes, actual);
        }

        [Test]
        public void LZStringEmojiCompression()
        {
            var actual = LZString.Compress("🥺");

            Assert.AreEqual(new byte[] { 0x06, 0x9f, 0xf5, 0xe2, 0x00, 0xda }, actual);
        }


        [Test]
        public void LZStringEmojiDecompression()
        {
            var actual = LZString.Decompress(new byte[] { 0x06, 0x9f, 0xf5, 0xe2, 0x00, 0xda });

            Assert.AreEqual("🥺", actual);
        }

        [Test]
        public void LZStringEmojiBase64Compression()
        {
            var actual = LZString.CompressToBase64("🥺");

            Assert.AreEqual("nwbi9do=", actual);
        }

        [Test]
        public void LZStringEmojiBase64Decompression()
        {
            var actual = LZString.DecompressFromBase64("nwbi9do=");

            Assert.AreEqual("🥺", actual);
        }

    }
}
