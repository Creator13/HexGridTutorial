public enum HexDirection {
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions {
    public static HexDirection Opposite(this HexDirection dir) {
        return (int) dir < 3 ? dir + 3 : dir - 3;
    }

    public static HexDirection Previous(this HexDirection dir) {
        return dir == HexDirection.NE ? HexDirection.NW : dir - 1;
    }
    
    public static HexDirection Next(this HexDirection dir) {
        return dir == HexDirection.NW ? HexDirection.NE : dir + 1;
    }

    public static HexDirection Previous2(this HexDirection dir) {
        dir -= 2;
        return dir >= HexDirection.NE ? dir : dir + 6;
    }
    
    public static HexDirection Next2(this HexDirection dir) {
        dir += 2;
        return dir <= HexDirection.NW ? dir : dir - 6;
    }
}