namespace RoyalGameOfUr.Engine;

public sealed class GameRules
{
    public string Name { get; }
    public IReadOnlySet<int> RosettePositions { get; }
    public int PiecesPerPlayer { get; }
    public int PathLength { get; }
    public int SharedLaneStart { get; }
    public int SharedLaneEnd { get; }
    public int DiceCount { get; }
    public IReadOnlyDictionary<int, int> CaptureMap { get; }
    public bool SafeRosettes { get; }
    public bool RosetteExtraRoll { get; }
    public bool CaptureExtraRoll { get; }
    public int? ZeroRollValue { get; }
    public bool AllowStacking { get; }
    public bool AllowBackwardMoves { get; }
    public bool AllowVoluntarySkip { get; }

    public GameRules(
        IReadOnlySet<int> rosettePositions,
        int piecesPerPlayer,
        int pathLength,
        int sharedLaneStart,
        int sharedLaneEnd,
        int diceCount = 4,
        string? name = null,
        IReadOnlyDictionary<int, int>? captureMap = null,
        bool safeRosettes = true,
        bool rosetteExtraRoll = true,
        bool captureExtraRoll = false,
        int? zeroRollValue = null,
        bool allowStacking = false,
        bool allowBackwardMoves = false,
        bool allowVoluntarySkip = false)
    {
        Name = name ?? "Custom";
        RosettePositions = rosettePositions;
        PiecesPerPlayer = piecesPerPlayer;
        PathLength = pathLength;
        SharedLaneStart = sharedLaneStart;
        SharedLaneEnd = sharedLaneEnd;
        DiceCount = diceCount;
        CaptureMap = captureMap ?? BuildIdentityCaptureMap(sharedLaneStart, sharedLaneEnd);
        SafeRosettes = safeRosettes;
        RosetteExtraRoll = rosetteExtraRoll;
        CaptureExtraRoll = captureExtraRoll;
        ZeroRollValue = zeroRollValue;
        AllowStacking = allowStacking;
        AllowBackwardMoves = allowBackwardMoves;
        AllowVoluntarySkip = allowVoluntarySkip;
    }

    public bool IsRosette(int position) => RosettePositions.Contains(position);

    public bool IsSharedLane(int position) => CaptureMap.ContainsKey(position);

    public int GetOpponentCapturePosition(int position) =>
        CaptureMap.TryGetValue(position, out int opponentPos) ? opponentPos : -1;

    private static Dictionary<int, int> BuildIdentityCaptureMap(int start, int end)
    {
        var map = new Dictionary<int, int>();
        for (int i = start; i <= end; i++)
            map[i] = i;
        return map;
    }

    private static Dictionary<int, int> BuildMastersCaptureMap()
    {
        var map = new Dictionary<int, int>();
        for (int i = 4; i <= 10; i++)
            map[i] = i; // shared middle: identity
        map[11] = 15;
        map[12] = 14;
        map[13] = 13;
        map[14] = 12;
        map[15] = 11;
        return map;
    }

    public static GameRules Finkel => new(
        rosettePositions: new HashSet<int> { 4, 8, 14 },
        piecesPerPlayer: 7,
        pathLength: 15,
        sharedLaneStart: 5,
        sharedLaneEnd: 12,
        name: "Finkel");

    public static GameRules Simple => new(
        rosettePositions: new HashSet<int> { 4, 8 },
        piecesPerPlayer: 7,
        pathLength: 15,
        sharedLaneStart: 5,
        sharedLaneEnd: 12,
        name: "Simple");

    public static GameRules Masters => new(
        rosettePositions: new HashSet<int> { 3, 7, 11, 15 },
        piecesPerPlayer: 7,
        pathLength: 16,
        sharedLaneStart: 4,
        sharedLaneEnd: 15,
        diceCount: 3,
        name: "Masters",
        captureMap: BuildMastersCaptureMap(),
        safeRosettes: false,
        zeroRollValue: 4);

    public static GameRules Blitz => new(
        rosettePositions: new HashSet<int> { 3, 7, 11, 15 },
        piecesPerPlayer: 5,
        pathLength: 16,
        sharedLaneStart: 4,
        sharedLaneEnd: 15,
        name: "Blitz",
        captureMap: BuildMastersCaptureMap(),
        safeRosettes: false,
        captureExtraRoll: true);

    public static GameRules Tournament => new(
        rosettePositions: new HashSet<int> { 3, 7, 11, 15 },
        piecesPerPlayer: 5,
        pathLength: 16,
        sharedLaneStart: 4,
        sharedLaneEnd: 15,
        name: "Tournament",
        captureMap: BuildMastersCaptureMap(),
        rosetteExtraRoll: false,
        allowStacking: true,
        allowBackwardMoves: true,
        allowVoluntarySkip: true);
}
