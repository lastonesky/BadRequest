using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Bad
{
    public class ChunkDecode
    {
        #region -- Private Fields --
        private IList<byte[]> _dataList = new List<byte[]>();
        #endregion

        #region -- Method --
        public ChunkDecode(byte[] data)
        {
            long pos = 0, start = 0;
            long Chunk_Size = 0;
            byte[] buff;
            while (pos < data.Length)
            {
                while (data[pos] != 13)
                {
                    if (pos < data.Length)
                    {
                        pos++;
                    }
                    else
                    {
                        pos = -1;
                    }
                }
                if (pos > -1)
                {
                    buff = new byte[pos - start];
                    Array.Copy(data, start, buff, 0, pos - start);
                    pos += 2;
                    Chunk_Size = HexStr2Lng(Encoding.UTF8.GetString(buff));
                    if (Chunk_Size > 0)
                    {
                        buff = new byte[Chunk_Size];
                        Array.Copy(data, pos, buff, 0, Chunk_Size);
                        _dataList.Add(buff);
                        pos += Chunk_Size + 2;
                        start = pos;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        public byte[] ToDecode()
        {
            long Count = _dataList.Sum(dat => dat.Length), start = 0;
            byte[] buff = new byte[Count];
            foreach (byte[] b in this._dataList)
            {
                Array.Copy(b, 0, buff, start, b.Length);
                start += b.Length;
            }
            return buff;
        }
        private long HexStr2Lng(string hexStr)
        {
            string tmp = "abcdef", u;
            long[] lngTmp = { 10, 11, 12, 13, 14, 15 };
            long result = 0, bn, pn;
            for (int i = hexStr.Length; i > 0; i--)
            {
                u = hexStr.Substring(i - 1, 1);
                if (!long.TryParse(u, out bn))
                {
                    if (tmp.IndexOf(u) > -1)
                    {
                        bn = lngTmp[tmp.IndexOf(u)];
                    }
                    else
                    {
                        throw new Exception("非十六进制字符");
                    }
                }
                pn = (long)Math.Pow(16, Math.Abs(i - hexStr.Length));
                result += bn * pn;
            }
            return result;
        }
        #endregion
    }
    public class GZip
    {
        public static byte[] GZipDecompress(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (GZipStream gZipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
                    {
                        byte[] bytes = new byte[40960];
                        int n;
                        while ((n = gZipStream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            stream.Write(bytes, 0, n);
                        }
                        gZipStream.Close();
                    }

                    return stream.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
        public static byte[] GZipCompress(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress))
                {
                    gZipStream.Write(data, 0, data.Length);
                    gZipStream.Close();
                }
                return stream.ToArray();
            }
        }
    }
}
