using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public static class AnswerRulesHelpers
    {
        public static string RuleStart { get; } = "<<";
        public static string RuleEnd { get; } = ">>";
        public static string CommandCondition { get; } = " ? ";
        public static string CommandLink { get; } = " H ";
        public static string CommandNFT { get; } = " N ";
        public static string ObjectStartIntent { get; } = "i.";
        public static string ObjectStartEntity { get; } = "e.";
        public static char ObjectKnown { get; } = '=';
        public static char ObjectUnknow { get; } = '?';
        public static char ObjectDontHave { get; } = '!';
        public static char ObjectEnd { get; } = ';';
        public static char ObjectConditionAnd { get; } = '&';
        public static char ObjectConditionOr { get; } = '|';
        public static (string, Dictionary<int, AnswerRule>) ParseRules(string answer)
        {
            var rules = new Dictionary<int, AnswerRule>();
            var substr = string.Empty;
            var mainstr = string.Empty;
            var answerArray = answer.ToArray();
            var startCapture = 0;
            var startIndex = 0;
            var endCapture = 0;
            for (int i = 0; i < answerArray.Length; i++)
            {
                var actualCharacter = answerArray[i];

                if (startCapture == 0 && actualCharacter == '<')
                    startCapture++;
                else if (startCapture == 1 && actualCharacter == '<')
                {
                    startCapture++;
                    if (answerArray.Length < i + 1)
                        startIndex = i + 1;
                    else
                        startIndex = i;

                    endCapture = 0;
                }
                else if (startCapture == 3 && endCapture == 0 && actualCharacter == '>')
                    endCapture++;
                else if (startCapture == 3 && endCapture == 1 && actualCharacter == '>')
                {
                    startCapture = 0;
                    endCapture++;
                }

                if (startCapture == 0 && endCapture == 0)
                    mainstr += actualCharacter;

                if (startCapture == 3 && endCapture == 0)
                    substr += answerArray[i];
                else if (startCapture == 0 && endCapture == 2)
                {
                    rules.Add(i, new AnswerRule(substr));
                    substr = string.Empty;
                }

                if (startCapture == 2)
                    startCapture++;
            }

            return (mainstr, rules);
        }

        public static AnswerRuleType ParseRuleType(string parsedRule)
        {
            if (parsedRule.Contains(AnswerRulesHelpers.CommandCondition))
                return AnswerRuleType.Condition;
            else if (parsedRule.Contains(AnswerRulesHelpers.CommandLink))
                return AnswerRuleType.Link;
            else if (parsedRule.Contains(AnswerRulesHelpers.CommandNFT))
                return AnswerRuleType.NFT;

            return AnswerRuleType.None;
        }

        public static string ParseRuleString(string parsedRule)
        {
            var substr = string.Empty;
            var answerArray = parsedRule.ToArray();
            var startCapture = 0;
            var startIndex = 0;
            var endCapture = 0;
            for (int i = 0; i < answerArray.Length; i++)
            {
                var actualCharacter = answerArray[i];
                if (startCapture == 0 && actualCharacter == '"')
                {
                    startCapture++;
                    if (answerArray.Length < i + 1)
                        startIndex = i + 1;
                    else
                        startIndex = i;

                    endCapture = 0;
                }
                else if (startCapture == 2 && endCapture == 0 && actualCharacter == '"')
                {
                    startCapture = 0;
                    endCapture++;
                }
                
                if (startCapture == 2 && endCapture == 0)
                    substr += answerArray[i];
                else if (startCapture == 0 && endCapture == 1)
                {
                    return substr;
                }

                if (startCapture == 1)
                    startCapture++;
            }

            return string.Empty;
        }

        public static RuleObject ParseObject(string parsedRule)
        {
            var result = new RuleObject();
            var tmp = (" " + parsedRule).Split(new[] { CommandCondition, CommandLink, CommandNFT }, StringSplitOptions.RemoveEmptyEntries);
            if (tmp != null && tmp.Length > 0)
            {
                var array = tmp[0].ToArray();
                var startCapture = 0;
                var startIndex = 0;
                var endCapture = 0;

                for (int i = 0; i < array.Length; i++)
                {
                    var actualCharacter = array[i];

                    var actualString = string.Empty;
                    if (i + 1 < array.Length)
                        actualString = new string(new char[] { array[i], array[i + 1] });

                    var actualStringSecond = string.Empty;
                    if (i + 3 < array.Length)
                        actualStringSecond = new string(new char[] { array[i + 2], array[i + 3] });

                    if (startCapture == 0 && actualString == ObjectStartIntent || actualString == ObjectStartEntity)
                    {
                        startCapture++;
                        startIndex = i;
                        endCapture = 0;

                        if (i > 0)
                        {
                            if (array[i - 1] == ObjectDontHave)
                                result.ObjectCommand = AnswerRuleObjectCommandType.DontHave;
                            else if (array[i - 1] == ObjectKnown)
                                result.ObjectCommand = AnswerRuleObjectCommandType.Known;
                            else if (array[i - 1] == ObjectUnknow)
                                result.ObjectCommand = AnswerRuleObjectCommandType.Unknown;
                        }
                    }
                    else if (startCapture == 1 && endCapture == 0 && actualCharacter == ObjectEnd)
                    {
                        startCapture = 0;
                        endCapture++;
                    }
                    else if (startCapture == 1 &&
                             endCapture == 0 &&
                             (actualCharacter == ObjectConditionAnd && array.Length > i + 2 &&
                                 (array[i + 1] == ObjectDontHave || array[i + 1] == ObjectKnown || array[i + 1] == ObjectUnknow) &&
                                 (actualStringSecond == ObjectStartEntity || actualStringSecond == ObjectStartIntent)
                             ))
                    {
                        startCapture = 0;
                        endCapture++;
                        var found = false;
                        var start = false;
                        var index = i;
                        var si = 0;
                        var obj = new RuleObject();
                        while (!found)
                        {
                            if (index > array.Length)
                                break;

                            if (start && array[index] != ObjectEnd)
                            {
                                obj.Name += array[index];
                            }
                            else if (start && array[index] == ObjectEnd)
                            {
                                found = true;
                                break;
                            }

                            if (si == 0 && array[index] == ObjectConditionAnd)
                                obj.Connector = ObjectConditionAnd;
                            else if (si == 0 && array[index] == ObjectConditionOr)
                                obj.Connector = ObjectConditionOr;

                            if (si == 1 && array[index] == ObjectDontHave)
                            {
                                obj.ObjectCommand = AnswerRuleObjectCommandType.DontHave;
                                start = true;
                            }
                            else if (si == 1 && array[index] == ObjectKnown)
                            {
                                obj.ObjectCommand = AnswerRuleObjectCommandType.Known;
                                start = true;
                            }
                            else if (si == 1 && array[index] == ObjectUnknow)
                            {
                                obj.ObjectCommand = AnswerRuleObjectCommandType.Unknown;
                                start = true;
                            }
                            
                            si++;
                            index++;
                        }

                        if (found)
                            result.ChildObject = obj;
                    }

                    if (startCapture == 1 &&
                        endCapture == 0 &&
                        array[i] != ObjectKnown &&
                        array[i] != ObjectUnknow &&
                        array[i] != ObjectEnd &&
                        array[i] != ' ' &&
                        array[i] != AnswerRulesHelpers.ObjectConditionAnd &&
                        array[i] != AnswerRulesHelpers.ObjectConditionOr)
                    {
                        result.Name += array[i];
                    }
                    else if (startCapture == 0 && endCapture == 1)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
    }
}
