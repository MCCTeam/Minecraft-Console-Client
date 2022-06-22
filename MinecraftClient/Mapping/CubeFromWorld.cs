using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// A row of blocks that will be mined
    /// </summary>
    public class Row
    {
        public readonly List<Location> BlocksInRow;

        /// <summary>
        /// Initialize a row of blocks
        /// </summary>
        /// <param name="bIL"> Enter a list of blocks </param>
        public Row(List<Location> blocksInRow = null)
        {
            BlocksInRow = blocksInRow ?? new List<Location>();
        }
    }

    /// <summary>
    /// Several rows are summarized in a layer
    /// </summary>
    public class Layer
    {
        public readonly List<Row> RowsInLayer;

        /// <summary>
        /// Add a new row to this layer
        /// </summary>
        /// <param name="givenRow"> enter a row that should be added </param>
        /// <returns> Index of the last row </returns>
        public void AddRow(Row givenRow = null)
        {
            RowsInLayer.Add(givenRow ?? new Row());
        }

        /// <summary>
        /// Initialize a layer
        /// </summary>
        /// <param name="rTM"> Enter a list of rows </param>
        public Layer(List<Row> rowInLayer = null)
        {
            RowsInLayer = rowInLayer ?? new List<Row>();
        }
    }

    /// <summary>
    /// Several layers result in a cube
    /// </summary>
    public class Cube
    {
        public readonly List<Layer> LayersInCube;

        /// <summary>
        /// Add a new layer to the cube
        /// </summary>
        /// <param name="givenLayer"> Enter a layer that should be added </param>
        /// <returns> Index of the last layer </returns>
        public void AddLayer(Layer givenLayer = null)
        {
            LayersInCube.Add(givenLayer ?? new Layer());
        }

        /// <summary>
        /// Initialize a cube
        /// </summary>
        /// <param name="lTM"> Enter a list of layers </param>
        public Cube(List<Layer> layerInCube = null)
        {
            LayersInCube = layerInCube ?? new List<Layer>();
        }
    }

    public static class CubeFromWorld
    {
        /// <summary>
        /// Creates a cube of blocks out of two coordinates.
        /// </summary>
        /// <param name="startBlock">Start Location</param>
        /// <param name="stopBlock">Stop Location</param>
        /// <returns>A cube of blocks consisting of Layers, Rows and single blocks</returns>
        public static Cube GetBlocksAsCube(World currentWorld, Location startBlock, Location stopBlock, List<Material> materialList = null, bool isBlacklist = true)
        {
            // Initialize cube to mine.
            Cube cubeToMine = new Cube();

            // Get the distance between start and finish as Vector.
            Location vectorToStopPosition = stopBlock - startBlock;

            // Initialize Iteration process
            int[] iterateX = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.X))).ToArray();
            int[] iterateY = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Y))).ToArray();
            int[] iterateZ = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Z))).ToArray();

            // Iterate through all coordinates relative to the start block.
            foreach (int y in iterateY)
            {
                Layer tempLayer = new Layer();
                foreach (int x in iterateX)
                {
                    Row tempRow = new Row();
                    foreach (int z in iterateZ)
                    {
                        if (materialList != null && materialList.Count > 0)
                        {
                            Location tempLocation = new Location(Math.Round(startBlock.X + x), Math.Round(startBlock.Y + y), Math.Round(startBlock.Z + z));
                            Material tempLocationMaterial = currentWorld.GetBlock(tempLocation).Type;

                            // XOR
                            // If blacklist == true and it does not contain the material (false); Add it.
                            // If blacklist == false (whitelist) and it contains the item (true); Add it.
                            if (isBlacklist ^ materialList.Contains(tempLocationMaterial))
                            {
                                tempRow.BlocksInRow.Add(tempLocation);
                            }
                        }
                        else
                        {
                            tempRow.BlocksInRow.Add(new Location(Math.Round(startBlock.X + x), Math.Round(startBlock.Y + y), Math.Round(startBlock.Z + z)));
                        }
                    }
                    if (tempRow.BlocksInRow.Count > 0)
                    {
                        tempLayer.AddRow(tempRow);
                    }
                }
                if (tempLayer.RowsInLayer.Count > 0)
                {
                    cubeToMine.AddLayer(tempLayer);
                }
            }

            return cubeToMine;
        }

        /// <summary>
        /// Get all numbers between from and to.
        /// </summary>
        /// <param name="start">Number to start</param>
        /// <param name="end">Number to stop</param>
        /// <returns>All numbers between the start and stop number, including the stop number</returns>
        private static List<int> GetNumbersFromTo(int start, int stop)
        {
            List<int> tempList = new List<int>();
            if (start <= stop)
            {
                for (int i = start; i <= stop; i++)
                {
                    tempList.Add(i);
                }
            }
            else
            {
                for (int i = start; i >= stop; i--)
                {
                    tempList.Add(i);
                }
            }
            return tempList;
        }
    }
}
