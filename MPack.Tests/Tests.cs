using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MPack.MPackTests
{
    [TestClass()]
    public class Tests
    {
        [TestMethod()]
        public void TestDouble()
        {
            double[] tests = new[]
            {
                0d,
                1d,
                -1d,
                224d,
                256d,
                65530d,
                65540d,
                double.NaN,
                double.MaxValue,
                double.MinValue,
                double.PositiveInfinity,
                double.NegativeInfinity
            };
            foreach (double value in tests)
            {
                Assert.AreEqual(value, MPack.ParseFromBytes(MPack.From(value).EncodeToBytes()).To<double>());
            }
        }

        [TestMethod()]
        public void TestNull()
        {
            Assert.IsNull(MPack.ParseFromBytes(MPack.Null().EncodeToBytes()).To<object>());
        }

        [TestMethod()]
        public void TestString()
        {
            string[] tests = new string[]
            {
                Helpers.GetString(2),
                Helpers.GetString(8),
                Helpers.GetString(16),
                Helpers.GetString(32),
                Helpers.GetString(257),
                Helpers.GetString(65537)
            };
            foreach (string value in tests)
            {
                Assert.AreEqual(value, MPack.ParseFromBytes(MPack.From(value).EncodeToBytes()).To<string>());
            }
        }

        [TestMethod()]
        public void TestInteger()
        {
            long[] tests = new[]
            {
                0,
                1,
                -1,
                sbyte.MinValue,
                sbyte.MaxValue,
                byte.MaxValue,
                short.MinValue,
                short.MaxValue,
                int.MinValue,
                int.MaxValue,
                long.MaxValue,
                long.MinValue,
            };
            foreach (long value in tests)
            {
                byte[] data = MPack.From(value).EncodeToBytes();
                long parsed = MPack.ParseFromBytes(data).To<long>();
                Assert.AreEqual(value, parsed, $"{value} is not equal {parsed}");
            }
        }

        [TestMethod()]
        public void TestMap()
        {
            MPackMap dictionary = new()
            {
                {
                    "array1",
                    MPack.From(new[]
                    {
                        MPack.From("array1_value1"),
                        MPack.From("array1_value2"),
                        MPack.From("array1_value3"),
                    })
                },
                { "bool1", MPack.From(true) },
                { "double1", MPack.From(50.5) },
                { "double2", MPack.From(15.2) },
                { "int1", MPack.From(50505) },
                { "int2", MPack.From(50) },
                { 3.14, MPack.From(3.14) },
                { 42, MPack.From(42) }
            };

            byte[] bytes = dictionary.EncodeToBytes();
            MPackMap result = MPack.ParseFromBytes(bytes) as MPackMap;
            CollectionAssert.AreEqual(dictionary.ToArray(), result.ToArray());
        }

        [TestMethod()]
        public void TestArray()
        {
            MPack[] tests = new[]
            {
                0,
                50505,
                float.NaN,
                float.MaxValue,
                float.MinValue,
                float.PositiveInfinity,
                float.NegativeInfinity,
                float.Epsilon,
            }.Select(f => MPack.From(f))
            .ToArray();

            MPackArray arr = new(tests);
            byte[] bytes = arr.EncodeToBytes();
            MPackArray round = MPack.ParseFromBytes(bytes) as MPackArray;

            Assert.IsNotNull(round);
            Assert.AreEqual(arr.Count, round.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                Assert.AreEqual(arr[i], round[i]);
            }
            Assert.AreEqual(arr, round);
        }

        [TestMethod()]
        public void TestUInt64()
        {
            ulong[] tests = new[]
            {
                ulong.MaxValue,
                ulong.MinValue,
            };
            foreach (ulong value in tests)
            {
                Assert.AreEqual(value, MPack.ParseFromBytes(MPack.From(value).EncodeToBytes()).To<ulong>());
            }
        }

        [TestMethod()]
        public void TestBoolean()
        {
            bool tru = MPack.ParseFromBytes(MPack.From(true).EncodeToBytes()).To<bool>();
            bool fal = MPack.ParseFromBytes(MPack.From(false).EncodeToBytes()).To<bool>();
            Assert.IsTrue(tru);
            Assert.IsFalse(fal);
        }

        [TestMethod()]
        public void TestSingle()
        {
            float[] tests = new[]
            {
                0,
                50505,
                float.NaN,
                float.MaxValue,
                float.MinValue,
                float.PositiveInfinity,
                float.NegativeInfinity,
                float.Epsilon,
            };
            foreach (float value in tests)
            {
                Assert.AreEqual(value, MPack.ParseFromBytes(MPack.From(value).EncodeToBytes()).To<float>());
            }
        }

        [TestMethod()]
        public void TestBinary()
        {
            byte[][] tests = new[]
            {
                Helpers.GetBytes(8),
                Helpers.GetBytes(16),
                Helpers.GetBytes(32),
                Helpers.GetBytes(257),
                Helpers.GetBytes(65537)
            };
            foreach (byte[] value in tests)
            {
                byte[] result = MPack.ParseFromBytes(MPack.From(value).EncodeToBytes()).To<byte[]>();
                Assert.IsTrue(Enumerable.SequenceEqual(value, result));
            }
        }

        [TestMethod]
        public void TestNumberToBytes()
        {
            CollectionAssert.AreEqual(MPack.From(127).EncodeToBytes(), new byte[] { 0b01111111 }); // 127
            CollectionAssert.AreEqual(MPack.From(0xFF).EncodeToBytes(), new byte[] { 0xCC, 0xFF }); // 255
            CollectionAssert.AreEqual(MPack.From(0x7FFF).EncodeToBytes(), new byte[] { 0xD1, 0x7F, 0xFF }); // 0x7FFF
            CollectionAssert.AreEqual(MPack.From((ushort)0x7FFF).EncodeToBytes(), new byte[] { 0xCD, 0x7F, 0xFF }); // (ushort)0x7FFF
            CollectionAssert.AreEqual(MPack.From(0xFFFF).EncodeToBytes(), new byte[] { 0xCD, 0xFF, 0xFF }); // 0xFFFF;
            CollectionAssert.AreEqual(MPack.From(0xFFFFFFFF).EncodeToBytes(), new byte[] { 0xCE, 0xFF, 0xFF, 0xFF, 0xFF }); // 0xFFFFFFFF
            CollectionAssert.AreEqual(MPack.From(0xF_FFFF_FFFF).EncodeToBytes(), new byte[] { 0xD3, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF }); // 0xF_FFFF_FFFF
            CollectionAssert.AreEqual(MPack.From((ulong)0xF_FFFF_FFFF).EncodeToBytes(), new byte[] { 0xCF, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF }); //(ulong)0xF_FFFF_FFFF
            CollectionAssert.AreEqual(MPack.From(0x8000_0000_FFFF_FFFF).EncodeToBytes(), new byte[] { 0xCF, 0x80, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }); // 0x8000_0000_FFFF_FFFF"

            CollectionAssert.AreEqual(MPack.From(-32).EncodeToBytes(), new byte[] { 0b11100000 }); // -32
            CollectionAssert.AreEqual(MPack.From(-33).EncodeToBytes(), new byte[] { 0xD0, 0xDF }); // -33
            CollectionAssert.AreEqual(MPack.From(-127).EncodeToBytes(), new byte[] { 0xD0, 0x81 }); // -127
            CollectionAssert.AreEqual(MPack.From(-128).EncodeToBytes(), new byte[] { 0xD1, 0xFF, 0x80 }); // -128
        }
    }
}