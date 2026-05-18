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

        [Test]
        public void CapVariantStepForFailedRetry_BlocksHarderStepOnlyDuringFailedRetry()
        {
            Assert.AreEqual(0, AdjustmentProposal.CapVariantStepForFailedRetry(1, true));
            Assert.AreEqual(0, AdjustmentProposal.CapVariantStepForFailedRetry(0, true));
            Assert.AreEqual(-1, AdjustmentProposal.CapVariantStepForFailedRetry(-1, true));
            Assert.AreEqual(1, AdjustmentProposal.CapVariantStepForFailedRetry(1, false));
        }

        [Test]
        public void CapVariantForFailedRetry_DoesNotExceedFailedVariant()
        {
            Assert.AreEqual(7, AdjustmentProposal.CapVariantForFailedRetry(8, 7, true));
            Assert.AreEqual(7, AdjustmentProposal.CapVariantForFailedRetry(7, 7, true));
            Assert.AreEqual(6, AdjustmentProposal.CapVariantForFailedRetry(6, 7, true));
            Assert.AreEqual(8, AdjustmentProposal.CapVariantForFailedRetry(8, 7, false));
        }

        [Test]
        public void ServiceFailedRetryCaps_DelegateToProposalGuards()
        {
            var service = new DDAService(null);

            Assert.AreEqual(0, service.CapVariantStepForFailedRetry(1, true));
            Assert.AreEqual(7, service.CapVariantForFailedRetry(8, 7, true));
        }
    }
}
