namespace RoyalGameOfUr.Engine.Dtos;

public record PieceDto(int Id, int Position);

public record GameStateDto(
    string RulesName,
    Player CurrentPlayer,
    Player? Winner,
    int LastRoll,
    int EffectiveRoll,
    PieceDto[] Player1Pieces,
    PieceDto[] Player2Pieces);
