using Unity.Mathematics;

namespace Client.Utils
{
    public class JobHelper
    {
        public static int2 WorldToCell(float2 posXZ, float cellSize)
        {
            return (int2)math.floor(posXZ / cellSize);
        }
        
        public static int Hash(int2 cell)
        {
            return cell.x * 73856093 ^ cell.y * 19349663;
        }
    }
}