namespace RoyalGameOfUr.Engine;

public sealed class GameRules
{
    public IReadOnlySet<int> RosettePositions { get; }
    public int PiecesPerPlayer { get; }
    public int PathLength { get; }
    public int SharedLaneStart { get; }
    public int SharedLaneEnd { get; }
    public int DiceCount { get; }

    public GameRules(
        IReadOnlySet<int> rosettePositions,
        int piecesPerPlayer,
        int pathLength,
        int sharedLaneStart,
        int sharedLaneEnd,
        int diceCount = 4)
    {
        RosettePositions = rosettePositions;
        PiecesPerPlayer = piecesPerPlayer;
        PathLength = pathLength;
        SharedLaneStart = sharedLaneStart;
        SharedLaneEnd = sharedLaneEnd;
        DiceCount = diceCount;
    }

    public bool IsRosette(int position) => RosettePositions.Contains(position);

    public bool IsSharedLane(int position) =>
        position >= SharedLaneStart && position <= SharedLaneEnd;

    public static GameRules Finkel => new(
        rosettePositions: new HashSet<int> { 4, 8, 14 },
        piecesPerPlayer: 7,
        pathLength: 15,
        sharedLaneStart: 5,
        sharedLaneEnd: 12);

    public static GameRules Simple => new(
        rosettePositions: new HashSet<int> { 4, 8 },
        piecesPerPlayer: 7,
        pathLength: 15,
        sharedLaneStart: 5,
        sharedLaneEnd: 12);
}
