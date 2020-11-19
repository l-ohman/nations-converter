﻿using GBX.NET;
using GBX.NET.BlockInfo;
using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NationsConverter.Stages
{
    public class GroundPlacer : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.Blocks.ForEach(x =>
            {
                x.Coord += (8, 0, 8); // Shift the block by 8x0x8 positions to center the blocks for the new Stadium

                if (version >= GameVersion.TM2)
                    x.Coord -= (0, 8, 0);
            });

            map.ImportFileToEmbed("UserData/Items/NationsConverter/z_terrain/w_grass/GrassGround.Item.Gbx", "Items/NationsConverter/z_terrain/w_grass");

            var dirtBlocks = new string[] { "StadiumDirt", "StadiumDirtHill" };

            for (var x = 0; x < map.Size.GetValueOrDefault().X; x++)
            {
                for (var z = 0; z < map.Size.GetValueOrDefault().Z; z++)
                {
                    var dirtBlockExists = false;

                    foreach (var groundBlock in map.Blocks.Where(o => o.Coord == (x, 0, z)))
                    {
                        if (dirtBlocks.Contains(groundBlock.Name))
                        {
                            dirtBlockExists = true;
                            break;
                        }
                    }

                    if (!dirtBlockExists)
                        map.PlaceAnchoredObject(
                            (@"NationsConverter\z_terrain\w_grass\GrassGround.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                            (x, 1, z) * map.Collection.GetBlockSize(),
                            (0, 0, 0));
                }
            }

            var blocks = map.Blocks.ToArray();

            map.Blocks = blocks.AsParallel().Where(x =>
            {
                if (x.Name == "StadiumDirt" || x.Name == "StadiumDirtHill")
                {
                    var dirtBlock = x;

                    foreach (var block in blocks)
                    {
                        if (parameters.Definitions.TryGetValue(block.Name, out Conversion[] variants))
                        {
                            if (variants != null)
                            {
                                var variant = block.Variant.GetValueOrDefault();

                                if (variants.Length > variant)
                                {
                                    var conversion = variants[variant];

                                    if (conversion != null) // If the variant actually has a conversion
                                    {
                                        if (conversion.RemoveGround)
                                        {
                                            if (BlockInfoManager.BlockModels.TryGetValue(block.Name, out BlockModel model))
                                            {
                                                var center = default(Vec3);
                                                var allCoords = new Int3[model.Ground.Length];
                                                var newCoords = new Vec3[model.Ground.Length];
                                                var newMin = default(Vec3);

                                                if (model.Ground.Length > 1)
                                                {
                                                    allCoords = model.Ground.Select(b => (Int3)b.Coord).ToArray();
                                                    var min = new Int3(allCoords.Select(c => c.X).Min(), allCoords.Select(c => c.Y).Min(), allCoords.Select(c => c.Z).Min());
                                                    var max = new Int3(allCoords.Select(c => c.X).Max(), allCoords.Select(c => c.Y).Max(), allCoords.Select(c => c.Z).Max());
                                                    var size = max - min + (1, 1, 1);
                                                    center = (min + max) * .5f;

                                                    for(var i = 0; i < model.Ground.Length; i++)
                                                        newCoords[i] = AdditionalMath.RotateAroundCenter(allCoords[i], center, AdditionalMath.ToRadians(block.Direction));

                                                    newMin = new Vec3(newCoords.Select(c => c.X).Min(), newCoords.Select(c => c.Y).Min(), newCoords.Select(c => c.Z).Min());
                                                }

                                                foreach (var unit in newCoords)
                                                    if (dirtBlock.Coord == block.Coord + (Int3)(unit - newMin))
                                                        return false;
                                            }
                                            else if (dirtBlock.Coord == block.Coord)
                                                return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }).ToList();
        }
    }
}
