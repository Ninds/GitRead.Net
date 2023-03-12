﻿using System;
using System.IO;
using System.Linq;

namespace GitRead.Net
{
    internal class PackIndexReader
    {
        private readonly string repoPath;

        internal PackIndexReader(string repoPath)
        {
            this.repoPath = repoPath;
        }

        internal long ReadIndex(string name, string hash)
        {
            byte[] fourByteBuffer = new byte[4];
            using (FileStream fileStream = File.OpenRead(Path.Combine(repoPath, "objects", "pack", name + ".idx")))
            {
                byte[] buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);
                if (buffer[0] != 255 || buffer[1] != 116 || buffer[2] != 79 || buffer[3] != 99)
                {
                    throw new Exception("Invalid index file");
                }
                fileStream.Read(buffer, 0, 4);
                if (buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0 || buffer[3] != 2)
                {
                    throw new Exception("Invalid index file version");
                }
                int fanoutIndex = Convert.ToInt32(hash.Substring(0, 2), 16); //Gives us a number between 0 and 255
                int numberOfHashesToSkip;
                if (fanoutIndex == 0)
                {
                    numberOfHashesToSkip = 0;
                }
                else
                {
                    fileStream.Seek((fanoutIndex - 1) * 4, SeekOrigin.Current);
                    fileStream.Read(buffer, 0, 4);
                    Array.Reverse(buffer);
                    numberOfHashesToSkip = BitConverter.ToInt32(buffer, 0);
                }
                int endIndex = ReadInt32(fileStream);
                Span<byte> hashBytes = stackalloc byte[20];
                HexStringToBytes(hash, hashBytes);
                int indexForHash = BinarySearch(fileStream, hashBytes, numberOfHashesToSkip, endIndex);
                if (indexForHash == -1)
                {
                    return -1;
                }
                int lastFanoutPos = 4 + 4 + (255 * 4);
                int totalNumberOfHashes = ReadInt32(fileStream, lastFanoutPos);
                int indexInto4ByteOffsets = lastFanoutPos + 4 + (20 * totalNumberOfHashes) + (4 * totalNumberOfHashes) + (4 * indexForHash);
                fileStream.Seek(indexInto4ByteOffsets, SeekOrigin.Begin);
                fileStream.Read(fourByteBuffer, 0, 4);
                bool use8ByteOffsets = (fourByteBuffer[0] & 0b1000_0000) != 0;
                long offset;
                if (!use8ByteOffsets)
                {
                    Array.Reverse(fourByteBuffer);
                    offset = BitConverter.ToInt32(fourByteBuffer, 0);
                }
                else
                {
                    fourByteBuffer[3] = (byte)(fourByteBuffer[3] & 0b0111_1111);
                    Array.Reverse(fourByteBuffer);
                    int indexInto8ByteOffsets = BitConverter.ToInt32(fourByteBuffer, 0);
                    indexInto4ByteOffsets = lastFanoutPos + 4 + (20 * totalNumberOfHashes) + (4 * totalNumberOfHashes) + (4 * totalNumberOfHashes) + (4 * indexInto8ByteOffsets);
                    offset = ReadInt64(fileStream, indexInto4ByteOffsets);
                }
                return offset;
            }
        }

        private int ReadInt32(FileStream fileStream, int pos = -1)
        {
            Span<byte> fourByteBuffer = stackalloc byte[4];
            if (pos != -1)
            {
                fileStream.Seek(pos, SeekOrigin.Begin);
            }
            fileStream.Read(fourByteBuffer);
            MemoryExtensions.Reverse(fourByteBuffer);
            return BitConverter.ToInt32(fourByteBuffer);
        }

        private long ReadInt64(FileStream fileStream, int pos)
        {
            Span<byte> eightByteBuffer = stackalloc byte[8];
            fileStream.Seek(pos, SeekOrigin.Begin);
            fileStream.Read(eightByteBuffer);
            MemoryExtensions.Reverse(eightByteBuffer);
            return BitConverter.ToInt64(eightByteBuffer);
        }

        private void HexStringToBytes(string str, Span<byte> res)
        {
            for (int i = 0; i < res.Length; i++)
            {
                int ch_u = str[i<<1];
                ch_u = (ch_u < 58 ? ch_u-48 : ( ch_u < 96 ? ch_u-55 : ch_u-87 ));
                int ch_l = str[(i<<1)+1];
                ch_l = (ch_l < 58 ? ch_l - 48 : (ch_l < 96 ? ch_l - 55 : ch_l - 87));
                res[i] = (byte)((ch_u << 4) + ch_l);

            }
           
        }

        private int BinarySearch(FileStream fileStream, ReadOnlySpan<byte> hash, int startIndex, int endIndex)
        {
            Span<byte> buffer = stackalloc byte[20];
            int startOfHashes = 4 + 4 + (256 * 4);
            int toCheck = startIndex + 1 == endIndex ? startIndex : (startIndex + endIndex + 1) / 2;
            fileStream.Seek(startOfHashes + (toCheck * 20), SeekOrigin.Begin);
            fileStream.Read(buffer);
            ComparisonResult comparison = Compare(hash, buffer);
            if (comparison == ComparisonResult.Equal)
            {
                return toCheck;
            }
            if (startIndex == endIndex)
            {
                return -1;
            }
            if (comparison == ComparisonResult.Less)
            {
                return BinarySearch(fileStream, hash, startIndex, toCheck - 1);
            }
            else
            {
                return BinarySearch(fileStream, hash, toCheck + 1, endIndex);
            }
        }

        private ComparisonResult Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] < b[i])
                {
                    return ComparisonResult.Less;
                }
                if (a[i] > b[i])
                {
                    return ComparisonResult.Greater;
                }
            }
            return ComparisonResult.Equal;
        }

        private enum ComparisonResult
        {
            Less,
            Equal,
            Greater
        }
    }
}