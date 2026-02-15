using System;
using System.IO;
using System.Text;

namespace LyricsNotify.Helpers;

public static class AudioHelper
{
    public static TimeSpan GetDuration(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".wav" => GetWavDuration(stream),
            ".mp3" => GetMp3Duration(stream),
            _ => TimeSpan.Zero
        };
    }

    private static TimeSpan GetWavDuration(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, true);
        
        if (new string(reader.ReadChars(4)) != "RIFF")
            return TimeSpan.Zero;

        reader.ReadInt32(); // File size - 8

        if (new string(reader.ReadChars(4)) != "WAVE")
            return TimeSpan.Zero;

        int sampleRate = 0;
        int channels = 0;
        int bitsPerSample = 0;
        long dataLength = 0;

        while (stream.Position < stream.Length)
        {
            if (stream.Position + 8 > stream.Length) break;
            var identifier = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (identifier == "fmt ")
            {
                reader.ReadInt16(); // format tag
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // avg bytes per sec
                reader.ReadInt16(); // block align
                bitsPerSample = reader.ReadInt16();
                
                if (chunkSize > 16) stream.Seek(chunkSize - 16, SeekOrigin.Current);
            }
            else if (identifier == "data")
            {
                dataLength = chunkSize;
                break;
            }
            else
            {
                stream.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        if (sampleRate == 0 || channels == 0 || bitsPerSample == 0)
            return TimeSpan.Zero;

        double seconds = (double)dataLength / (sampleRate * channels * (bitsPerSample / 8.0));
        return TimeSpan.FromSeconds(seconds);
    }

    private static TimeSpan GetMp3Duration(Stream stream)
    {
        long tagSize = 0;
        byte[] buffer = new byte[10];
        // Check for ID3v2 tag
        if (stream.Read(buffer, 0, 10) == 10 && Encoding.ASCII.GetString(buffer, 0, 3) == "ID3")
        {
            tagSize = ((buffer[6] & 0x7F) << 21) | ((buffer[7] & 0x7F) << 14) | ((buffer[8] & 0x7F) << 7) | (buffer[9] & 0x7F);
            tagSize += 10;
            stream.Position = tagSize;
        }
        else
        {
            stream.Position = 0;
        }

        byte[] frameBuffer = new byte[4096];
        int bytesRead = stream.Read(frameBuffer, 0, frameBuffer.Length);
        int offset = FindFrameSync(frameBuffer, 0, bytesRead);
        
        if (offset == -1) return TimeSpan.Zero;

        var header = ParseMp3Header(frameBuffer, offset);
        int sideInfoSize = GetSideInfoSize(header);
        int xingOffset = offset + 4 + sideInfoSize;

        // Check for Xing/Info header (VBR)
        if (xingOffset + 8 < bytesRead)
        {
            string tag = Encoding.ASCII.GetString(frameBuffer, xingOffset, 4);
            if (tag == "Xing" || tag == "Info")
            {
                int flags = ReadBigEndianInt32(frameBuffer, xingOffset + 4);
                if ((flags & 0x01) != 0) // Frames field exists
                {
                    int totalFrames = ReadBigEndianInt32(frameBuffer, xingOffset + 8);
                    return TimeSpan.FromSeconds((double)totalFrames * header.SamplesPerFrame / header.SampleRate);
                }
            }
            
            // Check for VBRI header (VBR)
            int vbriOffset = offset + 32;
            if (vbriOffset + 26 < bytesRead && Encoding.ASCII.GetString(frameBuffer, vbriOffset, 4) == "VBRI")
            {
                int totalFrames = ReadBigEndianInt32(frameBuffer, vbriOffset + 14);
                return TimeSpan.FromSeconds((double)totalFrames * header.SamplesPerFrame / header.SampleRate);
            }
        }

        // Fallback for CBR: (FileSize - TagSize) / Average Bitrate
        if (header.Bitrate == 0) return TimeSpan.Zero;
        double seconds = (stream.Length - tagSize) * 8.0 / (header.Bitrate * 1000.0);
        return TimeSpan.FromSeconds(seconds);
    }

    private struct Mp3Header
    {
        public int Bitrate;
        public int SampleRate;
        public int SamplesPerFrame;
        public int Version; // 1=V1, 2=V2, 3=V2.5
        public int Layer;   // 1, 2, 3
        public int Channels;
    }

    private static int FindFrameSync(byte[] buffer, int start, int length)
    {
        for (int i = start; i < length - 1; i++)
        {
            if (buffer[i] == 0xFF && (buffer[i + 1] & 0xE0) == 0xE0) return i;
        }
        return -1;
    }

    private static Mp3Header ParseMp3Header(byte[] buffer, int offset)
    {
        int b1 = buffer[offset + 1];
        int b2 = buffer[offset + 2];
        int b3 = buffer[offset + 3];

        int versionIndex = (b1 >> 3) & 0x03;
        int layerIndex = (b1 >> 1) & 0x03;
        int bitrateIndex = (b2 >> 4) & 0x0F;
        int sampleRateIndex = (b2 >> 2) & 0x03;
        int channelMode = (b3 >> 6) & 0x03;

        int version = versionIndex switch { 0 => 3, 2 => 2, 3 => 1, _ => 1 };
        int layer = 4 - layerIndex;

        int[,] bitrates = {
            { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 }, // V1, L1
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },    // V1, L2
            { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },    // V1, L3
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 },   // V2, L1
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },        // V2, L2/L3
        };

        int brRow = (version == 1) ? (layer - 1) : (layer == 1 ? 3 : 4);
        int bitrate = bitrates[brRow, bitrateIndex];

        int[][] sampleRates = {
            new[] { 44100, 48000, 32000 }, // V1
            new[] { 22050, 24000, 16000 }, // V2
            new[] { 11025, 12000, 8000 }   // V2.5
        };
        int sr = sampleRates[version == 1 ? 0 : (version == 2 ? 1 : 2)][sampleRateIndex];
        int samples = (layer == 1) ? 384 : (layer == 2 ? 1152 : (version == 1 ? 1152 : 576));

        return new Mp3Header { Bitrate = bitrate, SampleRate = sr, SamplesPerFrame = samples, Version = version, Layer = layer, Channels = channelMode == 3 ? 1 : 2 };
    }

    private static int GetSideInfoSize(Mp3Header header)
    {
        if (header.Version == 1) return header.Channels == 1 ? 17 : 32;
        return header.Channels == 1 ? 9 : 17;
    }

    private static int ReadBigEndianInt32(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
    }
}
