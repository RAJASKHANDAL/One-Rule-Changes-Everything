/// <summary>CONTROLS REVERSED: horizontal input is inverted through the central InputModifier (owner-tracked, never stacks).</summary>
public class ReversedControlsRule : RuleBase
{
    const string Key = "reversed";

    protected override void OnActivated()
    {
        InputModifier.AddReverse(this);
        StatusBus.Set(Key, "CONTROLS REVERSED", definition.accent);
    }

    protected override void OnDeactivated()
    {
        InputModifier.RemoveReverse(this);
        StatusBus.Clear(Key);
    }
}
