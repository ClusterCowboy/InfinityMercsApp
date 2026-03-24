namespace InfinityMercsApp.Views.Common;

public abstract class CompanyPeripheralMercsCompanyStatsBase
{
    public string NameHeading { get; init; } = string.Empty;
    public int? MoveFirstCm { get; init; }
    public int? MoveSecondCm { get; init; }
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";
    public string VitalityHeader { get; init; } = "VITA";
    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Characteristics { get; init; } = "-";
}
