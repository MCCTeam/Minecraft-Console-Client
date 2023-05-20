using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Allows moving through a Minecraft world
    /// </summary>
    public static class Movement
    {
        /* ========= PATHFINDING METHODS ========= */

        /// <summary>
        /// Handle movements due to gravity
        /// </summary>
        /// <param name="world">World the player is currently located in</param>
        /// <param name="location">Location the player is currently at</param>
        /// <param name="motionY">Current vertical motion speed</param>
        /// <returns>Updated location after applying gravity</returns>
        public static Location HandleGravity(World world, Location location, ref double motionY)
        {
            if (Settings.InternalConfig.GravityEnabled)
            {
                Location onFoots = new(location.X, Math.Floor(location.Y), location.Z);
                Location belowFoots = Move(location, Direction.Down);
                if (location.Y > Math.Truncate(location.Y) + 0.0001)
                {
                    belowFoots = location;
                    belowFoots.Y = Math.Truncate(location.Y);
                }

                if (!IsOnGround(world, location) && !IsSwimming(world, location))
                {
                    while (!IsOnGround(world, belowFoots) && belowFoots.Y >= 1 + World.GetDimension().minY)
                        belowFoots = Move(belowFoots, Direction.Down);
                    location = Move2Steps(location, belowFoots, ref motionY, true).Dequeue();
                }
                else if (!world.GetBlock(onFoots).Type.IsSolid())
                    location = Move2Steps(location, onFoots, ref motionY, true).Dequeue();
            }

            return location;
        }

        /// <summary>
        /// Return a list of possible moves for the player
        /// </summary>
        /// <param name="world">World the player is currently located in</param>
        /// <param name="originLocation">Location the player is currently at</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <returns>A list of new locations the player can move to</returns>
        public static IEnumerable<Location> GetAvailableMoves(World world, Location originLocation,
            bool allowUnsafe = false)
        {
            Location location = originLocation.ToCenter();
            List<Location> availableMoves = new();
            if (IsOnGround(world, location) || IsSwimming(world, location))
            {
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    Location dest = Move(location, dir);
                    if (CanMove(world, location, dir) && (allowUnsafe || IsSafe(world, dest)))
                        availableMoves.Add(dest);
                }
            }
            else
            {
                foreach (Direction dir in new[] { Direction.East, Direction.West, Direction.North, Direction.South })
                    if (CanMove(world, location, dir) && IsOnGround(world, Move(location, dir)) &&
                        (allowUnsafe || IsSafe(world, Move(location, dir))))
                        availableMoves.Add(Move(location, dir));
                availableMoves.Add(Move(location, Direction.Down));
            }

            return availableMoves;
        }

        /// <summary>
        /// Decompose a single move from a block to another into several steps
        /// </summary>
        /// <remarks>
        /// Allows moving by little steps instead or directly moving between blocks,
        /// which would be rejected by anti-cheat plugins anyway.
        /// </remarks>
        /// <param name="start">Start location</param>
        /// <param name="goal">Destination location</param>
        /// <param name="motionY">Current vertical motion speed</param>
        /// <param name="falling">Specify if performing falling steps</param>
        /// <param name="stepsByBlock">Amount of steps by block</param>
        /// <returns>A list of locations corresponding to the requested steps</returns>
        public static Queue<Location> Move2Steps(Location start, Location goal, ref double motionY,
            bool falling = false, int stepsByBlock = 8)
        {
            if (stepsByBlock <= 0)
                stepsByBlock = 1;

            if (falling)
            {
                //Use MC-Like falling algorithm
                double y = start.Y;
                Queue<Location> fallSteps = new();
                fallSteps.Enqueue(start);
                motionY -= 0.08D;
                motionY *= 0.9800000190734863D;
                y += motionY;

                if (y < goal.Y)
                    return new Queue<Location>(new[] { goal });

                return new Queue<Location>(new[] { new Location(start.X, y, start.Z) });
            }
            else
            {
                //Regular MCC moving algorithm
                motionY = 0; //Reset motion speed
                double totalStepsDouble = start.Distance(goal) * stepsByBlock;
                int totalSteps = (int)Math.Ceiling(totalStepsDouble);
                Location step = (goal - start) / totalSteps;

                if (totalStepsDouble >= 1)
                {
                    Queue<Location> movementSteps = new();
                    for (int i = 1; i <= totalSteps; i++)
                        movementSteps.Enqueue(start + step * i);
                    return movementSteps;
                }
                else
                    return new Queue<Location>(new[] { goal });
            }
        }

        /// <summary>
        /// Calculate a path from the start location to the destination location
        /// </summary>
        /// <remarks>
        /// Based on the A* pathfinding algorithm described on Wikipedia
        /// </remarks>
        /// <see href="https://en.wikipedia.org/wiki/A*_search_algorithm#Pseudocode"/>
        /// <param name="world">World</param>
        /// <param name="start">Start location</param>
        /// <param name="goal">Destination location</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <param name="maxOffset">If no valid path can be found, also allow locations within specified distance of destination</param>
        /// <param name="minOffset">Do not get closer of destination than specified distance</param>
        /// <param name="timeout">How long to wait before stopping computation</param>
        /// <remarks>When location is unreachable, computation will reach timeout, then optionally fallback to a close location within maxOffset</remarks>
        /// <returns>A list of locations, or null if calculation failed</returns>
        public static Queue<Location>? CalculatePath(World world, Location start, Location goal, bool allowUnsafe,
            int maxOffset, int minOffset, TimeSpan timeout)
        {
            CancellationTokenSource cts = new();
            Task<Queue<Location>?> pathfindingTask = Task.Factory.StartNew(() =>
                CalculatePath(world, start, goal, allowUnsafe, maxOffset, minOffset, cts.Token));
            pathfindingTask.Wait(timeout);
            if (!pathfindingTask.IsCompleted)
            {
                cts.Cancel();
                pathfindingTask.Wait();
            }

            return pathfindingTask.Result;
        }

        /// <summary>
        /// Calculate a path from the start location to the destination location
        /// </summary>
        /// <remarks>
        /// Based on the A* pathfinding algorithm described on Wikipedia
        /// </remarks>
        /// <see href="https://en.wikipedia.org/wiki/A*_search_algorithm#Pseudocode"/>
        /// <param name="world">World</param>
        /// <param name="start">Start location</param>
        /// <param name="goal">Destination location</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <param name="maxOffset">If no valid path can be found, also allow locations within specified distance of destination</param>
        /// <param name="minOffset">Do not get closer of destination than specified distance</param>
        /// <param name="ct">Token for stopping computation after a certain time</param>
        /// <returns>A list of locations, or null if calculation failed</returns>
        public static Queue<Location>? CalculatePath(World world, Location start, Location goal, bool allowUnsafe,
            int maxOffset, int minOffset, CancellationToken ct)
        {
            // This is a bad configuration
            if (minOffset > maxOffset)
                throw new ArgumentException("minOffset must be lower or equal to maxOffset", nameof(minOffset));

            // Round start coordinates for easier calculation
            Location startLower = start.ToFloor();
            Location goalLower = goal.ToFloor();

            // We always use distance squared so our limits must also be squared.
            minOffset *= minOffset;
            maxOffset *= maxOffset;

            // Prepare variables and datastructures for A*

            // Dictionary that contains the relation between all coordinates and resolves the final path
            Dictionary<Location, Location> cameFrom = new();
            // Create a Binary Heap for all open positions => Allows fast access to Nodes with lowest scores
            BinaryHeap openSet = new();
            // Dictionary to keep track of the G-Score of every location
            Dictionary<Location, int> gScoreDict = new();

            // Set start values for variables
            openSet.Insert(0, (int)startLower.DistanceSquared(goalLower), startLower);
            gScoreDict[startLower] = 0;
            BinaryHeap.Node? current = null;

            // Start of A*

            // Execute while we have nodes to process and we are not cancelled
            while (openSet.Count() > 0 && !ct.IsCancellationRequested)
            {
                // Get the root node of the Binary Heap
                // Node with the lowest F-Score or lowest H-Score on tie
                current = openSet.GetRootLocation();

                // Return if goal found and no maxOffset was given OR current node is between minOffset and maxOffset
                if ((current.Location == goalLower && maxOffset <= 0) ||
                    (maxOffset > 0 && current.HScore >= minOffset && current.HScore <= maxOffset))
                    return ReconstructPath(cameFrom, current.Location, start, goal);

                // Discover neighbored blocks
                foreach (Location neighbor in GetAvailableMoves(world, current.Location, allowUnsafe))
                {
                    // If we are cancelled: break
                    if (ct.IsCancellationRequested)
                        break;

                    // tentative_GScore is the distance from start to the neighbor through current
                    int tentativeGScore = current.GScore + (int)current.Location.DistanceSquared(neighbor);

                    // If the neighbor is not in the GScoreDict OR its current tentativeGScore is lower than the previously saved one: 
                    if (!gScoreDict.ContainsKey(neighbor) ||
                        (gScoreDict.ContainsKey(neighbor) && tentativeGScore < gScoreDict[neighbor]))
                    {
                        // Save the new relation between the neighbored block and the current one
                        cameFrom[neighbor] = current.Location;
                        gScoreDict[neighbor] = tentativeGScore;

                        // If this location is not already included in the Binary Heap: save it
                        if (!openSet.ContainsLocation(neighbor))
                            openSet.Insert(tentativeGScore, (int)neighbor.DistanceSquared(goalLower), neighbor);
                    }
                }
            }

            // Goal could not be reached. Set the path to the closest location if close enough
            if (current != null && openSet.MinHScoreNode != null &&
                (maxOffset == int.MaxValue || openSet.MinHScoreNode.HScore <= maxOffset))
                return ReconstructPath(cameFrom, openSet.MinHScoreNode.Location, start, goal);

            return null;
        }

        /// <summary>
        /// Helper function for CalculatePath(). Backtrack from goal to start to reconstruct a step-by-step path.
        /// </summary>
        /// <param name="cameFrom">The collection of Locations that leads back to the start</param>
        /// <param name="current">Endpoint of our later walk</param>
        /// <param name="start">Start location</param>
        /// <param name="end">End location</param>
        /// <returns>the path that leads to current from the start position</returns>
        private static Queue<Location> ReconstructPath(Dictionary<Location, Location> cameFrom, Location current,
            Location start, Location end)
        {
            int midPathCnt = 0;
            List<Location> totalPath = new();

            // Move from the center of the block to the final position
            if (current != end && current == end.ToFloor())
                totalPath.Add(end);

            // Generate intermediate paths
            totalPath.Add(current.ToCenter());
            while (cameFrom.ContainsKey(current))
            {
                ++midPathCnt;
                current = cameFrom[current];
                totalPath.Add(current.ToCenter());
            }

            if (midPathCnt <= 2 && start.DistanceSquared(end) < 2.0)
                return new Queue<Location>(new[] { end });
            else
            {
                // Move to the center of the block first
                if (current != start && current == start.ToFloor())
                    totalPath.Add(start.ToCenter());

                totalPath.Reverse();
                return new Queue<Location>(totalPath);
            }
        }

        /// <summary>
        /// A datastructure to store Locations as Nodes and provide them in sorted and queued order.
        /// !!!
        /// CAN BE REPLACED WITH PriorityQueue IN .NET-6
        /// https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-6.0
        /// !!!
        /// </summary>
        public class BinaryHeap
        {
            /// <summary>
            /// Represents a location and its attributes
            /// </summary>
            public class Node
            {
                // Distance to start
                public int GScore;

                // Distance to Goal
                public int HScore;

                public int FScore
                {
                    get { return HScore + GScore; }
                }

                public Location Location;

                public Node(int gScore, int hScore, Location loc)
                {
                    this.GScore = gScore;
                    this.HScore = hScore;
                    Location = loc;
                }
            }

            // List which contains all nodes in form of a Binary Heap
            private readonly List<Node> heapList;

            // Hashset for quick checks of locations included in the heap
            private readonly HashSet<Location> locationList;
            public Node? MinHScoreNode;

            public BinaryHeap()
            {
                heapList = new List<Node>();
                locationList = new HashSet<Location>();
                MinHScoreNode = null;
            }

            /// <summary>
            /// Insert a new location in the heap
            /// </summary>
            /// <param name="newGScore">G-Score of the location</param>
            /// <param name="newHScore">H-Score of the location</param>
            /// <param name="loc">The location</param>
            public void Insert(int newGScore, int newHScore, Location loc)
            {
                // Begin at the end of the list
                int i = heapList.Count;

                // Temporarily save the node created with the parameters to allow comparisons
                Node newNode = new(newGScore, newHScore, loc);

                // Add new note to the end of the list
                heapList.Add(newNode);
                locationList.Add(loc);

                // Save node with the smallest H-Score => Distance to goal
                if (MinHScoreNode == null || newNode.HScore < MinHScoreNode.HScore)
                    MinHScoreNode = newNode;

                if (i == 0)
                    return;

                // There is no need of sorting for one node.
                // Go up the heap from child to parent and move parent down...
                // while we are not looking at the root node AND the new node has better attributes than the parent node ((i - 1) / 2)
                while (i > 0 && FirstNodeBetter(newNode /* Current Child */,
                           heapList[(i - 1) / 2] /* Corresponding Parent */))
                {
                    // Move parent down and replace current child -> New free space is created
                    heapList[i] = heapList[(i - 1) / 2];
                    // Select the next parent to check
                    i = (i - 1) / 2;
                }

                // Nodes were moved down at position I there is now a free space at the correct position for our new node:
                // Insert new node in position
                heapList[i] = newNode;
            }

            /// <summary>
            /// Obtain the root which represents the node the the best attributes currently
            /// </summary>
            /// <returns>node with the best attributes currently</returns>
            /// <exception cref="InvalidOperationException"></exception>
            public Node GetRootLocation()
            {
                // The heap is empty. There is nothing to return.
                if (heapList.Count == 0)
                    throw new InvalidOperationException("The heap is empty.");

                // Save the root node
                var rootNode = heapList[0];
                locationList.Remove(rootNode.Location);

                // Temporarirly store the last item's value.
                var lastNode = heapList[^1];

                // Remove the last value.
                heapList.RemoveAt(heapList.Count - 1);

                if (heapList.Count > 0)
                {
                    // Start at the first index.
                    var currentParentPos = 0;

                    // Go through the heap from root to bottom...
                    // Continue until the halfway point of the heap.
                    while (currentParentPos < heapList.Count / 2)
                    {
                        // Select the left child of the current parent
                        var currentChildPos = (2 * currentParentPos) + 1;

                        // If the currently selected child is not the last entry of the list AND right child has better attributes
                        if ((currentChildPos < heapList.Count - 1) && FirstNodeBetter(heapList[currentChildPos + 1],
                                heapList[currentChildPos]))
                        {
                            // Select the right child
                            currentChildPos++;
                        }

                        // If the last item is smaller than both siblings at the
                        // current height, break.
                        if (FirstNodeBetter(lastNode, heapList[currentChildPos]))
                            break;

                        // Move the item at index j up one level.
                        heapList[currentParentPos] = heapList[currentChildPos];
                        // Move index i to the appropriate branch.
                        currentParentPos = currentChildPos;
                    }

                    // Insert the last node into the currently free position
                    heapList[currentParentPos] = lastNode;
                }

                return rootNode;
            }

            /// <summary>
            /// Compares two nodes and evaluates their position to the goal.
            /// </summary>
            /// <param name="firstNode">First node to compare</param>
            /// <param name="secondNode">Second node to compare</param>
            /// <returns>True if the first node has a more promising position to the goal than the second</returns>
            private static bool FirstNodeBetter(Node firstNode, Node secondNode)
            {
                // Is the FScore smaller?
                return (firstNode.FScore < secondNode.FScore) ||
                       // If FScore is equal, evaluate the h-score
                       (firstNode.FScore == secondNode.FScore && firstNode.HScore < secondNode.HScore);
            }

            /// <summary>
            /// Get the size of the heap
            /// </summary>
            /// <returns>size of the heap</returns>
            public int Count()
            {
                return heapList.Count;
            }

            /// <summary>
            /// Check if the heap contains a node with a certain location
            /// </summary>
            /// <param name="loc">Location to check</param>
            /// <returns>true if a node with the given location is in the heap</returns>
            public bool ContainsLocation(Location loc)
            {
                return locationList.Contains(loc);
            }
        }

        /* ========= LOCATION PROPERTIES ========= */

        // TODO: Find a way to remove this Hack for Vines here.

        /// <summary>
        /// Check if the specified location is on the ground
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location is on the ground</returns>
        public static bool IsOnGround(World world, Location location)
        {
            ChunkColumn? chunkColumn = world.GetChunkColumn(location);
            if (chunkColumn == null || chunkColumn.FullyLoaded == false)
                return true; // avoid moving downward in a not loaded chunk

            Location down = Move(location, Direction.Down);
            Material currentMaterial = world.GetBlock(down).Type;

            var result = currentMaterial.IsSolid()
                         || currentMaterial == Material.TwistingVines || currentMaterial == Material.TwistingVinesPlant
                         || currentMaterial == Material.WeepingVines || currentMaterial == Material.WeepingVinesPlant
                         || currentMaterial == Material.Vine;

            var northCheck = 1 + Math.Floor(down.Z) - down.Z > 0.7;
            var eastCheck = down.X - Math.Floor(down.X) > 0.7;
            var southCheck = down.Z - Math.Floor(down.Z) > 0.7;
            var westCheck = 1 + Math.Floor(down.X) - down.X > 0.7;

            if (!result && northCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.North));

            if (!result && northCheck && eastCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.NorthEast));

            if (!result && eastCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.East));

            if (!result && eastCheck && southCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.SouthEast));

            if (!result && southCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.South));

            if (!result && southCheck && westCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.SouthWest));

            if (!result && westCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.West));

            if (!result && westCheck && northCheck)
                result |= IsSolidOrVine(world, Move(down, Direction.NorthWest));

            return result && (location.Y <= Math.Truncate(location.Y) + 0.0001);
        }

        private static bool IsSolidOrVine(World world, Location location)
        {
            var block = world.GetBlock(location);
            return block.Type.IsSolid()
                   || block.Type == Material.TwistingVines
                   || block.Type == Material.TwistingVinesPlant
                   || block.Type == Material.WeepingVines
                   || block.Type == Material.WeepingVinesPlant
                   || block.Type == Material.Vine;
        }

        /// <summary>
        /// Check if the specified location implies swimming
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location implies swimming</returns>
        private static bool IsSwimming(World world, Location location)
        {
            return world.GetBlock(location).Type.IsLiquid();
        }

        /// <summary>
        /// Check if the specified location can be climbed on
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location can be climbed on</returns>
        private static bool IsClimbing(World world, Location location)
        {
            return world.GetBlock(location).Type.CanBeClimbedOn();
        }

        /// <summary>
        /// Check if the specified location is safe
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the destination location won't directly harm the player</returns>
        private static bool IsSafe(World world, Location location)
        {
            return
                //No block that can harm the player
                !world.GetBlock(location).Type.CanHarmPlayers()
                && !world.GetBlock(Move(location, Direction.Up)).Type.CanHarmPlayers()
                && !world.GetBlock(Move(location, Direction.Down)).Type.CanHarmPlayers()

                //No fall from a too high place
                && (world.GetBlock(Move(location, Direction.Down)).Type.IsSolid() ||
                    IsClimbing(world, Move(location, Direction.Down))
                    || world.GetBlock(Move(location, Direction.Down, 2)).Type.IsSolid() ||
                    IsClimbing(world, Move(location, Direction.Down, 2))
                    || world.GetBlock(Move(location, Direction.Down, 3)).Type.IsSolid() ||
                    IsClimbing(world, Move(location, Direction.Down, 3)))

                //Not an underwater location
                && !(world.GetBlock(Move(location, Direction.Up)).Type.IsLiquid());
        }

        /* ========= SIMPLE MOVEMENTS ========= */

        /// <summary>
        /// Check if the player can move in the specified direction
        /// </summary>
        /// <param name="world">World the player is currently located in</param>
        /// <param name="location">Location the player is currently at</param>
        /// <param name="direction">Direction the player is moving to</param>
        /// <returns>True if the player can move in the specified direction</returns>
        public static bool CanMove(World world, Location location, Direction direction)
        {
            switch (direction)
            {
                // Move vertical
                case Direction.Down:
                    return IsClimbing(world, Move(location, Direction.Down)) || !IsOnGround(world, location);
                case Direction.Up:
                    bool nextTwoBlocks =
                        !world.GetBlock(Move(Move(location, Direction.Up), Direction.Up)).Type.IsSolid();

                    // Check if the current block can be climbed on
                    if (IsClimbing(world, location))
                        // Check if next block after the next one can be climbed upon
                        return IsClimbing(world, Move(location, Direction.Up)) || nextTwoBlocks;

                    return (IsOnGround(world, location) || IsSwimming(world, location)) && nextTwoBlocks;

                // Move horizontal
                case Direction.East:
                case Direction.West:
                case Direction.South:
                case Direction.North:
                    return PlayerFitsHere(world, Move(location, direction));

                // Move diagonal
                case Direction.NorthEast:
                    return PlayerFitsHere(world, Move(location, Direction.North)) &&
                           PlayerFitsHere(world, Move(location, Direction.East)) &&
                           PlayerFitsHere(world, Move(location, direction));
                case Direction.SouthEast:
                    return PlayerFitsHere(world, Move(location, Direction.South)) &&
                           PlayerFitsHere(world, Move(location, Direction.East)) &&
                           PlayerFitsHere(world, Move(location, direction));
                case Direction.SouthWest:
                    return PlayerFitsHere(world, Move(location, Direction.South)) &&
                           PlayerFitsHere(world, Move(location, Direction.West)) &&
                           PlayerFitsHere(world, Move(location, direction));
                case Direction.NorthWest:
                    return PlayerFitsHere(world, Move(location, Direction.North)) &&
                           PlayerFitsHere(world, Move(location, Direction.West)) &&
                           PlayerFitsHere(world, Move(location, direction));

                default:
                    throw new ArgumentException("Unknown direction", nameof(direction));
            }
        }

        /// <summary>
        /// Evaluates if a player fits in this location
        /// </summary>
        /// <param name="world">Current world</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if a player is able to stand in this location</returns>
        public static bool PlayerFitsHere(World world, Location location)
        {
            var canClimb = IsClimbing(world, location) && IsClimbing(world, Move(location, Direction.Up));
            var isNotSolid = !world.GetBlock(location).Type.IsSolid() &&
                             !world.GetBlock(Move(location, Direction.Up)).Type.IsSolid();

            // Handle slabs
            if (!isNotSolid && world.GetBlock(Move(location, Direction.Up))
                    .IsTopSlab(McClient.Instance!.GetProtocolVersion()))
                isNotSolid = true;

            return canClimb || isNotSolid;
        }

        /// <summary>
        /// Get an updated location for moving in the specified direction
        /// </summary>
        /// <param name="location">Current location</param>
        /// <param name="direction">Direction to move to</param>
        /// <param name="length">Distance, in blocks</param>
        /// <returns>Updated location</returns>
        public static Location Move(Location location, Direction direction, int length = 1)
        {
            return location + Move(direction) * length;
        }

        /// <summary>
        /// Get a location delta for moving in the specified direction
        /// </summary>
        /// <param name="direction">Direction to move to</param>
        /// <returns>A location delta for moving in that direction</returns>
        private static Location Move(Direction direction)
        {
            return direction switch
            {
                // Move vertical
                Direction.Down => new Location(0, -1, 0),
                Direction.Up => new Location(0, 1, 0),

                // Move horizontal straight
                Direction.East => new Location(1, 0, 0),
                Direction.West => new Location(-1, 0, 0),
                Direction.South => new Location(0, 0, 1),
                Direction.North => new Location(0, 0, -1),

                // Move horizontal diagonal
                Direction.NorthEast => Move(Direction.North) + Move(Direction.East),
                Direction.SouthEast => Move(Direction.South) + Move(Direction.East),
                Direction.SouthWest => Move(Direction.South) + Move(Direction.West),
                Direction.NorthWest => Move(Direction.North) + Move(Direction.West),

                _ => throw new ArgumentException("Unknown direction", nameof(direction))
            };
        }

        /// <summary>
        /// Check that the chunks at both the start and destination locations have been loaded
        /// </summary>
        /// <param name="world">Current world</param>
        /// <param name="start">Start location</param>
        /// <param name="dest">Destination location</param>
        /// <returns>Is loading complete</returns>
        public static bool CheckChunkLoading(World world, Location start, Location dest)
        {
            var chunkColumn = world.GetChunkColumn(dest);
            if (chunkColumn == null || chunkColumn.FullyLoaded == false)
                return false;

            chunkColumn = world.GetChunkColumn(start);
            if (chunkColumn == null || chunkColumn.FullyLoaded == false)
                return false;

            return true;
        }
    }
}