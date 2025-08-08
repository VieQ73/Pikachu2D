public static class Pathfinder
{
    // Hàm kiểm tra cơ bản cho Cột mốc 1
    public static bool AreTilesMatchable(Tile t1, Tile t2)
    {
        // Điều kiện cơ bản: Phải cùng loại
        if (t1.TileType != t2.TileType)
        {
            return false;
        }

        // TODO: Thêm logic tìm đường BFS ở đây trong Cột mốc 2

        // Tạm thời, chỉ cần cùng loại là coi như match để test
        return true;
    }

}