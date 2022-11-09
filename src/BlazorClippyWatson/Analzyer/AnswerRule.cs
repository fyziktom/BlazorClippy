using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public enum AnswerRuleType
    {
        None,
        Condition,
        Link,
        NFT
    }
    public enum AnswerRuleObjectCommandType
    {
        None,
        Known,
        Unknown,
        DontHave
    }

    public class RuleObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public char Connector { get; set; } = '&';
        public AnswerRuleObjectCommandType ObjectCommand { get; set; } = AnswerRuleObjectCommandType.None;

        public RuleObject? ChildObject { get; set; }
    }

    public class RuleDetails
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RuleString { get; set; } = string.Empty;
        public RuleObject Object { get; set; } = new RuleObject();
    }
    public class AnswerRule
    {
        public AnswerRule(string parsedRuleFromAnswer)
        {
            if (!string.IsNullOrEmpty(parsedRuleFromAnswer))
            {
                ParsedRuleFromAnswer = parsedRuleFromAnswer;
                Type = AnswerRulesHelpers.ParseRuleType(parsedRuleFromAnswer);

                if (Type == AnswerRuleType.Condition)
                {
                    var split = parsedRuleFromAnswer.Split(AnswerRulesHelpers.CommandCondition, StringSplitOptions.RemoveEmptyEntries);
                    if (split != null && split.Length > 0)
                    {
                        foreach (var sp in split)
                        {
                            Rules.Add(new RuleDetails()
                            {
                                RuleString = AnswerRulesHelpers.ParseRuleString(sp),
                                Object = AnswerRulesHelpers.ParseObject(sp)
                            });
                        }
                    }
                    else
                    {
                        Rules.Add(new RuleDetails()
                        {
                            RuleString = AnswerRulesHelpers.ParseRuleString(ParsedRuleFromAnswer),
                            Object = AnswerRulesHelpers.ParseObject(ParsedRuleFromAnswer)
                        });
                    }
                }
                else if (Type == AnswerRuleType.NFT)
                {
                    var split = parsedRuleFromAnswer.Split(AnswerRulesHelpers.CommandNFT, StringSplitOptions.RemoveEmptyEntries);
                    if (split != null && split.Length > 0)
                    {
                        foreach (var sp in split)
                        {
                            Rules.Add(new RuleDetails()
                            {
                                RuleString = AnswerRulesHelpers.ParseRuleString(sp),
                                Object = AnswerRulesHelpers.ParseObject(sp)
                            });
                        }
                    }
                    else
                    {
                        Rules.Add(new RuleDetails()
                        {
                            RuleString = AnswerRulesHelpers.ParseRuleString(ParsedRuleFromAnswer),
                            Object = AnswerRulesHelpers.ParseObject(ParsedRuleFromAnswer)
                        });
                    }
                }
            }
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AnswerRuleType Type { get; set; } = AnswerRuleType.None;
        public string ParsedRuleFromAnswer { get; set; } = string.Empty;

        public List<RuleDetails> Rules { get; set; } = new List<RuleDetails>();
        public string Command { get; set; } = string.Empty;
        public string TextOutput { get; set; } = string.Empty;
        public Func<string, string>? Rule { get; set; }

    }
}
