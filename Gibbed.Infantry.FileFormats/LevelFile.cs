﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.Infantry.FileFormats
{
    public class LevelFile
    {
        public int Width;
        public int Height;

        public int ObjectXOffset;
        public int ObjectYOffset;

        public readonly short[] PhysicsLow = new short[32];
        public readonly short[] PhysicsHigh = new short[32];

        public readonly uint[] Palette = new uint[512];
        public readonly int[] TerrainIds = new int[128];

        public uint LightColorWhite;
        public uint LightColorRed;
        public uint LightColorGreen;
        public uint LightColorBlue;

        public Level.Tile[] Tiles;
        public List<Level.Entity> Entities;

        public LevelFile()
        {
            this.SetDefaults();
        }

        private void SetDefaults()
        {
            this.LightColorWhite = 0xFFFFFF00u;
            this.LightColorRed = 0x0000FF00u;
            this.LightColorGreen = 0x00FF0000u;
            this.LightColorBlue = 0xFF000000u;

            Array.Clear(this.PhysicsLow, 0, this.PhysicsLow.Length);

            this.PhysicsHigh[0] = 0;
            this.PhysicsHigh[1] = 1024;
            this.PhysicsHigh[2] = 1024;
            this.PhysicsHigh[3] = 1024;
            this.PhysicsHigh[4] = 1024;
            this.PhysicsHigh[5] = 1024;
            this.PhysicsHigh[6] = 16;
            this.PhysicsHigh[7] = 16;
            this.PhysicsHigh[8] = 16;
            this.PhysicsHigh[9] = 16;
            this.PhysicsHigh[10] = 16;
            this.PhysicsHigh[11] = 32;
            this.PhysicsHigh[12] = 32;
            this.PhysicsHigh[13] = 32;
            this.PhysicsHigh[14] = 32;
            this.PhysicsHigh[15] = 32;
            this.PhysicsHigh[16] = 64;
            this.PhysicsHigh[17] = 64;
            this.PhysicsHigh[18] = 64;
            this.PhysicsHigh[19] = 64;
            this.PhysicsHigh[20] = 64;
            this.PhysicsHigh[21] = 128;
            this.PhysicsHigh[22] = 128;
            this.PhysicsHigh[23] = 128;
            this.PhysicsHigh[24] = 128;
            this.PhysicsHigh[25] = 128;
            this.PhysicsHigh[26] = 1024;
            this.PhysicsHigh[27] = 1024;
            this.PhysicsHigh[29] = 1024;
            this.PhysicsHigh[28] = 1024;
            this.PhysicsHigh[30] = 1024;
            this.PhysicsHigh[31] = 1024;
        }

        public void Deserialize(Stream input)
        {
            var header = input.ReadStructure<Level.Header>();
            
            this.Width = header.Width;
            this.Height = header.Height;

            this.ObjectXOffset = header.ObjectXOffset;
            this.ObjectYOffset = header.ObjectYOffset;

            for (int i = 0; i < header.Padding.Length; i++)
            {
                if (header.Padding[i] != 0)
                {
                    throw new FormatException("non-zero data in padding");
                }
            }

            Array.Copy(header.TerrainIds, this.TerrainIds, header.TerrainIds.Length);
            for (int i = 0; i < this.TerrainIds.Length; i++)
            {
                this.TerrainIds[i] %= 16;
            }

            if (header.Version >= 4)
            {
                this.LightColorWhite = header.LightColorWhite;
                this.LightColorRed = header.LightColorRed;
                this.LightColorGreen = header.LightColorGreen;
                this.LightColorBlue = header.LightColorBlue;

                Array.Copy(header.PhysicsLow, this.PhysicsLow, header.PhysicsLow.Length);
                Array.Copy(header.PhysicsHigh, this.PhysicsHigh, header.PhysicsHigh.Length);
            }
            else
            {
                this.SetDefaults();
            }

            var objectBlobs = new Level.BlobReference[header.ObjectCount];
            var floorBlobs = new Level.BlobReference[header.FloorCount];

            for (int i = 0; i < objectBlobs.Length; i++)
            {
                if (header.Version >= 6)
                {

                    objectBlobs[i] = input.ReadStructure<Level.BlobReference>();
                }
                else
                {
                    objectBlobs[i] = new Level.BlobReference();
                    objectBlobs[i].FileName = null;
                    objectBlobs[i].Id = string.Format("o{0}.cfs", i);
                }
            }

            for (int i = 0; i < floorBlobs.Length; i++)
            {
                if (header.Version >= 6)
                {
                    floorBlobs[i] = input.ReadStructure<Level.BlobReference>();
                }
                else
                {
                    floorBlobs[i] = new Level.BlobReference();
                    floorBlobs[i].FileName = null;
                    floorBlobs[i].Id = string.Format("f{0}.cfs", i);
                }
            }

            this.Tiles = new Level.Tile[this.Width * this.Height];

            var tileData = input.DecompressRLE(3, this.Tiles.Length);
            for (int i = 0, j = 0; i < this.Tiles.Length; i++, j += 3)
            {
                this.Tiles[i].A = tileData[j + 0];
                this.Tiles[i].B = tileData[j + 1];
                this.Tiles[i].C = tileData[j + 2];
            }

            if (header.Version < 4)
            {
                var magic = new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x1A, 0x1B,
                    0x1C, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x06, 0x07, 0x08, 0x09, 0x10, 0x1A, 0x1B,
                    0x1C, 0x1D, 0x00, 0x00, 0x6C, 0x00, 0x00, 0x00,
                };

                for (int i = 0; i < this.Tiles.Length; i++)
                {
                    var a = this.Tiles[i].C;
                    var b = (byte)((magic[a & 0x1F] ^ a) & 0x1F);
                    this.Tiles[i].C ^= b;
                }
            }

            var entities = new Level.Entity[header.EntityCount];

            if (header.Version >= 3)
            {
                for (int i = 0; i < header.EntityCount; i++)
                {
                    entities[i] = input.ReadStructure<Level.Entity>();
                }
            }
            else
            {
                using (var entityData =
                    new MemoryStream(input.DecompressRLE(14, header.EntityCount)))
                {
                    for (int i = 0; i < header.EntityCount; i++)
                    {
                        var oldEntity = entityData.ReadStructure<Level.OldEntity>();
                        
                        //entities[i] = new Level.Entity();
                        entities[i].X = oldEntity.X;
                        entities[i].Y = oldEntity.Y;
                        entities[i].U04 = oldEntity.U04 & 0x3FFFFFFFu;
                        entities[i].U08 = oldEntity.U08 & 0x7FFFFFFFu;
                        entities[i].U0C = (oldEntity.U0D & 0x7Fu) << 23;
                        entities[i].U10 = oldEntity.U0C;
                    }
                }
            }

            if (header.Version < 5)
            {
                for (int i = 0; i < header.EntityCount; i++)
                {
                    entities[i].U0C &= 0xBFFFFFFFu;
                }
            }

            this.Entities = new List<Level.Entity>();
            this.Entities.AddRange(entities);

            if (input.Position != input.Length)
            {
                throw new FormatException();
            }
        }
    }
}