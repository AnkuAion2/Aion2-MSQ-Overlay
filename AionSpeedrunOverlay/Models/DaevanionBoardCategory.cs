namespace AionSpeedrunOverlay.Models
{
    public enum DaevanionBoardCategory
    {
        Nezekan,
        Zikel,
        Vaizel,
        Triniel
    }


    public readonly record struct DaevanionNode(int Row, int Column);
}
