using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LNGTextEditor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args[0].Contains(".lng"))
            {
                Extract(args[0]);
            }
            else
            {
                Rebuild(args[0]);
            }
        }
        public static void Extract(string lng)
        {
            var reader = new BinaryReader(File.OpenRead(lng));
            int blockCount = reader.ReadUInt16();
            int[] blocks = new int[blockCount];
            reader.ReadInt32();
            var dir = Path.GetFileNameWithoutExtension(lng) + "\\";
            Directory.CreateDirectory(Path.GetFileNameWithoutExtension(lng));
            for (int i = 0; i < blockCount; i++)
            {
                blocks[i] = reader.ReadInt32();
            }
            for (int i = 0; i < blockCount; i++)
            {
                reader.BaseStream.Position = blocks[i];
                reader.ReadByte();
                int count = reader.ReadInt32();
                int[] strLen = new int[count];
                string[] strings = new string[count];
                for (int s = 0; s < count; s++)
                {
                    strLen[s] = reader.ReadInt32();
                }
                for (int s = 0; s < count; s++)
                {
                    if (s == 0)
                    {
                        strings[s] = Encoding.UTF8.GetString(reader.ReadBytes(strLen[s]));
                    }
                    else
                    {
                        strings[s] = Encoding.UTF8.GetString(reader.ReadBytes(strLen[s] - strLen[s - 1])).Replace("\n", "<lf>").Replace("\r", "<br>");
                    }
                }
                File.WriteAllLines(dir + i + ".txt", strings);
            }
        }
        public static void Rebuild(string inputDir)
        {
            var writer = new BinaryWriter(File.Create(inputDir + ".lng"));
            string[] files = Directory.GetFiles(inputDir);
            int[] blockOff = new int[files.Length];
            writer.Write((short)files.Length);
            byte[] bytes = { 0x01, 0x00, 0x00, 0x00 };
            writer.Write(bytes, 0, bytes.Length);
            writer.Write(new byte[4 * (files.Length + 1)]);
            for (int s = 0; s < files.Length; s++)
            {
                blockOff[s] = (int)writer.BaseStream.Position;
                string[] strings = File.ReadAllLines(files[s]);
                writer.Write((byte)0x3);
                writer.Write(strings.Length);
                int[] size = new int[strings.Length];
                for (int i = 0; i < strings.Length; i++)
                {
                    strings[i] = strings[i].Replace("<lf>", "\n").Replace("<br>", "\r");
                    if (i == 0)
                    {
                        size[i] = strings[i].Length;
                        writer.Write(size[i]);
                    }
                    else
                    {
                        size[i] = strings[i].Length;
                        writer.Write(size.Sum());
                    }
                }
                for (int i = 0; i < strings.Length; i++)
                {
                    writer.Write(Encoding.UTF8.GetBytes(strings[i]));
                }
            }
            writer.BaseStream.Position = 0x06;
            for (int i = 0; i < files.Length; i++)
            {
                writer.Write(blockOff[i]);
            }
            writer.Write((int)writer.BaseStream.Length);
        }
    }
}
