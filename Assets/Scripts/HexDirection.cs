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
}