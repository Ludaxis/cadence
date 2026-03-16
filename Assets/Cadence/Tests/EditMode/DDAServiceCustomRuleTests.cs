using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Cadence.Tests
{
    [TestFixture]
    public class DDAServiceCustomRuleTests
    {
        [Test]
        public void RegisterRule_CustomRuleAppearsInProposal()
        {
            var service = new DDAService(CreateConfig());
            service.RegisterRule(new CustomRule());

            service.BeginSession("level_1",
                new Dictionary<string, float> { { "difficulty", 100f } },
                LevelType.Standard);

            service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            service.EndSession(SessionOutcome.Win);

            var proposal = service.GetProposal(
                new Dictionary<string, float> { { "difficulty", 100f } },
                LevelType.Standard,
                1);

            Assert.IsNotNull(proposal);
            Assert.IsTrue(ContainsRuleNamed(proposal.Deltas, CustomRule.Name));
        }

        [Test]
        public void RegisterRuleProvider_CustomRulePackAppearsInProposal()
        {
            var service = new DDAService(CreateConfig());
            service.RegisterRuleProvider(new CustomRuleProvider());

            service.BeginSession("level_provider",
                new Dictionary<string, float>
                {
                    { "difficulty", 100f },
                    { "assist_budget", 2f }
                },
                LevelType.Standard);

            service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
            service.RecordSignal(SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality, 0);
            service.EndSession(SessionOutcome.Win);

            var proposal = service.GetProposal(
                new Dictionary<string, float>
                {
                    { "difficulty", 100f },
                    { "assist_budget", 2f }
                },
                LevelType.Standard,
                1);

            Assert.IsNotNull(proposal);
            Assert.IsTrue(ContainsRuleNamed(proposal.Deltas, CustomRule.Name));
            Assert.IsTrue(ContainsRuleNamed(proposal.Deltas, CustomAssistRule.Name));
        }

        [Test]
        public void RegisterLevelTypeConfigProvider_OverridesBuiltInSemantics()
        {
            var service = new DDAService(CreateConfig());
            service.RegisterLevelTypeConfigProvider(new CustomLevelTypeConfigProvider());

            for (int i = 0; i < 6; i++)
            {
                service.BeginSession($"custom_type_{i}",
                    new Dictionary<string, float> { { "custom_budget", 20f } },
                    LevelType.MoveLimited);
                service.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, 0);
                service.RecordSignal(SignalKeys.MoveOptimal, 0f, SignalTier.DecisionQuality, 0);
                service.EndSession(SessionOutcome.Lose);
            }

            var proposal = service.GetProposal(
                new Dictionary<string, float>
                {
                    { "custom_budget", 20f },
                    { "difficulty", 100f }
                },
                LevelType.MoveLimited,
                6);

            Assert.IsNotNull(proposal);
            Assert.Greater(GetProposedValue(proposal.Deltas, "custom_budget"), 20f);
        }

        private static DDAConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<DDAConfig>();
            config.EnableSignalStorage = false;
            config.EnableMidSessionDetection = false;
            config.EnableBetweenSessionAdjustment = true;
            config.PlayerModelConfig = ScriptableObject.CreateInstance<PlayerModelConfig>();
            config.FlowDetectorConfig = ScriptableObject.CreateInstance<FlowDetectorConfig>();
            config.AdjustmentEngineConfig = ScriptableObject.CreateInstance<AdjustmentEngineConfig>();
            config.AdjustmentEngineConfig.GlobalCooldownSeconds = 0f;
            config.AdjustmentEngineConfig.PerParameterCooldownSeconds = 0f;
            return config;
        }

        private static bool ContainsRuleNamed(List<ParameterDelta> deltas, string ruleName)
        {
            for (int i = 0; i < deltas.Count; i++)
            {
                if (deltas[i].RuleName == ruleName)
                    return true;
            }

            return false;
        }

        private static float GetProposedValue(List<ParameterDelta> deltas, string parameterKey)
        {
            for (int i = 0; i < deltas.Count; i++)
            {
                if (deltas[i].ParameterKey == parameterKey)
                    return deltas[i].ProposedValue;
            }

            Assert.Fail($"No delta found for {parameterKey}");
            return 0f;
        }

        private sealed class CustomRule : IAdjustmentRule
        {
            public const string Name = "CustomRule";

            public string RuleName => Name;

            public bool IsApplicable(AdjustmentContext context) => true;

            public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
            {
                proposal.Deltas.Add(new ParameterDelta
                {
                    ParameterKey = "difficulty",
                    CurrentValue = 100f,
                    ProposedValue = 105f,
                    RuleName = Name
                });
            }
        }

        private sealed class CustomAssistRule : IAdjustmentRule
        {
            public const string Name = "CustomAssistRule";

            public string RuleName => Name;

            public bool IsApplicable(AdjustmentContext context) => true;

            public void Evaluate(AdjustmentContext context, AdjustmentProposal proposal)
            {
                proposal.Deltas.Add(new ParameterDelta
                {
                    ParameterKey = "assist_budget",
                    CurrentValue = 2f,
                    ProposedValue = 3f,
                    RuleName = Name
                });
            }
        }

        private sealed class CustomRuleProvider : IAdjustmentRuleProvider
        {
            public IEnumerable<IAdjustmentRule> CreateRules(DDAConfig config)
            {
                yield return new CustomRule();
                yield return new CustomAssistRule();
            }
        }

        private sealed class CustomLevelTypeConfigProvider : ILevelTypeConfigProvider
        {
            public bool TryGetLevelTypeConfig(LevelType type, out LevelTypeConfig config)
            {
                if (type != LevelType.MoveLimited)
                {
                    config = null;
                    return false;
                }

                config = new LevelTypeConfig(type, "custom_budget", 1f, true, true)
                {
                    SecondaryParameterKeys = new List<string> { "difficulty" },
                    ParameterSemanticsEntries = new List<ParameterSemantics>
                    {
                        new ParameterSemantics
                        {
                            ParameterKey = "custom_budget",
                            Polarity = ParameterPolarity.HigherIsEasier,
                            Adjustable = true,
                            HasMinValue = true,
                            MinValue = 1f
                        },
                        new ParameterSemantics
                        {
                            ParameterKey = "difficulty",
                            Polarity = ParameterPolarity.HigherIsHarder,
                            Adjustable = true,
                            HasMinValue = true,
                            MinValue = 1f
                        }
                    }
                };
                return true;
            }
        }
    }
}
