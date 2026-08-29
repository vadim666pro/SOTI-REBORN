using Robust.Shared.Prototypes;

namespace Content.Shared.CounterStrike;

[Prototype("csPhrases")]
public sealed partial class CsPhrasesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("phrases")]
    public List<string> Phrases { get; private set; } = new();

    [DataField("images")]
    public List<string> Images { get; private set; } = new();
}
