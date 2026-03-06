using System;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace XenominerExtractor
{
    public class HiddenGame : Game
    {
        public HiddenGame() { _ = new GraphicsDeviceManager(this); }
        public new void RunOneFrame() => base.RunOneFrame();
    }

    class Program
    {
        static string ContentPath = string.Empty;
        static string ExportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExtractedAssets");

        static void Main(string[] args)
        {
            Console.WriteLine("=== Xenominer Asset Extractor ===");

            // 1. Interactive Path Selection
            while (true)
            {
                Console.Write("Enter the path to your Xenominer installation folder. This is usually located at C:\\Program Files (x86)\\Xenominer: ");
                string? inputPath = Console.ReadLine()?.Trim('\"', ' '); // Strips quotes if drag-and-dropped

                if (string.IsNullOrWhiteSpace(inputPath)) continue;

                string potentialContentPath = Path.Combine(inputPath, "Content");

                if (Directory.Exists(potentialContentPath))
                {
                    ContentPath = potentialContentPath;
                    break;
                }
                else if (Directory.Exists(inputPath) && new DirectoryInfo(inputPath).Name.Equals("Content", StringComparison.OrdinalIgnoreCase))
                {
                    // Just in case you dragged the Content folder itself instead of the root game folder
                    ContentPath = inputPath;
                    break;
                }
                else
                {
                    Console.WriteLine("[!] Could not find a 'Content' folder at that location. Please verify the path.\n");
                }
            }

            Console.WriteLine($"\nTargeting: {ContentPath}\nStarting extraction...\n");

            // 2. Initialize MonoGame
            using var game = new HiddenGame();
            game.RunOneFrame();

            // 3. Force DLL Load
            try
            {
                Assembly.LoadFrom("XnaAux.dll");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Could not find XnaAux.dll! Make sure it built into the same folder as this exe. Error: {ex.Message}");
                return;
            }

            var content = new ContentManager(game.Services, ContentPath);

            // 4. Extract Everything
            var allFiles = Directory.GetFiles(ContentPath, "*.xnb", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                string assetName = Path.GetRelativePath(ContentPath, file).Replace(".xnb", "");
                ProcessAsset(content, assetName, file);
            }

            Console.WriteLine($"\nExtraction finished! All files saved to: {ExportPath}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void ProcessAsset(ContentManager content, string assetName, string filePath)
        {
            try
            {
                object asset = content.Load<object>(assetName);
                if (asset is Texture2D tex) ExportTextureUniversal(tex, assetName);
                else if (IsModel(asset, out Model? model)) ExportModel(model!, assetName);
                else if (asset is SoundEffect) ExportAudioManual(filePath, assetName);
            }
            catch (Exception ex)
            {
                if (!assetName.Contains("Effects"))
                    Console.WriteLine($"[FAIL] {assetName}: {ex.Message}");
            }
        }

        static void ExportTextureUniversal(Texture2D tex, string assetName)
        {
            string outPath = Path.Combine(ExportPath, assetName + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            try
            {
                using var canvas = new RenderTarget2D(tex.GraphicsDevice, tex.Width, tex.Height, false, SurfaceFormat.Color, DepthFormat.None);
                tex.GraphicsDevice.SetRenderTarget(canvas);
                tex.GraphicsDevice.Clear(Color.Transparent);
                using (var sb = new SpriteBatch(tex.GraphicsDevice))
                {
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                    sb.Draw(tex, new Rectangle(0, 0, tex.Width, tex.Height), Color.White);
                    sb.End();
                }
                tex.GraphicsDevice.SetRenderTarget(null);
                using Stream s = File.Create(outPath);
                canvas.SaveAsPng(s, tex.Width, tex.Height);
                Console.WriteLine($"[SUCCESS: TEX] {assetName}");
            }
            catch { Console.WriteLine($"[ERR: TEX] {assetName} conversion failed."); }
        }

        static bool IsModel(object asset, out Model? model)
        {
            model = asset as Model;
            if (model != null) return true;
            var prop = asset.GetType().GetProperty("Model") ?? asset.GetType().GetField("Model") as MemberInfo;
            if (prop != null)
            {
                model = (prop is PropertyInfo p ? p.GetValue(asset) : ((FieldInfo)prop).GetValue(asset)) as Model;
                return model != null;
            }
            return false;
        }

        static void ExportModel(Model model, string assetName)
        {
            StringBuilder obj = new StringBuilder();
            int vOff = 1;
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var stride = part.VertexBuffer.VertexDeclaration.VertexStride;
                    var vertices = new VertexPositionNormalTexture[part.NumVertices];
                    part.VertexBuffer.GetData(part.VertexOffset * stride, vertices, 0, part.NumVertices, stride);
                    foreach (var v in vertices)
                    {
                        obj.AppendLine($"v {v.Position.X} {v.Position.Y} {v.Position.Z}");
                        obj.AppendLine($"vt {v.TextureCoordinate.X} {1.0f - v.TextureCoordinate.Y}");
                    }
                    ushort[] indices = new ushort[part.PrimitiveCount * 3];
                    part.IndexBuffer.GetData(part.StartIndex * 2, indices, 0, part.PrimitiveCount * 3);
                    for (int i = 0; i < indices.Length; i += 3)
                        obj.AppendLine($"f {indices[i] + vOff}/{indices[i] + vOff} {indices[i + 1] + vOff}/{indices[i + 1] + vOff} {indices[i + 2] + vOff}/{indices[i + 2] + vOff}");
                    vOff += part.NumVertices;
                }
            }
            string outPath = Path.Combine(ExportPath, assetName + ".obj");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, obj.ToString());
            Console.WriteLine($"[SUCCESS: MOD] {assetName}");
        }

        static void ExportAudioManual(string filePath, string assetName)
        {
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] magic = br.ReadBytes(3);
                    if (magic[0] != 'X' || magic[1] != 'N' || magic[2] != 'B') return;

                    br.ReadChar();
                    br.ReadByte();
                    byte flags = br.ReadByte();

                    bool compressed = (flags & 0x80) != 0 || (flags & 0x40) != 0;
                    if (compressed)
                    {
                        Console.WriteLine($"[ERR: SND] {assetName} is compressed. Manual extraction requires uncompressed audio XNBs.");
                        return;
                    }

                    br.ReadInt32();

                    int readerCount = Read7BitEncodedInt(br);
                    for (int i = 0; i < readerCount; i++)
                    {
                        br.ReadString();
                        br.ReadInt32();
                    }
                    Read7BitEncodedInt(br);

                    Read7BitEncodedInt(br);

                    int formatLength = br.ReadInt32();
                    byte[] format = br.ReadBytes(formatLength);

                    int dataLength = br.ReadInt32();
                    byte[] data = br.ReadBytes(dataLength);

                    string outPath = Path.Combine(ExportPath, assetName + ".wav");
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

                    using (FileStream wavStream = new FileStream(outPath, FileMode.Create))
                    using (BinaryWriter bw = new BinaryWriter(wavStream))
                    {
                        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                        bw.Write(20 + formatLength + dataLength);
                        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                        bw.Write(Encoding.ASCII.GetBytes("fmt "));
                        bw.Write(formatLength);
                        bw.Write(format);
                        bw.Write(Encoding.ASCII.GetBytes("data"));
                        bw.Write(dataLength);
                        bw.Write(data);
                    }
                    Console.WriteLine($"[SUCCESS: SND] {assetName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR: SND] {assetName} manual extraction failed: {ex.Message}");
            }
        }

        static int Read7BitEncodedInt(BinaryReader reader)
        {
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                if (shift == 35) throw new FormatException("Bad 7-bit int format");
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
}