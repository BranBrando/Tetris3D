using UnityEngine;

namespace TetrisGame
{
    public class GridSystem : MonoBehaviour
    {
        private Transform[,,] grid;
        private int width;
        private int height;
        private int depth;
        private int y_offset;

        public void Initialize(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            y_offset = 3;
            grid = new Transform[width, height + y_offset, depth];
        }

        public bool IsPositionValid(Vector3Int position)
        {
            return position.x >= 0 && position.x < width &&
                   position.y >= 0 && position.y < (height + y_offset) &&
                   position.z >= 0 && position.z < depth &&
                   grid[position.x, position.y, position.z] == null;
        }

        public void StoreBlock(Vector3Int position, Transform block)
        {
            if (IsPositionValid(position))
            {
                grid[position.x, position.y, position.z] = block;
            }
        }

        #region XZ Plane Methods (Horizontal Layers)
        public bool CheckXZPlane(int y)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ClearXZPlane(int y)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        Destroy(grid[x, y, z].gameObject);
                    }
                    grid[x, y, z] = null;
                }
            }
        }

        public void ShiftDownAbove(int startY)
        {
            for (int y = startY; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (grid[x, y, z] != null)
                        {
                            grid[x, y - 1, z] = grid[x, y, z];
                            grid[x, y, z] = null;
                            grid[x, y - 1, z].position += Vector3.down;
                        }
                    }
                }
            }
        }
        #endregion

        #region XY Plane Methods (Depth Layers)
        public bool CheckXYPlane(int z)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y, z] == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ClearXYPlane(int z)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y, z] != null)
                    {
                        Destroy(grid[x, y, z].gameObject);
                    }
                    grid[x, y, z] = null;
                }
            }
        }

        public void ShiftForwardBehind(int startZ)
        {
            for (int z = startZ; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (grid[x, y, z] != null)
                        {
                            grid[x, y, z - 1] = grid[x, y, z];
                            grid[x, y, z] = null;
                            grid[x, y, z - 1].position += Vector3.back;
                        }
                    }
                }
            }
        }
        #endregion

        #region YZ Plane Methods (Width Layers)
        public bool CheckYZPlane(int x)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void ClearYZPlane(int x)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        Destroy(grid[x, y, z].gameObject);
                    }
                    grid[x, y, z] = null;
                }
            }
        }

        public void ShiftRightToLeft(int startX)
        {
            for (int x = startX; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (grid[x, y, z] != null)
                        {
                            grid[x - 1, y, z] = grid[x, y, z];
                            grid[x, y, z] = null;
                            grid[x - 1, y, z].position += Vector3.left;
                        }
                    }
                }
            }
        }
        #endregion

        public bool IsPositionOccupied(Vector3Int position)
        {
            return position.x >= 0 && position.x < width &&
                   position.y >= 0 && position.y < height &&
                   position.z >= 0 && position.z < depth &&
                   grid[position.x, position.y, position.z] != null;
        }
    }
}
