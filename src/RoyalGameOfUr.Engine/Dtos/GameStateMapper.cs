namespace RoyalGameOfUr.Engine.Dtos;

public static class GameStateMapper
{
    public static GameStateDto ToDto(GameState state)
    {
        var p1 = state.GetPieces(Player.One);
        var p2 = state.GetPieces(Player.Two);

        var p1Dtos = new PieceDto[p1.Length];
        for (int i = 0; i < p1.Length; i++)
            p1Dtos[i] = new PieceDto(p1[i].Id, p1[i].Position);

        var p2Dtos = new PieceDto[p2.Length];
        for (int i = 0; i < p2.Length; i++)
            p2Dtos[i] = new PieceDto(p2[i].Id, p2[i].Position);

        return new GameStateDto(
            state.Rules.Name,
            state.CurrentPlayer,
            state.Winner,
            state.LastRoll,
            state.EffectiveRoll,
            p1Dtos,
            p2Dtos);
    }

    public static GameState FromDto(GameStateDto dto)
    {
        var rules = ResolveRules(dto.RulesName);
        var builder = new GameStateBuilder(rules);

        foreach (var piece in dto.Player1Pieces)
            builder.WithPiece(Player.One, piece.Id, piece.Position);

        foreach (var piece in dto.Player2Pieces)
            builder.WithPiece(Player.Two, piece.Id, piece.Position);

        builder.WithCurrentPlayer(dto.CurrentPlayer);

        var state = builder.Build();
        state.LastRoll = dto.LastRoll;
        state.EffectiveRoll = dto.EffectiveRoll;
        state.Winner = dto.Winner;
        return state;
    }

    public static GameRules ResolveRules(string name) => name switch
    {
        "Finkel" => GameRules.Finkel,
        "Simple" => GameRules.Simple,
        "Masters" => GameRules.Masters,
        "Blitz" => GameRules.Blitz,
        "Tournament" => GameRules.Tournament,
        _ => GameRules.Finkel
    };
}
