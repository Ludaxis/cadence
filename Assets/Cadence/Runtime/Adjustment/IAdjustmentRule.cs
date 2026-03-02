namespace Cadence
{
    public interface IAdjustmentRule
    {
        string RuleName { get; }
        bool IsApplicable(AdjustmentContext context);
        void Evaluate(AdjustmentContext context, AdjustmentProposal proposal);
    }
}
