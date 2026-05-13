using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class AdjustmentProposalTests
    {
        [Test]
        public void CapVariantStepForConfidence_LowConfidenceCapsPositiveAndNegativeSteps()
        {
            Assert.AreEqual(1, AdjustmentProposal.CapVariantStepForConfidence(3, 0.2f));
            Assert.AreEqual(-1, AdjustmentProposal.CapVariantStepForConfidence(-4, 0.2f));
        }

        [Test]
        public void CapVariantStepForConfidence_HighConfidenceKeepsOriginalStep()
        {
            Assert.AreEqual(3, AdjustmentProposal.CapVariantStepForConfidence(3, 0.6f));
            Assert.AreEqual(-4, AdjustmentProposal.CapVariantStepForConfidence(-4, 0.6f));
        }

        [Test]
        public void CapVariantStepForConfidence_InstanceUsesProposalConfidence()
        {
            var proposal = new AdjustmentProposal { Confidence = 0.39f };

            Assert.AreEqual(1, proposal.CapVariantStepForConfidence(2));
        }

        [Test]
        public void ServiceCapVariantStepForConfidence_RespectsConfigToggle()
        {
            var config = ScriptableObject.CreateInstance<DDAConfig>();
            config.AdjustmentEngineConfig = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.AdjustmentEngineConfig.EnableLowConfidenceStepCap = false;
            var service = new DDAService(config);
            var proposal = new AdjustmentProposal { Confidence = 0.1f };

            Assert.AreEqual(3, service.CapVariantStepForConfidence(3, proposal));
        }
    }
}
